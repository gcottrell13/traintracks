using Godot;
using System.Drawing;
using System.Security.Cryptography;

namespace traintracks;

public partial class OrbitCamera3D : Camera3D
{

    private Vector3 _orbitPoint;
    private float _angleY;
    private float _angleZ;
    private float _distance;
    private Vector3 _axis = Vector3.Up;

    [Export]
    public Vector3 OrbitPoint { get => _orbitPoint; set { SetValues(point: value); } }

    /// <summary>
    /// Angle around the axis
    /// </summary>
    [Export]
    public float AngleAround { get => _angleY; set { SetValues(angleY: value); } }

    /// <summary>
    /// Angle up and down, according to the axis
    /// </summary>
    [Export]
    public float AngleHeight { get => _angleZ; set { SetValues(angleZ: value); } }

    [Export]
    public float Distance { get => _distance; set { SetValues(distance: value); } }

    [Export]
    public Vector3 Axis { get => _axis; set { SetValues(axis: value); } }

    public Vector3 Forward { get; private set; }
    public Vector3 Left { get; private set; }


    public void SetValues(Vector3? point = null, float? angleY = null, float? angleZ = null, float? distance = null, Vector3? axis = null)
    {
        _orbitPoint = point ?? OrbitPoint;
        _angleY = angleY ?? AngleAround;
        _angleZ = angleZ ?? AngleHeight;
        _distance = distance ?? Distance;
        _axis = axis ?? Axis;
        Update();
    }

    private void Update()
    {
        var t = new Transform3D(Basis.LookingAt(Axis, Vector3.Forward), Vector3.Zero);
        var r = new Vector3(Mathf.Cos(AngleAround), Mathf.Sin(AngleAround), -Mathf.Sin(AngleHeight)) * Distance; // negative in Z because we want positive angles to result in positive heights
        var x = t.Basis * r;
        LookAtFromPosition(x + OrbitPoint, OrbitPoint);
        Forward = -t.Basis.X.Rotated(t.Basis.Z, AngleAround);
        Left = t.Basis.Y.Rotated(t.Basis.Z, AngleAround);
        //if (IsInsideTree() && GetFrustum()[5].IntersectsRay(OrbitPoint, -Axis) is Vector3 intersect)
        //    Position += OrbitPoint - intersect;
    }
}
