using System;
using System.Collections;
using System.Globalization;
using System.Linq;

namespace NHibernate.Shards.Strategy.Exit
{
    public static class AggregationUtil
    {
        public static AggregationFunc GetSumFunc(System.Type operandType)
        {
            switch (System.Type.GetTypeCode(operandType))
            {
                case TypeCode.SByte:
                    return c => (sbyte)c.Cast<object>().Sum(o => Convert.ToInt32(o));
                case TypeCode.Byte:
                    return c => (byte)c.Cast<object>().Sum(o => Convert.ToInt32(o));
                case TypeCode.Int16:
                    return c => (short)c.Cast<object>().Sum(o => Convert.ToInt32(o));
                case TypeCode.UInt16:
                    return c => (ushort)c.Cast<object>().Sum(o => Convert.ToInt32(o));
                case TypeCode.Int32:
                    return c => c.Cast<object>().Sum(o => Convert.ToInt32(o));
                case TypeCode.UInt32:
                    return c => (uint)c.Cast<object>().Sum(o => Convert.ToInt32(o));
                case TypeCode.Int64:
                    return c => c.Cast<object>().Sum(o => Convert.ToInt64(o));
                case TypeCode.UInt64:
                    return c => (ulong)c.Cast<object>().Sum(o => Convert.ToDecimal(o));
                case TypeCode.Decimal:
                    return c => c.Cast<object>().Sum(o => Convert.ToDecimal(o));
                case TypeCode.Double:
                    return c => c.Cast<object>().Sum(o => Convert.ToDouble(o));
                case TypeCode.Single:
                    return c => c.Cast<object>().Sum(o => Convert.ToSingle(o));
                default:
                    string message = string.Format(
                        CultureInfo.InvariantCulture,
                        "Calculation of sums is not supported for operands of type '{0}'.",
                        operandType.Name);
                    throw new NotSupportedException(message);
            }
        }

        public static object Min(this IEnumerable items)
        {
            IComparable result = null;
            foreach (var item in items)
            {
                if (result == null)
                {
                    result = item as IComparable;
                }
                else if (item != null)
                {
                    if (result.CompareTo(item) > 0)
                    {
                        result = (IComparable)item;
                    }
                }
            }
            return result;
        }

        public static object Max(this IEnumerable items)
        {
            IComparable result = null;
            foreach (var item in items)
            {
                if (result == null)
                {
                    result = item as IComparable;
                }
                else if (item != null)
                {
                    if (result.CompareTo(item) < 0)
                    {
                        result = (IComparable)item;
                    }
                }
            }
            return result;
        }

        public static object Average(this IEnumerable items, Func<object, double?> avgSelector, Func<object, int?> countSelector)
        {
            double sumTotal = 0;
            double countTotal = 0;

            foreach (var result in items)
            {
                var avg = avgSelector(result);
                var count = countSelector(result);
                if (avg.HasValue && count.HasValue)
                {
                    sumTotal += avg.Value * count.Value;
                    countTotal += count.Value;
                }
            }

            return countTotal > 0
                ? sumTotal / countTotal
                : default(double?);
        }
    }
}
