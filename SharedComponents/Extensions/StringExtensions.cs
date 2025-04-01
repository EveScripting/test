using System;

namespace SharedComponents.Extensions
{
    public static class StringExtensions
    {
        /// <summary>
        ///     takes a substring between two anchor strings (or the end of the string if that anchor is null)
        /// </summary>
        /// <param name="this">a string</param>
        /// <param name="from">an optional string to search after</param>
        /// <param name="until">an optional string to search before</param>
        /// <param name="comparison">an optional comparison for the search</param>
        /// <returns>a substring based on the search</returns>
        public static string Substring(this string @this, string from = null, string until = null,
            StringComparison comparison = StringComparison.InvariantCulture)
        {
            var fromLength = (from ?? string.Empty).Length;
            var startIndex = !string.IsNullOrEmpty(from)
                ? @this.IndexOf(from, comparison) + fromLength
                : 0;

            if (startIndex < fromLength) throw new ArgumentException("from: Failed to find an instance of the first anchor");

            var endIndex = !string.IsNullOrEmpty(until)
                ? @this.IndexOf(until, startIndex, comparison)
                : @this.Length;

            if (endIndex < 0) throw new ArgumentException("until: Failed to find an instance of the last anchor");

            var subString = @this.Substring(startIndex, endIndex - startIndex);
            return subString;
        }

        public static bool Contains(this string source, string toCheck, StringComparison comp)
        {
            return source?.IndexOf(toCheck, comp) >= 0;
        }

        public static bool ContainsIgnoreCase(this string source, string toCheck)
        {
            return source?.IndexOf(toCheck, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public static string ReplaceWithoutException(this string source, string oldValue, string newValue)
        {
            try
            {
                if (oldValue == null)
                    return source;

                return source.Replace(oldValue, newValue);
            }
            catch
            {
                return source;
            }
        }
    }

}