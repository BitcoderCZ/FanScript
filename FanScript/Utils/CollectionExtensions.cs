using System.Collections.Immutable;
using System.Runtime.CompilerServices;

namespace FanScript.Utils
{
    internal static class CollectionExtensions
    {
        public static TValue AddIfAbsent<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key, TValue defaultValue)
        {
            if (!dict.TryGetValue(key, out TValue? val))
            {
                val = defaultValue;
                dict.Add(key, val);
            }

            return val;
        }

        public static ReadOnlyMemory<T> AsMemory<T>(this ImmutableArray<T> array, Range range)
        {
            var (start, length) = range.GetOffsetAndLength(array.Length);
            return array.AsMemory().Slice(start, length);
        }

        public static IEnumerable<T> GetDuplicates<T>(this IEnumerable<T> collection)
            => collection
                .GroupBy(x => x)
                .Where(g => g.Count() > 1)
                .Select(y => y.Key);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ReadOnlySpan<T> Slice<T>(this ReadOnlySpan<T> span, Range range)
        {
            var (index, length) = range.GetOffsetAndLength(span.Length);
            return span.Slice(index, length);
        }
    }
}
