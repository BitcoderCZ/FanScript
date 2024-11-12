using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace FanScript.Utils;

internal interface IMultiValueDictionary<TKey, TValue, TCollection>
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
    : IMultiValueDictionary<TKey, TValue, List<TValue>>
    where TKey : notnull
{
    private readonly Dictionary<TKey, List<TValue>> _dict;

    public ListMultiValueDictionary()
    {
        _dict = [];
    }

    public ListMultiValueDictionary(int capacity)
    {
        _dict = new(capacity);
    }

    public ListMultiValueDictionary(IEnumerable<KeyValuePair<TKey, List<TValue>>> collection)
    {
        _dict = new(collection);
    }

    public ListMultiValueDictionary(IDictionary<TKey, List<TValue>> dictionary)
    {
        _dict = new(dictionary);
    }

    public ICollection<TKey> Keys => _dict.Keys;

    public ICollection<List<TValue>> Values => _dict.Values;

    public int Count => _dict.Count;

    bool ICollection<KeyValuePair<TKey, List<TValue>>>.IsReadOnly => false;

    public List<TValue> this[TKey key]
    {
        get => _dict[key];
        set => _dict[key] = value;
    }

    public void Add(TKey key, TValue value)
        => GetValue(key).Add(value);

    public void Add(TKey key, List<TValue> value)
        => _dict.Add(key, value);

    public void AddRange(TKey key, IEnumerable<TValue> value)
        => GetValue(key).AddRange(value);

    public void Clear()
        => _dict.Clear();

    public bool ContainsKey(TKey key)
        => _dict.ContainsKey(key);

    public bool Remove(TKey key)
    => _dict.Remove(key);

    public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out List<TValue> value)
        => _dict.TryGetValue(key, out value);

    public IEnumerator<KeyValuePair<TKey, List<TValue>>> GetEnumerator()
        => _dict.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => _dict.GetEnumerator();

    public List<TValue> GetValue(TKey key)
    {
        if (!_dict.TryGetValue(key, out List<TValue>? list))
        {
            list = [];
            _dict.Add(key, list);
        }

        return list;
    }

    void ICollection<KeyValuePair<TKey, List<TValue>>>.Add(KeyValuePair<TKey, List<TValue>> item)
        => ((ICollection<KeyValuePair<TKey, List<TValue>>>)_dict).Add(item);

    public bool Contains(KeyValuePair<TKey, List<TValue>> item)
        => _dict.Contains(item);

    bool ICollection<KeyValuePair<TKey, List<TValue>>>.Remove(KeyValuePair<TKey, List<TValue>> item)
        => ((ICollection<KeyValuePair<TKey, List<TValue>>>)_dict).Remove(item);

    void ICollection<KeyValuePair<TKey, List<TValue>>>.CopyTo(KeyValuePair<TKey, List<TValue>>[] array, int arrayIndex)
        => ((ICollection<KeyValuePair<TKey, List<TValue>>>)_dict).CopyTo(array, arrayIndex);
}

public sealed class SetMultiValueDictionary<TKey, TValue>
    : IMultiValueDictionary<TKey, TValue, HashSet<TValue>>
    where TKey : notnull
{
    private readonly Dictionary<TKey, HashSet<TValue>> _dict;

    public SetMultiValueDictionary()
    {
        _dict = [];
    }

    public SetMultiValueDictionary(int capacity)
    {
        _dict = new(capacity);
    }

    public SetMultiValueDictionary(IEnumerable<KeyValuePair<TKey, HashSet<TValue>>> collection)
    {
        _dict = new(collection);
    }

    public SetMultiValueDictionary(IDictionary<TKey, HashSet<TValue>> dictionary)
    {
        _dict = new(dictionary);
    }

    public ICollection<TKey> Keys => _dict.Keys;

    public ICollection<HashSet<TValue>> Values => _dict.Values;

    public int Count => _dict.Count;

    bool ICollection<KeyValuePair<TKey, HashSet<TValue>>>.IsReadOnly => false;

    public HashSet<TValue> this[TKey key]
    {
        get => _dict[key];
        set => _dict[key] = value;
    }

    public void Add(TKey key, TValue value)
        => GetValue(key).Add(value);

    public void Add(TKey key, HashSet<TValue> value)
        => _dict.Add(key, value);

    public void AddRange(TKey key, IEnumerable<TValue> value)
        => GetValue(key).UnionWith(value);

    public void Clear()
        => _dict.Clear();

    public bool ContainsKey(TKey key)
        => _dict.ContainsKey(key);

    public bool Remove(TKey key)
    => _dict.Remove(key);

    public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out HashSet<TValue> value)
        => _dict.TryGetValue(key, out value);

    public IEnumerator<KeyValuePair<TKey, HashSet<TValue>>> GetEnumerator()
        => _dict.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => _dict.GetEnumerator();

    public HashSet<TValue> GetValue(TKey key)
    {
        if (!_dict.TryGetValue(key, out HashSet<TValue>? set))
        {
            set = [];
            _dict.Add(key, set);
        }

        return set;
    }

    void ICollection<KeyValuePair<TKey, HashSet<TValue>>>.Add(KeyValuePair<TKey, HashSet<TValue>> item)
        => ((ICollection<KeyValuePair<TKey, HashSet<TValue>>>)_dict).Add(item);

    public bool Contains(KeyValuePair<TKey, HashSet<TValue>> item)
        => _dict.Contains(item);

    bool ICollection<KeyValuePair<TKey, HashSet<TValue>>>.Remove(KeyValuePair<TKey, HashSet<TValue>> item)
        => ((ICollection<KeyValuePair<TKey, HashSet<TValue>>>)_dict).Remove(item);

    void ICollection<KeyValuePair<TKey, HashSet<TValue>>>.CopyTo(KeyValuePair<TKey, HashSet<TValue>>[] array, int arrayIndex)
        => ((ICollection<KeyValuePair<TKey, HashSet<TValue>>>)_dict).CopyTo(array, arrayIndex);
}

#pragma warning disable SA1204 // Static elements should appear before instance elements
public static class MultiValueDictionaryExtensions
#pragma warning restore SA1204
{
    public static ListMultiValueDictionary<TKey, TValue> ToListMultiValueDictionary<T, TKey, TValue>(this IEnumerable<T> collection, Func<T, TKey> keySelector, Func<T, TValue> valueSelector)
        where TKey : notnull
    {
        ListMultiValueDictionary<TKey, TValue> dict = collection.TryGetNonEnumeratedCount(out int collectionCount)
            ? new ListMultiValueDictionary<TKey, TValue>(collectionCount)
            : [];

        foreach (var item in collection)
        {
            dict.Add(keySelector(item), valueSelector(item));
        }

        return dict;
    }

    public static SetMultiValueDictionary<TKey, TValue> ToSetMultiValueDictionary<T, TKey, TValue>(this IEnumerable<T> collection, Func<T, TKey> keySelector, Func<T, TValue> valueSelector)
        where TKey : notnull
    {
        SetMultiValueDictionary<TKey, TValue> dict = collection.TryGetNonEnumeratedCount(out int collectionCount)
            ? new SetMultiValueDictionary<TKey, TValue>(collectionCount)
            : [];

        foreach (var item in collection)
        {
            dict.Add(keySelector(item), valueSelector(item));
        }

        return dict;
    }
}
