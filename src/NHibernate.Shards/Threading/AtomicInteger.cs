using System.Threading;

namespace NHibernate.Shards.Threading
{
	/// <summary>
	/// An <c>int</c> value that may be updated atomically
	/// </summary>
	public class AtomicInteger
	{
		private int _value;

		/// <summary>
		/// Construct a atomic integer
		/// </summary>
		public AtomicInteger() : this(0)
		{
		}

		/// <summary>
		/// Construct a atomic integer
		/// </summary>
		/// <param name="value">initial value</param>
		public AtomicInteger(int value)
		{
			_value = value;
		}

		/// <summary>
		/// Get the current integer
		/// </summary>
		public int Value
		{
			get { return Thread.VolatileRead(ref _value); }
			set { Thread.VolatileWrite(ref _value, value); }
		}

		/// <summary>
		/// Atomically increments by one the current value.
		/// </summary>
		/// <returns>Return the incremented value.</returns>
		public int IncrementAndGet()
		{
			return Interlocked.Increment(ref _value);
		}

		/// <summary>
		/// Atomically decrements by one the current value.
		/// </summary>
		/// <returns>Return the decremented value.</returns>
		public int DecrementAndGet()
		{
			return Interlocked.Decrement(ref _value);
		}

		/// <summary>
		/// Atomically increments by one the current value.
		/// </summary>
		/// <returns>Return the past value.</returns>
		public int GetAndIncrement()
		{
			int old;
			do
			{
				old = Thread.VolatileRead(ref _value);
			} while (old != Interlocked.CompareExchange(ref _value, old + 1, old));
			return old;
		}

		/// <summary>
		/// Atomically decrements by one the current value.
		/// </summary>
		/// <returns>Return the past value.</returns>
		public int GetAndDecrement()
		{
			int old;
			do
			{
				old = Thread.VolatileRead(ref _value);
			} while (old != Interlocked.CompareExchange(ref _value, old - 1, old));
			return old;
		}

		/// <summary>
		/// Atomically sets to the given value and returns the old value.
		/// </summary>
		/// <param name="newValue">the new value</param>
		/// <returns>the past value</returns>
		public int GetAndSet(int newValue)
		{
			int old;
			do
			{
				old = Thread.VolatileRead(ref _value);
			} while (old != Interlocked.CompareExchange(ref _value, newValue, old));
			return old;
		}

		public override string ToString()
		{
			return Value.ToString();
		}
	}
}