using NHibernate.Shards.Threading;

namespace NHibernate.Shards.Test.Threading
{
	public class InmediatlyCallable : ICallable<int>
	{
		private int value;

		public InmediatlyCallable(int value)
		{
			this.value = value;
		}

		#region ICallable<int> Members

		public int Call()
		{
			return value;
		}

		#endregion
	}
}