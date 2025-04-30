using Godot;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace traintracks;

public struct NeighborId(int id)
{
    public int Id = id < 0 || id >= 6 ? Mathf.PosMod(id, 6) : id;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static NeighborId operator -(NeighborId self) => new(self.Id + 3);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static NeighborId operator +(NeighborId self, int offset) => new(self.Id + offset);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator int(NeighborId self) => self.Id;

    public override readonly bool Equals([NotNullWhen(true)] object? obj) => obj is NeighborId n && n.Id == Id;
    public override readonly int GetHashCode() => Id;

    public static bool operator ==(NeighborId a, NeighborId b) => a.Id == b.Id;
    public static bool operator !=(NeighborId a, NeighborId b) => a.Id != b.Id;
}
