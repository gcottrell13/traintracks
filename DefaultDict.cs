using System;
using System.Collections.Generic;

namespace traintracks;

public class DefaultDictionary<K, V>(Func<V> defaultValue) where K : notnull
{
    Func<V> defaultValue = defaultValue;
    Dictionary<K, V> values = [];

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

    public void Clear() => values.Clear();
}
