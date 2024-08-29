using System.Collections.Immutable;

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

        public static IEnumerable<TResult> Zip<TFirst, TSecond, TResult>(this IEnumerable<TFirst> first, IEnumerable<TSecond> second, Func<TFirst, TSecond, (TResult, bool, bool)> selector)
        {
            var firstEnumerator = first.GetEnumerator();
            var secondEnumerator = second.GetEnumerator();

            bool moveFirst = true;
            bool moveSecond = true;

            while ((!moveFirst || firstEnumerator.MoveNext()) && (!moveSecond || secondEnumerator.MoveNext()))
            {
                (TResult result, moveFirst, moveSecond) = selector(firstEnumerator.Current, secondEnumerator.Current);

                yield return result;
            }
        }

        public static ReadOnlyMemory<T> AsMemory<T>(this ImmutableArray<T> array, Range range)
        {
            var (start, length) = range.GetOffsetAndLength(array.Length);
            return array.AsMemory().Slice(start, length);
        }

        public static void AddMultiValue<TKey, TValue>(this IDictionary<TKey, List<TValue>> dics, TKey key, TValue value)
        {
            if (!dics.TryGetValue(key, out var list))
            {
                list = new List<TValue>();
                dics.Add(key, list);
            }

            list.Add(value);
        }
    }
}
