using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace FanScript.Utils
{
    public interface IMultiValueDictionary<TKey, TValue, TCollection>
        : IDictionary<TKey, TCollection>
        where TKey : notnull
        where TCollection : ICollection<TValue>
    {
        void Add(TKey key, TValue value);
        void AddRange(TKey key, IEnumerable<TValue> value);

        /// <summary>
        /// If <paramref name="key"/> is already present, returns the associated value, if not, adds a new value and returns it
        /// </summary>
        /// <param name="key"></param>
        /// <returns>The value associated with <paramref name="key"/></returns>
        TCollection GetValue(TKey key);
    }

    public sealed class ListMultiValueDictionary<TKey, TValue>
        : IMultiValueDictionary<TKey, TValue, List<TValue>> where TKey : notnull
    {
        private readonly Dictionary<TKey, List<TValue>> dict;

        public List<TValue> this[TKey key]
        {
            get => dict[key];
            set => dict[key] = value;
        }

        public ICollection<TKey> Keys => dict.Keys;

        public ICollection<List<TValue>> Values => dict.Values;

        public int Count => dict.Count;

        bool ICollection<KeyValuePair<TKey, List<TValue>>>.IsReadOnly => false;

        public ListMultiValueDictionary()
        {
            dict = new();
        }
        public ListMultiValueDictionary(int capacity)
        {
            dict = new(capacity);
        }
        public ListMultiValueDictionary(IEnumerable<KeyValuePair<TKey, List<TValue>>> collection)
        {
            dict = new(collection);
        }
        public ListMultiValueDictionary(IDictionary<TKey, List<TValue>> dictionary)
        {
            dict = new(dictionary);
        }

        public void Add(TKey key, TValue value)
            => GetValue(key).Add(value);
        public void Add(TKey key, List<TValue> value)
            => dict.Add(key, value);
        public void AddRange(TKey key, IEnumerable<TValue> value)
            => GetValue(key).AddRange(value);

        public void Clear()
            => dict.Clear();

        public bool ContainsKey(TKey key)
            => dict.ContainsKey(key);

        public bool Remove(TKey key)
        => dict.Remove(key);

        public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out List<TValue> value)
            => dict.TryGetValue(key, out value);

        public IEnumerator<KeyValuePair<TKey, List<TValue>>> GetEnumerator()
            => dict.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
            => dict.GetEnumerator();

        public List<TValue> GetValue(TKey key)
        {
            if (!dict.TryGetValue(key, out List<TValue>? list))
            {
                list = new List<TValue>();
                dict.Add(key, list);
            }

            return list;
        }

        void ICollection<KeyValuePair<TKey, List<TValue>>>.Add(KeyValuePair<TKey, List<TValue>> item)
            => ((ICollection<KeyValuePair<TKey, List<TValue>>>)dict).Add(item);

        public bool Contains(KeyValuePair<TKey, List<TValue>> item)
            => dict.Contains(item);

        bool ICollection<KeyValuePair<TKey, List<TValue>>>.Remove(KeyValuePair<TKey, List<TValue>> item)
            => ((ICollection<KeyValuePair<TKey, List<TValue>>>)dict).Remove(item);

        void ICollection<KeyValuePair<TKey, List<TValue>>>.CopyTo(KeyValuePair<TKey, List<TValue>>[] array, int arrayIndex)
            => ((ICollection<KeyValuePair<TKey, List<TValue>>>)dict).CopyTo(array, arrayIndex);
    }

    public sealed class SetMultiValueDictionary<TKey, TValue>
        : IMultiValueDictionary<TKey, TValue, HashSet<TValue>> where TKey : notnull
    {
        private readonly Dictionary<TKey, HashSet<TValue>> dict;

        public HashSet<TValue> this[TKey key]
        {
            get => dict[key];
            set => dict[key] = value;
        }

        public ICollection<TKey> Keys => dict.Keys;

        public ICollection<HashSet<TValue>> Values => dict.Values;

        public int Count => dict.Count;

        bool ICollection<KeyValuePair<TKey, HashSet<TValue>>>.IsReadOnly => false;

        public SetMultiValueDictionary()
        {
            dict = new();
        }
        public SetMultiValueDictionary(int capacity)
        {
            dict = new(capacity);
        }
        public SetMultiValueDictionary(IEnumerable<KeyValuePair<TKey, HashSet<TValue>>> collection)
        {
            dict = new(collection);
        }
        public SetMultiValueDictionary(IDictionary<TKey, HashSet<TValue>> dictionary)
        {
            dict = new(dictionary);
        }

        public void Add(TKey key, TValue value)
            => GetValue(key).Add(value);
        public void Add(TKey key, HashSet<TValue> value)
            => dict.Add(key, value);
        public void AddRange(TKey key, IEnumerable<TValue> value)
            => GetValue(key).UnionWith(value);

        public void Clear()
            => dict.Clear();

        public bool ContainsKey(TKey key)
            => dict.ContainsKey(key);

        public bool Remove(TKey key)
        => dict.Remove(key);

        public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out HashSet<TValue> value)
            => dict.TryGetValue(key, out value);

        public IEnumerator<KeyValuePair<TKey, HashSet<TValue>>> GetEnumerator()
            => dict.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
            => dict.GetEnumerator();

        public HashSet<TValue> GetValue(TKey key)
        {
            if (!dict.TryGetValue(key, out HashSet<TValue>? set))
            {
                set = new HashSet<TValue>();
                dict.Add(key, set);
            }

            return set;
        }

        void ICollection<KeyValuePair<TKey, HashSet<TValue>>>.Add(KeyValuePair<TKey, HashSet<TValue>> item)
            => ((ICollection<KeyValuePair<TKey, HashSet<TValue>>>)dict).Add(item);

        public bool Contains(KeyValuePair<TKey, HashSet<TValue>> item)
            => dict.Contains(item);

        bool ICollection<KeyValuePair<TKey, HashSet<TValue>>>.Remove(KeyValuePair<TKey, HashSet<TValue>> item)
            => ((ICollection<KeyValuePair<TKey, HashSet<TValue>>>)dict).Remove(item);

        void ICollection<KeyValuePair<TKey, HashSet<TValue>>>.CopyTo(KeyValuePair<TKey, HashSet<TValue>>[] array, int arrayIndex)
            => ((ICollection<KeyValuePair<TKey, HashSet<TValue>>>)dict).CopyTo(array, arrayIndex);
    }

    public static class MultiValueDictionaryExtensions
    {
        public static ListMultiValueDictionary<TKey, TValue> ToListMultiValueDictionary<T, TKey, TValue>(this IEnumerable<T> collection, Func<T, TKey> keySelector, Func<T, TValue> valueSelector)
            where TKey : notnull
        {
            ListMultiValueDictionary<TKey, TValue> dict;
            if (collection.TryGetNonEnumeratedCount(out int collectionCount))
                dict = new ListMultiValueDictionary<TKey, TValue>(collectionCount);
            else
                dict = new ListMultiValueDictionary<TKey, TValue>();

            foreach (var item in collection)
                dict.Add(keySelector(item), valueSelector(item));

            return dict;
        }

        public static SetMultiValueDictionary<TKey, TValue> ToSetMultiValueDictionary<T, TKey, TValue>(this IEnumerable<T> collection, Func<T, TKey> keySelector, Func<T, TValue> valueSelector)
            where TKey : notnull
        {
            SetMultiValueDictionary<TKey, TValue> dict;
            if (collection.TryGetNonEnumeratedCount(out int collectionCount))
                dict = new SetMultiValueDictionary<TKey, TValue>(collectionCount);
            else
                dict = new SetMultiValueDictionary<TKey, TValue>();

            foreach (var item in collection)
                dict.Add(keySelector(item), valueSelector(item));

            return dict;
        }
    }
}
