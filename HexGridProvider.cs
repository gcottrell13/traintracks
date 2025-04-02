using Godot;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;

namespace traintracks;

public enum GridType
{
    // points in x direction
    /**
     * 
     * 
      __
   __/  \__
  /  \__/  \
  \__/  \__/
  /  \__/  \
  \__/  \__/
     \__/
 
     * 
     * 
     */
    OffsetEvenX = 1,

    /**
     * 
     * 
   __    __
  /  \__/  \
  \__/  \__/
  /  \__/  \
  \__/  \__/
  /  \__/  \
  \__/  \__/
     * 
     */
    OffsetOddX = 2,

    // points in y direction
    OffsetEvenY = 4,
    OffsetOddY = 8,

    FlatTop = OffsetOddX | OffsetEvenX,
    PointyTop = OffsetOddY | OffsetEvenY,
}

[GlobalClass]
[Tool]
public partial class HexGridProvider : Resource
{
    Transform3D t = Transform3D.Identity;
    float s;
    GridType g = GridType.OffsetEvenX;

    [Export]
    public Transform3D Transform { get => t; set { t = value; EmitChanged(); } }

    [Export]
    public GridType GridType { get => g; set { g = value; EmitChanged(); } }

    private static readonly float sqrt_3 = (float)Math.Sqrt(3);

    public Transform3D GetGridTransform(float size, int x, int y)
    {
        if ((GridType & GridType.FlatTop) != 0) return FlatTop(size, x, y);
        return PointyTop(size, x, y);
    }

    private Transform3D FlatTop(float size, int x, int y)
    {
        var horiz = size * 3 / 2;
        var vert = size * sqrt_3;
        var xx = x * horiz;
        var yy = y * vert;
        if ((GridType == GridType.OffsetEvenX && x % 2 == 0) || (GridType == GridType.OffsetOddX && x % 2 == 1))
            yy += vert / 2;

        return Transform.TranslatedLocal(new Vector3(xx, 0, yy));
    }

    private Transform3D PointyTop(float size, int x, int y)
    {
        var horiz = size * sqrt_3;
        var vert = size * 3 / 2;
        var xx = x * horiz;
        var yy = y * vert;
        if ((GridType == GridType.OffsetEvenY && y % 2 == 0) || (GridType == GridType.OffsetOddY && y % 2 == 1))
            xx += horiz / 2;
        var rot = Transform3D.Identity.RotatedLocal(Vector3.Up, (float)Math.PI / 2);
        return Transform.TranslatedLocal(new Vector3(xx, 0, yy)) * rot;
    }

    private static Vector2[] NeighborsOffsetOddX = [

            new(0, -1),
            new(1, 0),
            new(1, 1),
            new(0, 1),
            new(-1, 1),
            new(-1, 0),

            new(1, -1),
            new(2, 0),
            new(1, 2),
            new(-1, 2),
            new(-2, 0),
            new(-1, -1),
        ];

    private static Vector2[] NeighborsOffsetEvenX = [
            new(0, -1),
            new(1, -1),
            new(1, 0),
            new(0, 1),
            new(-1, 0),
            new(-1, -1),
            new(1, -2),
            new(2, 0),
            new(1, 1),
            new(-1, 1),
            new(-2, 0),
            new(-1, -2),
        ];

    public Vector2 GetNeighborCoordinate(int x, int y, int index) => g switch
    {
        GridType.OffsetEvenX => Mathf.PosMod(x, 2) == 1 ? NeighborsOffsetEvenX[index] : NeighborsOffsetOddX[index],
        GridType.OffsetOddX => Mathf.PosMod(x, 2) == 1 ? NeighborsOffsetOddX[index] : NeighborsOffsetEvenX[index],
        _ => throw new NotImplementedException(),
    } + new Vector2(x, y);
}
