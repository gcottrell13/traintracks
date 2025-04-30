using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace traintracks;

public enum HextileConnectionType
{
    // non-zero so that the default HextileConnection has zero type
    Near = 1,
    Far = 2,
}

public enum HextileConnectionCheck
{
    Height = 1,
    Index = 2,
    Type = 4,
    HI = Height | Index,
    HT = Height | Type,
    IT = Index | Type,
    HIT = Height | Index | Type,
}

public readonly struct HextileConnection(int height, NeighborId index, HextileConnectionType type)
{
    public readonly int Height = height;
    public readonly NeighborId Index = index;
    public readonly HextileConnectionType ConnectionType = type;

    public readonly override int GetHashCode() => (int)ConnectionType + (Index.GetHashCode() << 3) + (Height << 6);
    public readonly override bool Equals([NotNullWhen(true)] object? obj)
        => obj is HextileConnection hc && hc.GetHashCode() == GetHashCode();

    public static bool operator ==(HextileConnection left, HextileConnection right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(HextileConnection left, HextileConnection right)
    {
        return !(left == right);
    }

    public readonly HextileConnection Opposite => new(Height, -Index, ConnectionType);

    public override string ToString() => $"c{ConnectionType}_i{Index}_h{Height}_i{GetHashCode()}";
}

public readonly struct HextileConnectionDouble(HextileConnection one, HextileConnection two)
{
    public readonly HextileConnection One = one;
    public readonly HextileConnection Two = two;

    // these two are used purely to maintain the un-ordered-ness of this struct for equality testing purposes (like in dictionaries)
    private readonly HextileConnection Lesser = one.GetHashCode() < two.GetHashCode() ? one : two;
    private readonly HextileConnection Greater = one.GetHashCode() < two.GetHashCode() ? two : one;

    public readonly override int GetHashCode() => (Lesser.GetHashCode() << 8) + Greater.GetHashCode();

    public override string ToString() => $"{Lesser}V{Greater}h{GetHashCode()}";

    public readonly bool Has(HextileConnection hex, HextileConnectionCheck checkType = HextileConnectionCheck.HIT) 
        => ((checkType & HextileConnectionCheck.Height) == 0 || One.Height == hex.Height || Two.Height == hex.Height) 
        && ((checkType & HextileConnectionCheck.Index) == 0 || One.Index == hex.Index || Two.Index == hex.Index) 
        && ((checkType & HextileConnectionCheck.Type) == 0 || One.ConnectionType == hex.ConnectionType || Two.ConnectionType == hex.ConnectionType);

    public readonly bool HasHeightDifference() => One.Height != Two.Height;
    public readonly int HeightDifference() => Math.Abs(One.Height - Two.Height);

    public readonly override bool Equals([NotNullWhen(true)] object? obj)
     => obj is HextileConnectionDouble hc && hc.GetHashCode() == GetHashCode();

    public static bool operator ==(HextileConnectionDouble left, HextileConnectionDouble right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(HextileConnectionDouble left, HextileConnectionDouble right)
    {
        return !(left == right);
    }

}