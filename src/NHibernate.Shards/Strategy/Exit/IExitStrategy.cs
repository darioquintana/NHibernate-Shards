namespace NHibernate.Shards.Strategy.Exit
{
    /// <summary>
    /// Classes implementing this interface gather results from operations that are
    /// executed across shards. 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IExitStrategy<T>
    {
        /// <summary>
        /// Add the provided result and return whether or not the caller can halt
        /// processing.
        /// </summary>
        /// <param name="result">The result to add</param>
        /// <param name="shard">The shard from which the result was obtained.</param>
        /// <returns>Whether or not the caller can halt processing.</returns>
        bool AddResult(T result, IShard shard);

        /// <summary>
        /// Transforms the received results from individual shards into the final 
        /// operation result.
        /// </summary>
        /// <returns></returns>
        T CompileResults();
    }
}