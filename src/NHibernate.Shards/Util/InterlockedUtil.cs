namespace NHibernate.Shards.Util
{
    using System.Threading;

    public static class InterlockedUtil
    {
        public static int Add(ref int location, int delta)
        {
            int currentValue = location;
            int targetValue;
            do
            {
                targetValue = currentValue + delta;
                currentValue = Interlocked.CompareExchange(ref location, targetValue, currentValue);
            } while (currentValue != targetValue);
            return currentValue;
        }
    }
}
