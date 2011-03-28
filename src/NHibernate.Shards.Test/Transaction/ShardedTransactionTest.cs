using System.Collections.Generic;
using System.Data;
using System.Linq;
using NHibernate.Shards.Test.Mock;
using NHibernate.Shards.Transaction;
using NHibernate.Transaction;
using NUnit.Framework;

namespace NHibernate.Shards.Test.Transaction
{
    using System;

    [TestFixture]
    public class ShardedTransactionImplTest
    {
        private ShardedTransactionImpl shardedTransaction;
        private TransactionStub[] shardTransactions;

        [SetUp]
        public void SetUp()
        {
            shardTransactions = new[]
                {
                    new TransactionStub(),
                    new TransactionStub()
                };
            var shards = shardTransactions.Select(tx => new MockShard(new MockSession(tx)));
            shardedTransaction = new ShardedTransactionImpl(new MockShardedSessionImplementor(shards));
        }

        [Test]
        public void CanBegin()
        {
            shardedTransaction.Begin();
            Assert.That(shardedTransaction.IsActive, Is.True, "IsActive");
            Assert.That(shardedTransaction.WasCommitted, Is.False, "WasCommitted");
            Assert.That(shardedTransaction.WasRolledBack, Is.False, "WasRolledBack");
        }

        [Test]
        public void CanBeginTwice()
        {
            shardedTransaction.Begin();
            shardedTransaction.Begin();
            Assert.That(shardedTransaction.IsActive, Is.True, "IsActive");
            Assert.That(shardedTransaction.WasCommitted, Is.False, "WasCommitted");
            Assert.That(shardedTransaction.WasRolledBack, Is.False, "WasRolledBack");
        }

        [Test]
        public void CannotBeginAfterFailedCommit()
        {
            shardedTransaction.Begin();
            shardTransactions[1].Fail = true;
            Assert.That(() => shardedTransaction.Commit(), Throws.InstanceOf<HibernateException>());
            Assert.That(() => shardedTransaction.Begin(), Throws.InstanceOf<HibernateException>());
        }

        [Test]
        public void BeginFailsOnFailedShardTransaction()
        {
            shardTransactions[1].Fail = true;
            Assert.That(() => shardedTransaction.Begin(), Throws.InstanceOf<HibernateException>());
        }

        [Test]
        public void CanBeginAgainAfterFailedBegin()
        {
            shardTransactions[1].Fail = true;
            Assert.That(() => shardedTransaction.Begin(), Throws.InstanceOf<HibernateException>());
            
            shardTransactions[1].Fail = false;
            Assert.That(() => shardedTransaction.Begin(), Throws.Nothing);
            Assert.That(shardedTransaction.IsActive, Is.True, "IsActive");
            Assert.That(shardedTransaction.WasCommitted, Is.False, "WasCommitted");
            Assert.That(shardedTransaction.WasRolledBack, Is.False, "WasRolledBack");
        }

        [Test]
        public void CanCommit()
        {
            shardedTransaction.Begin();
            shardedTransaction.Commit();
            Assert.That(shardedTransaction.IsActive, Is.False, "IsActive");
            Assert.That(shardedTransaction.WasCommitted, Is.True, "WasCommitted");
            Assert.That(shardedTransaction.WasRolledBack, Is.False, "WasRolledBack");
        }

        [Test]
        public void CannotCommitBeforeBegin()
        {
            Assert.That(() => shardedTransaction.Commit(), Throws.InstanceOf<HibernateException>());
            Assert.That(shardedTransaction.IsActive, Is.False, "IsActive");
            Assert.That(shardedTransaction.WasCommitted, Is.False, "WasCommitted");
            Assert.That(shardedTransaction.WasRolledBack, Is.False, "WasRolledBack");
        }

