using Godot;
using System;
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
    Transform3D t;
    float s;
    GridType g = GridType.OffsetEvenX;

    [Export]
    public Transform3D Transform { get => t; set { t = value; OnUpdated?.Invoke(); } }

    [Export]
    public GridType GridType { get => g; set { g = value; OnUpdated?.Invoke(); } }

    private static readonly float sqrt_3 = (float)Math.Sqrt(3);

    public delegate void Updated();
    public event Updated? OnUpdated;

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
}
