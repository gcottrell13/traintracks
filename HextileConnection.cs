using System;
using System.Diagnostics.CodeAnalysis;

namespace traintracks;

public enum HextileConnectionType
{
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

public readonly struct HextileConnection(byte height, byte index, HextileConnectionType type)
{
    public readonly byte Height = height;
    public readonly byte Index = index;
    public readonly HextileConnectionType ConnectionType = type;
    public readonly override int GetHashCode() => (int)ConnectionType + Index << 2 + Height << 4;
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
}

public readonly struct HextileConnectionDouble
{
    public readonly HextileConnection One;
    public readonly HextileConnection Two;

    public HextileConnectionDouble(HextileConnection one, HextileConnection two)
    {
        (One, Two) = one.GetHashCode() < two.GetHashCode() ? (One, Two) : (Two, One);
    }

    public readonly override int GetHashCode()
    {
        return One.GetHashCode() << 8 + Two.GetHashCode();
    }

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