        [Test]
        public void CannotCommitAfterRollback()
        {
            shardedTransaction.Begin();
            shardedTransaction.Rollback();
            Assert.That(() => shardedTransaction.Commit(), Throws.InstanceOf<ObjectDisposedException>());
            Assert.That(shardedTransaction.IsActive, Is.False, "IsActive");
            Assert.That(shardedTransaction.WasCommitted, Is.False, "WasCommitted");
            Assert.That(shardedTransaction.WasRolledBack, Is.True, "WasRolledBack");
        }

        [Test]
        public void CommitFailsOnFailedShardTransaction()
        {
            shardedTransaction.Begin();
            shardTransactions[1].Fail = true;
            Assert.That(() => shardedTransaction.Commit(), Throws.InstanceOf<HibernateException>());
            Assert.That(shardedTransaction.IsActive, Is.False, "IsActive");
            Assert.That(shardedTransaction.WasCommitted, Is.False, "WasCommitted");
            Assert.That(shardedTransaction.WasRolledBack, Is.False, "WasRolledBack");
        }

        [Test]
        public void CannotRollbackBeforeBegin()
        {
            //  try {
            //    shardedTransaction.rollback();
            //    fail();
            //  } catch (HibernateException he) {
            //    // good
            //    assertFalse(shardedTransaction.wasRolledBack());
            //  }
            Assert.That(() => shardedTransaction.Rollback(), Throws.InstanceOf<HibernateException>());
            Assert.That(shardedTransaction.IsActive, Is.False, "IsActive");
            Assert.That(shardedTransaction.WasCommitted, Is.False, "WasCommitted");
            Assert.That(shardedTransaction.WasRolledBack, Is.False, "WasRolledBack");
        }

        [Test]
        public void CanRollback()
        {
            //  shardedTransaction.begin();
            //  shardedTransaction.rollback();
            //  assertTrue(shardedTransaction.wasRolledBack());
            shardedTransaction.Begin();
            shardedTransaction.Rollback();
            Assert.That(shardedTransaction.IsActive, Is.False, "IsActive");
            Assert.That(shardedTransaction.WasCommitted, Is.False, "WasCommitted");
            Assert.That(shardedTransaction.WasRolledBack, Is.True, "WasRolledBack");
        }

        [Test]
        public void CannotRollbackAfterCommit()
        {
            //  shardedTransaction.begin();
            //  shardedTransaction.commit();
            //  try {
            //    shardedTransaction.rollback();
            //    fail();
            //  } catch (HibernateException he) {
            //    // good
            //    assertTrue(shardedTransaction.wasRolledBack());
            //  }
            shardedTransaction.Begin();
            shardedTransaction.Commit();
            Assert.That(() => shardedTransaction.Rollback(), Throws.InstanceOf<ObjectDisposedException>());
            Assert.That(shardedTransaction.IsActive, Is.False, "IsActive");
            Assert.That(shardedTransaction.WasCommitted, Is.True, "WasCommitted");
            Assert.That(shardedTransaction.WasRolledBack, Is.False, "WasRolledBack");
        }

        [Test]
        public void CanRollbackAfterFailedCommit()
        {
            //  shardedTransaction.begin();
            //  shardTransactionStub.fail = true;
            //  try {
            //    shardedTransaction.commit();
            //  } catch (HibernateException he) {
            //    assertFalse(shardedTransaction.wasRolledBack());
            //    assertTrue(he.getCause() instanceof HibernateException);
            //    shardedTransaction.rollback();
            //    assertTrue(shardedTransaction.wasRolledBack());
            //  }
            shardedTransaction.Begin();
            shardTransactions[1].Fail = true;
            Assert.That(() => shardedTransaction.Commit(), Throws.InstanceOf<HibernateException>());
            Assert.That(shardedTransaction.WasRolledBack, Is.False, "WasRolledBack after failed commit");

            shardedTransaction.Rollback();
            Assert.That(shardedTransaction.WasRolledBack, Is.True, "WasRolledBack after rollback");
        }

