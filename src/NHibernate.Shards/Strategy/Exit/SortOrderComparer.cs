using System;
using System.Collections;
using System.Collections.Generic;

namespace NHibernate.Shards.Strategy.Exit
{
    public class SortOrderComparer : IComparer<object>, IComparer
    {
        private readonly IList<SortOrder> orders;

        public SortOrderComparer(IEnumerable<SortOrder> orders)
        {
            if (orders == null)
            {
                throw new ArgumentNullException("orders");
            }

            this.orders = new List<SortOrder>(orders);

            if (this.orders.Count <= 0)
            {
                throw new ArgumentException("One or more sort orders required.", "orders");
            }
        }

        #region IComparer<object> Members

        ///<summary>
        ///Compares two objects and returns a value indicating whether one is less than, equal to, or greater than the other.
        ///</summary>
        ///<returns>
        ///Value Condition Less than zero x is less than y. Zero x equals y. Greater than zero x is greater than y. 
        ///</returns>
        ///<param name="y">The second object to compare. </param>
        ///<param name="x">The first object to compare. </param>
        public int Compare(object x, object y)
        {
            if (!Equals(x, y))
            {
                foreach (var order in this.orders)
                {
                    IComparable xValue = x != null
                        ? ListExitOperationUtils.GetPropertyValue(x, order.PropertyName)
                        : null;
                    IComparable yValue = y != null
                        ? ListExitOperationUtils.GetPropertyValue(y, order.PropertyName)
                        : null;

                    int result;
                    if (xValue == null)
                    {
                        result = yValue == null ? 0 : -1;
                    }
                    else
                    {
                        result = yValue == null ? 1 : xValue.CompareTo(yValue);
                    }

                    if (result != 0)
                    {
                        return order.IsDescending ? -result : result;
                    }
                }
            }
            
            return 0;
        }

        #endregion
    }
}
