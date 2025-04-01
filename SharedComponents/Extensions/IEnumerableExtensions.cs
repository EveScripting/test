/*
 * Created by SharpDevelop.
 * User: duketwo
 * Date: 23.10.2016
 * Time: 22:07
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */

using System;
using System.Collections.Generic;

namespace SharedComponents.Extensions
{
    /// <summary>
    ///     Description of IEnumerableExtensions.
    /// </summary>
    public static class IEnumerableExtensions
    {
        public static IEnumerable<TSource> DistinctBy<TSource, TKey>
            (this IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
        {
            var seenKeys = new HashSet<TKey>();
            foreach (var element in source)
                if (seenKeys.Add(keySelector(element)))
                    yield return element;
        }

        public static IEnumerable<TSource> ForEachInLine<TSource>
            (this IEnumerable<TSource> source, Action<TSource> action)
        {
            foreach (var element in source)
            {
                action(element);
                yield return element;
            }
        }

        public static IEnumerable<TSource> When<TSource>(this IEnumerable<TSource> source, bool condition, Func<IEnumerable<TSource>, IEnumerable<TSource>> func)
        {
            if (!condition)
                return source;

            return func(source);

        }
    }
}