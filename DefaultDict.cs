using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
namespace traintracks;

public class DefaultDictionary<K, V>(Func<V> defaultValue) : IReadOnlyDictionary<K, V> where K : notnull
{
    readonly Func<V> defaultValue = defaultValue;
    readonly Dictionary<K, V> values = [];

    public V this[K i]
    {
        get {
            if (!values.TryGetValue(i, out var value))
            {
                value = defaultValue();
                this[i] = value;
            }
            return value;
        }
        set => values[i] = value;
    }

    public IEnumerable<K> Keys => values.Keys;

    public IEnumerable<V> Values => values.Values;

    public int Count => values.Count;

    public void Clear() => values.Clear();

    bool IReadOnlyDictionary<K, V>.ContainsKey(K key)
    {
        return values.ContainsKey(key);
    }

    bool IReadOnlyDictionary<K, V>.TryGetValue(K key, [MaybeNullWhen(false)] out V value)
    {
        // this should not modify the underlying dictionary
        return values.TryGetValue(key, out value);
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return ((IEnumerable)values).GetEnumerator();
    }

    IEnumerator<KeyValuePair<K, V>> IEnumerable<KeyValuePair<K, V>>.GetEnumerator()
    {
        return values.GetEnumerator();
    }
}
