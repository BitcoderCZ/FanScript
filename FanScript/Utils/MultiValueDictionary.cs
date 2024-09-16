using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FanScript.Utils
{
    public sealed class MultiValueDictionary<TKey, TValue>
        : IDictionary<TKey, List<TValue>> where TKey : notnull
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

        public MultiValueDictionary()
        {
            dict = new Dictionary<TKey, List<TValue>>();
        }
        public MultiValueDictionary(int capacity)
        {
            dict = new Dictionary<TKey, List<TValue>>(capacity);
        }
        public MultiValueDictionary(IEnumerable<KeyValuePair<TKey, List<TValue>>> collection)
        {
            dict = new Dictionary<TKey, List<TValue>>(collection);
        }
        public MultiValueDictionary(IDictionary<TKey, List<TValue>> dictionary)
        {
            dict = new Dictionary<TKey, List<TValue>>(dictionary);
        }

        public void Add(TKey key, TValue value)
            => getValue(key).Add(value);
        public void Add(TKey key, List<TValue> value)
            => dict.Add(key, value);
        public void AddRange(TKey key, IEnumerable<TValue> value)
            => getValue(key).AddRange(value);

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

        private List<TValue> getValue(TKey key)
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

    public static class MultiValueDictionaryExtensions
    {
        public static MultiValueDictionary<TKey, TValue> ToMultiValueDictionary<T, TKey, TValue>(this IEnumerable<T> collection, Func<T, TKey> keySelector, Func<T, TValue> valueSelector)
            where TKey : notnull
        {
            MultiValueDictionary<TKey, TValue> dict;
            if (collection.TryGetNonEnumeratedCount(out int collectionCount))
                dict = new MultiValueDictionary<TKey, TValue>(collectionCount);
            else
                dict = new MultiValueDictionary<TKey, TValue>();

            foreach (var item in collection)
                dict.Add(keySelector(item), valueSelector(item));

            return dict;
        }
    }
}
