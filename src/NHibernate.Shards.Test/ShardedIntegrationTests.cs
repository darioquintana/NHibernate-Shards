﻿namespace NHibernate.Shards.Test
{
    using System;
    using System.Collections.Generic;
    using NHibernate.Criterion;
    using NHibernate.Mapping.ByCode;
	using NHibernate.Mapping.ByCode.Conformist;
    using NUnit.Framework;

	[TestFixture]
	public class ShardedIntegrationTests : ShardedTestCase
	{
		#region Test SetUp

		protected override void Configure(NHibernate.Cfg.Configuration protoConfig)
		{
            protoConfig.Properties[NHibernate.Cfg.Environment.ShowSql] = "True";

            var mapper = new ModelMapper();
			mapper.AddMapping<PersonMap>();
			protoConfig.AddMapping(mapper.CompileMappingForAllExplicitlyAddedEntities());
		}

		#endregion

		#region Tests

		[Test]
		public void CanSaveEntityWithComponentList()
		{
			var person = new Person {LegalName = new PersonName {FirstName = "John", LastName = "Doe"}};
			person.Aliases.Add(new PersonName {FirstName = "Johny", LastName = "D"});
			person.Aliases.Add(new PersonName {FirstName = "Don Jon"});

			using (var session = SessionFactory.OpenSession())
			using (session.BeginTransaction())
			{
				session.Save(person);
				session.Flush();
			}

			Assert.That(person.Id, Is.GreaterThan(0));
		}

        [Test]
        public void CanQueryWithCriteria()
        {
            var person1 = new Person { LegalName = new PersonName { FirstName = "John", LastName = "Doe" } };
            var person2 = new Person { LegalName = new PersonName { FirstName = "Mary", LastName = "Jane" } };

            using (var session = SessionFactory.OpenSession())
            {
                using (session.BeginTransaction())
                {
                    session.Save(person1);
                    session.Save(person2);
                    session.Flush();
                    session.Clear();

                    var persistentPersons = session.CreateCriteria<Person>()
                        .Add(Restrictions.Eq(nameof(Person.LegalName) + "." + nameof(PersonName.FirstName), "Mary"))
                        .List();
                    Assert.That(persistentPersons, Has.Count.EqualTo(1) & Is.EquivalentTo(new[] { person2 }));
                }
            }
        }

        [Test]
        public void CanCountRowsWithCriteria()
        {
            var person1 = new Person { LegalName = new PersonName { FirstName = "John", LastName = "Doe" } };
            var person2 = new Person { LegalName = new PersonName { FirstName = "Mary", LastName = "Jane" } };

            using (var session = SessionFactory.OpenSession())
            {
                using (session.BeginTransaction())
                {
                    session.Save(person1);
                    session.Save(person2);
                    session.Flush();
                    session.Clear();

                    var rowCount = session.CreateCriteria<Person>()
                        .SetProjection(Projections.RowCount())
                        .UniqueResult<int>();
                    Assert.That(rowCount, Is.EqualTo(2), "RowCount");

                    var rowCountInt64 = session.CreateCriteria<Person>()
                        .SetProjection(Projections.RowCountInt64())
                        .UniqueResult<long>();
                    Assert.That(rowCountInt64, Is.EqualTo(2), "RowCountInt64");
                }
            }
        }

        [Test]
	    public void CanQueryWithQueryOver()
	    {
            var person1 = new Person { LegalName = new PersonName { FirstName = "John", LastName = "Doe" } };
            var person2 = new Person { LegalName = new PersonName { FirstName = "Mary", LastName = "Jane" } };

	        using (var session = SessionFactory.OpenSession())
	        {
	            using (session.BeginTransaction())
	            {
	                session.Save(person1);
	                session.Save(person2);
	                session.Flush();
                    session.Clear();

                    var persistentPersons = session.QueryOver<Person>()
	                    .Where(p => p.LegalName.FirstName == "Mary")
                        .List();
                    Assert.That(persistentPersons, Has.Count.EqualTo(1) & Is.EquivalentTo(new[] { person2 }));
	            }
            }
        }

        [Test]
        public void CanCountRowsWithQueryOver()
        {
            var person1 = new Person { LegalName = new PersonName { FirstName = "John", LastName = "Doe" } };
            var person2 = new Person { LegalName = new PersonName { FirstName = "Mary", LastName = "Jane" } };

            using (var session = SessionFactory.OpenSession())
            {
                using (session.BeginTransaction())
                {
                    session.Save(person1);
                    session.Save(person2);
                    session.Flush();
                    session.Clear();

                    var rowCount = session.QueryOver<Person>().RowCount();
                    Assert.That(rowCount, Is.EqualTo(2), "RowCount");

                    var rowCountInt64 = session.QueryOver<Person>().RowCountInt64();
                    Assert.That(rowCountInt64, Is.EqualTo(2), "RowCountInt64");
                }
            }
        }

        #endregion

        #region Domain model and mapping classes

        public class Person
		{
			public int? Id { get; set; }
		    public Guid Guid { get; set; } = Guid.NewGuid();
            public PersonName LegalName { get; set; }
			public IList<PersonName> Aliases { get; protected set; }

			public Person()
			{
				this.Aliases = new List<PersonName>();
			}

            public bool Equals(Person person)
            {
                if (ReferenceEquals(person, null)) return false;
                return this.Guid == person.Guid;
            }

            public override bool Equals(object obj)
            {
                return Equals(obj as Person);
            }

		    public override int GetHashCode()
		    {
		        return this.Guid.GetHashCode();
		    }
		}

		public class PersonName
		{
			public string FirstName { get; set; }
			public string LastName { get; set; }
		}

		public class PersonMap : ClassMapping<Person>
		{
			public PersonMap()
			{
				Id(x => x.Id, m =>
				{
					m.Generator(Generators.Native);
					m.Column("id");
				});

                Property(x => x.Guid, p => p.Column("uid"));
				Component(x => x.LegalName, c =>
				{
					c.Property(x => x.FirstName, p => p.Column("first_name"));
					c.Property(x => x.LastName, p => p.Column("last_name"));
					c.Lazy(false);
				});

				List(x => x.Aliases, l =>
				{
					l.Table("aliases");
					l.Key(k =>
					{
						k.Column("person_id");
						k.ForeignKey("fk_alias_person");
						k.NotNullable(true);
					});
					l.Index(i => i.Column("item_no"));
				}, e => e.Component(c =>
				{
					c.Property(x => x.FirstName);
					c.Property(x => x.LastName);
					c.Lazy(false);
				}));

				Lazy(false);
				Table("person");
			}
		}

		#endregion
	}
}
