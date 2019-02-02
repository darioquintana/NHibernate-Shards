using System;
using System.Collections.Generic;
using System.Linq;
using NHibernate.Shards.Util;

namespace NHibernate.Shards.Strategy.Exit
{

    /// <summary>
	/// Thread-safe ExitStrategy that concatenates all the lists that are added.
	/// </summary>
	public class ListExitStrategy<T> : IListExitStrategy<T>
	{
		// List instance to which final results are added
		private IEnumerable<T> result;

		private readonly IExitOperationFactory exitOperationFactory;

		public ListExitStrategy(IExitOperationFactory exitOperationFactory)
		{
			Preconditions.CheckNotNull(exitOperationFactory);
			this.exitOperationFactory = exitOperationFactory;
		}

		/// <summary>
		/// Add the provided result and return whether or not the caller can halt
		/// processing.
		/// </summary>
		/// <param name="partialResult">The result to add</param>
		/// <param name="shard"></param>
		/// <returns>Whether or not the caller can halt processing</returns>
		public bool AddResult(IEnumerable<T> partialResult, IShard shard)
		{
			Preconditions.CheckNotNull(partialResult);

			result = result == null
				? partialResult
				: result.Concat(partialResult);
			return false;
		}

		public IEnumerable<T> CompileResults()
		{
		    var exitOperation = this.exitOperationFactory.CreateExitOperation();
			return exitOperation.Execute(result ?? Enumerable.Empty<T>());
		}
	}
}