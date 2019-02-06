namespace NHibernate.Shards.Criteria
{
	using System;
	using System.Reflection;
	using NHibernate.Criterion;
	using NHibernate.Impl;
	using NHibernate.Shards.Engine;
	using NHibernate.Shards.Session;
	using NHibernate.Shards.Util;

	public static class DetachedCriteriaExtensions
	{
		// ReSharper disable once PossibleNullReferenceException
		private static readonly Func<DetachedCriteria, CriteriaImpl> GetCriteriaImpl = (Func<DetachedCriteria, CriteriaImpl>)typeof(DetachedCriteria)
				.GetMethod(nameof(GetCriteriaImpl), BindingFlags.Instance | BindingFlags.NonPublic)
				.CreateDelegate(typeof(Func<DetachedCriteria, CriteriaImpl>));

		public static ICriteria GetExecutableCriteria(this DetachedCriteria criteria, IShardedSession shardedSession)
		{
			Preconditions.CheckNotNull(criteria);
			Preconditions.CheckNotNull(shardedSession);

			return ToShardedCriteria(GetCriteriaImpl(criteria), (IShardedSessionImplementor)shardedSession);
		}

		internal static ICriteria GetExecutableCriteria(this DetachedCriteria criteria, IShardedSessionImplementor shardedSession)
		{
			Preconditions.CheckNotNull(criteria);
			Preconditions.CheckNotNull(shardedSession);
			return ToShardedCriteria(GetCriteriaImpl(criteria), shardedSession);
		}

		private static ICriteria ToShardedCriteria(CriteriaImpl other, IShardedSessionImplementor shardedSession)
		{
			Preconditions.CheckNotNull(other);
			Preconditions.CheckNotNull(shardedSession);

			var entityName = other.EntityOrClassName;
			var alias = other.Alias;

			ICriteria CriteriaFactory(ISession s) =>
				alias != null
					? s.CreateCriteria(entityName, alias)
					: s.CreateCriteria(entityName);

			var result = new ShardedCriteriaImpl(shardedSession, other.EntityOrClassName, CriteriaFactory);

			foreach (var entry in other.IterateSubcriteria())
			{
				result.CreateCriteria(entry.Path, entry.Alias, entry.JoinType, entry.WithClause);
			}
			foreach (var entry in other.IterateExpressionEntries())
			{
				result.Add(entry.Criterion);
			}
			foreach (var entry in other.LockModes)
			{
				result.SetLockMode(entry.Key, entry.Value);
			}
			foreach (var entry in other.IterateOrderings())
			{
				result.AddOrder(entry.Order);
			}

			if (other.Cacheable)
			{
				result.SetCacheable(true);
				if (other.CacheMode != null) result.SetCacheMode(other.CacheMode.Value);
				if (other.CacheRegion != null) result.SetCacheRegion(other.CacheRegion);
			}

			if (other.Comment != null) result.SetComment(other.Comment);
			if (other.FetchSize > 0) result.SetFetchSize(other.FetchSize);
			if (other.FirstResult > 0) result.SetFirstResult(other.FirstResult);
			if (other.MaxResults > 0) result.SetMaxResults(other.MaxResults);
			if (other.Projection != null) result.SetProjection(other.Projection);
			if (other.ResultTransformer != null) result.SetResultTransformer(other.ResultTransformer);
			if (other.Timeout > 0) result.SetTimeout(other.Timeout);
			if (other.IsReadOnlyInitialized) result.SetReadOnly(other.IsReadOnly);

			return result;
		}
	}
}
