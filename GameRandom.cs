using Godot;
using System.Collections.Generic;

namespace traintracks;

public static class GameRandom
{
    public readonly static RandomNumberGenerator Rng = new();

    public static T RandomChoice<T>(this IList<T> collection)
    {
        return collection[Rng.RandiRange(0, collection.Count - 1)];
    }

    public static T PopRandom<T>(this IList<T> collection)
    {
        var index = Rng.RandiRange(0, collection.Count - 1);
        var value = collection[index];
        collection.RemoveAt(index);
        return value;
    }
}
