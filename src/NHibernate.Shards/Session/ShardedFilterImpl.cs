using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NHibernate.Engine;
using NHibernate.Shards.Engine;
using NHibernate.Shards.Util;

namespace NHibernate.Shards.Session
{
    public class ShardedFilterImpl: IShardedFilter
    {
        #region Instance fields

        private readonly IShardedSessionImplementor shardedSession;
        private readonly string name;

        private readonly IDictionary<ISession, IFilter> enabledFiltersBySession = new Dictionary<ISession, IFilter>();
        private readonly ICollection<Action<IFilter>> enableActions = new List<Action<IFilter>>();

        #endregion

        #region Ctor

        public ShardedFilterImpl(IShardedSessionImplementor shardedSession, string name)
		{
            Preconditions.CheckNotNull(shardedSession);
            Preconditions.CheckNotNull(name);
            this.shardedSession = shardedSession;
            this.name = name;
		}

        #endregion

        #region Properties

        private IFilter SomeFilter
        {
            get
            {
                return this.enabledFiltersBySession.Values.FirstOrDefault()
                    ?? EnableFor(shardedSession.AnyShard.EstablishSession());
            }
        }

        #endregion

        #region Public methods

        public IFilter EnableFor(ISession session)
        {
            IFilter result;
            if (!this.enabledFiltersBySession.TryGetValue(session, out result))
            {
                result = session.EnableFilter(this.name);
                foreach (var action in enableActions)
                {
                    action(result);
                }
                enabledFiltersBySession.Add(session, result);
            }
            return result;
        }

        public void Disable()
        {
            foreach (var session in this.enabledFiltersBySession.Keys)
            {
                session.DisableFilter(this.name);
            }
            this.enabledFiltersBySession.Clear();
            this.enableActions.Clear();
        }

        #endregion

        #region IFilter Members

        public FilterDefinition FilterDefinition
        {
            get { return SomeFilter.FilterDefinition; }
        }

        public string Name
        {
            get { return this.name; }
        }

        public IFilter SetParameter(string name, object value)
        {
            ApplyActionToShards(f => f.SetParameter(name, value));
            return this;
        }

        public IFilter SetParameterList(string name, object[] values)
        {
            ApplyActionToShards(f => f.SetParameterList(name, values));
            return this;
        }

        public IFilter SetParameterList(string name, ICollection values)
        {
            ApplyActionToShards(f => f.SetParameterList(name, values));
            return this;
        }

        /// <summary>
        /// Perform validation of the filter state.  This is used to verify the
        /// state of the filter after its enablement and before its use.
        /// </summary>
        public void Validate()
        {
            ApplyActionToShards(f => f.Validate());
        }

        #endregion

        #region Private methods

        protected void ApplyActionToShards(Action<IFilter> action)
        {
            enableActions.Add(action);
            foreach (var query in this.enabledFiltersBySession.Values)
            {
                action(query);
            }
        }

        #endregion
    }
}
