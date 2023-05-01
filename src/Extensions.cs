using System;
using System.Collections.Generic;
using System.Linq;

namespace epi.switcher.extron.quantum
{
    internal static class Extensions
    {
        public static IEnumerable<string> TokenizeParams(this string s, char separator = ' ')
        {
            var inQuotes = false;
            return s.Split(c =>
            {
                if (c == '\"')
                    inQuotes = !inQuotes;
                return !inQuotes && c == separator;
            }).Select(t => t.Trim());
        }

        public static IEnumerable<string> Split(this string s, Func<char, bool> controller)
        {
            var n = 0;
            for (var c = 0; c < s.Length; c++)
            {
                if (!controller(s[c])) continue;
                yield return s.Substring(n, c - n);
                n = c + 1;
            }
            yield return s.Substring(n);
        }

        public static string EmptyIfNull(this string s) => s ?? string.Empty;

        public static string DefaultIfNull(this string s, string value) => s ?? value;

        public static string Next(this IEnumerator<string> enumerator) => enumerator.MoveNext() ? enumerator.Current.EmptyIfNull() : string.Empty;

        public static bool NextEquals(this IEnumerator<string> enumerator, string other, StringComparison comparison) => enumerator.Next().Equals(other, comparison);

        public static bool NextContains(this IEnumerator<string> enumerator, string other) => enumerator.Next().Contains(other);

    }
}