        [Test]
        public void RollbackFailsOnFailedShardTransaction()
        {
            //  shardedTransaction.begin();
            //  shardTransactionStub.fail = true;
            //  try {
            //    shardedTransaction.rollback();
            //  } catch (HibernateException he) {
            //    assertFalse(shardedTransaction.wasRolledBack());
            //    assertTrue(he.getCause() instanceof HibernateException);
            //  }
            shardedTransaction.Begin();
            shardTransactions[1].Fail = true;
            Assert.That(() => shardedTransaction.Rollback(), Throws.InstanceOf<HibernateException>());
            Assert.That(shardedTransaction.IsActive, Is.False, "IsActive");
            Assert.That(shardedTransaction.WasCommitted, Is.False, "WasCommitted");
            Assert.That(shardedTransaction.WasRolledBack, Is.False, "WasRolledBack");
        }

        [Test]
        public void CannotBeginAgainAfterCommit()
        {
            shardedTransaction.Begin();
            shardedTransaction.Commit();

            // Deviation from Hibernate.Shards, but consistent with NHibernate transaction behaviour.
            Assert.That(() => shardedTransaction.Begin(), Throws.InstanceOf<ObjectDisposedException>());
        }

        [Test]
        public void CannotBeginAgainAfterRollback()
        {
            shardedTransaction.Begin();
            shardedTransaction.Commit();

            // Deviation from Hibernate.Shards, but consistent with NHibernate transaction behaviour.
            Assert.That(() => shardedTransaction.Rollback(), Throws.InstanceOf<ObjectDisposedException>());
        }

        #region Mocks

        private class TransactionStub : ShardedTransactionDefaultMock
        {
            private bool wasCommitted;

            public bool Fail { get; set; }

            public override void Dispose()
            {}

            public override void SetupTransaction(ISession session)
            { }

            public override void Begin(IsolationLevel isolationLevel)
            {
                if (Fail)
                {
                    throw new TransactionException("Begin failed");
                }
            }

            public override void Commit()
            {
                if (Fail)
                {
                    throw new TransactionException("Commit failed");
                }
                wasCommitted = true;
            }

            public override void Rollback()
            {
                if (Fail)
                {
                    throw new TransactionException("Rollback failed");
                }
            }

            public override bool WasCommitted
            {
                get { return wasCommitted; }
            }

            public override bool IsActive
            {
                get { return true; }
            }

            public override void RegisterSynchronization(ISynchronization synchronization)
            { }
        }

        private class MockSession : SessionDefaultMock
        {
            private readonly ITransaction transaction;

            public MockSession(ITransaction t)
            {
                transaction = t;
            }

            public override ITransaction Transaction
            {
                get { return transaction; }
            }

            public override ITransaction BeginTransaction(IsolationLevel isolationLevel)
            {
                transaction.Begin(isolationLevel);
                return transaction;
            }
        }

        private class MockShard: ShardDefaultMock 
        {
            private readonly ISession session;

            public MockShard(ISession s) 
            {
                session = s;
            }

            public override ISession Session 
            {
                get { return session; }
            }

            public override ISession EstablishSession()
            {
                return session;
            }
        }

        private class MockShardedSessionImplementor: ShardedSessionImplementorDefaultMock 
        {
            private readonly IList<MockShard> shards;

            public MockShardedSessionImplementor(IEnumerable<MockShard> shards) 
            {
                this.shards = shards.ToList();
            }

            public override IEnumerable<ISession> EstablishedSessions
            {
                get
                {
                    return shards
                        .Where(s => s.Session != null)
                        .Select(s => s.Session);
                }
            }

            public override void AfterTransactionBegin(IShardedTransaction transaction)
            {
                foreach (var shard in shards)
                {
                    transaction.Enlist(shard.EstablishSession());
                }
            }

            public override void AfterTransactionCompletion(IShardedTransaction transaction, bool? success)
            {}
        }

        #endregion
    }
}
