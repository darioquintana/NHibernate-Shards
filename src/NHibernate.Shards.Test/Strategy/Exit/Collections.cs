using System;
using System.Collections.Generic;
using System.Linq;

namespace NHibernate.Shards.Test.Strategy.Exit
{
    public class Collections
    {
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="listToRandom"></param>
        /// <param name="rnd"></param>
        /// <returns></returns>
        public static IList<T> RandomList<T>(IEnumerable<T> listToRandom, Random rnd)
        {
            T[] arr = listToRandom.ToArray();
            for (int i = arr.Length; i > 1; i--)
            {
                Swap(arr, i - 1, rnd.Next(i));
            }
            return arr;
        }

        /// <summary>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="listToRandom"></param>
        /// <returns></returns>
        public static IList<T> RandomList<T>(ICollection<T> listToRandom)
        {
            return RandomList(listToRandom, new Random());
        }

        private static void Swap<T>(T[] arr, int i, int j)
        {
            T tmp = arr[i];
            arr[i] = arr[j];
            arr[j] = tmp;
        }
    }
}