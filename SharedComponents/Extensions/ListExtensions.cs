using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace SharedComponents.Extensions
{
    public static class ListExtensions
    {
        private static Random random = new Random();

        public static IEnumerable<T> RandomPermutation<T>(this IEnumerable<T> sequence)
        {
            var retArray = sequence.ToArray();


            for (var i = 0; i < retArray.Length - 1; i += 1)
            {
                var swapIndex = random.Next(i, retArray.Length);
                if (swapIndex != i)
                {
                    var temp = retArray[i];
                    retArray[i] = retArray[swapIndex];
                    retArray[swapIndex] = temp;
                }
            }

            return retArray;
        }

        public static IList OfTypeToList(this IEnumerable source, Type type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));
            return
                (IList)Activator.CreateInstance(
                    typeof(List<>)
                        .MakeGenericType(type),
                    typeof(System.Linq.Enumerable)
                        .GetMethod(nameof(System.Linq.Enumerable.OfType),
                            BindingFlags.Static | BindingFlags.Public)
                        .MakeGenericMethod(type)
                        .Invoke(null, new object[] { source }));
        }

        public static IEnumerable<object> DyamicOfType<T>(
            this IEnumerable<T> input, Type type)
        {
            var ofType = typeof(Queryable).GetMethod("OfType",
                BindingFlags.Static | BindingFlags.Public);
            var ofTypeT = ofType.MakeGenericMethod(type);
            return (IEnumerable<object>)ofTypeT.Invoke(null, new object[] { input });
        }
        private static readonly Random rng = new Random();
        /// <summary>
        /// Get a random element from a enumerable
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="enumerable"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static T Random<T>(this IEnumerable<T> enumerable)
        {
            if (enumerable == null)
            {
                throw new ArgumentNullException(nameof(enumerable));
            }
            var list = enumerable as IList<T> ?? enumerable.ToList();
            return list.Count == 0 ? default(T) : list[rng.Next(0, list.Count)];
        }
    }
}