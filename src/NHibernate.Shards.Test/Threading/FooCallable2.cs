using System.Diagnostics;
using System.Threading;
using NUnit.Framework;

namespace NHibernate.Shards.Test.Threading
{
	public class FooCallable2 : OperationCallable<string>
	{
		public FooCallable2() : base(5000, "FooCallable2")
		{
		}

		public override string Call()
		{
			try
			{
				Debug.WriteLine("comenz�");
				Thread.Sleep(millis);
				Debug.WriteLine("termin�");
				Assert.Fail("A exception must be throw before");
			}
			catch(ThreadInterruptedException)
			{
			}
			return default(string);
		}
	}
}