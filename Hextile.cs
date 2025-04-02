using Godot;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace traintracks;


public partial class Hextile : Node3D, IClickable, IMouseEnterable
{
	public const int OPPOSITE = 3;
	public const int CLOCKWISE = 1;

	private Area3D collisionArea;

	private static readonly CylinderShape3D Area3DShape = new CylinderShape3D();

    private readonly CylinderMesh hexMesh = new()
    {
        RadialSegments = 6,
        Rings = 0,
    };
    private readonly CylinderMesh clickedOnMesh = new()
    {
        RadialSegments = 6,
        Rings = 0,
        Material = MaterialCache.Images.HexTile.GlowLg,
    };

    private MeshInstance3D Hexagon;
	private MeshInstance3D ClickedOnMarker;
	private Dictionary<HextileConnectionDouble, TrackStraight> TrainTracks = [];

	public Hextile()
	{
		collisionArea = new Area3D();
		AddChild(collisionArea);
		collisionArea.AddChild(new CollisionShape3D() { Shape = Area3DShape });

		Hexagon = new MeshInstance3D()
		{
			Mesh = hexMesh,
			RotationDegrees = new(0, 30, 0),
			Position = new(0, -1, 0),
		};
		AddChild(Hexagon);

		ClickedOnMarker = new MeshInstance3D()
		{
			Mesh = clickedOnMesh,
			Visible = false,
			RotationDegrees = new(0, 30, 0),
			Position = new(0, 1, 0),
		};
		AddChild(ClickedOnMarker);

		if (Engine.IsEditorHint() && FindChildren("label", owned: false).Count == 0)
		{
			AddChild(new Label3D()
			{
				Name="label",
				Billboard=BaseMaterial3D.BillboardModeEnum.Enabled,
				Text="HEX",
				Scale=new(10, 10, 10),
				Position=new(0, 5, 0),
			});
		}
	}

	public override void _Ready()
	{
		if (!Engine.IsEditorHint())
		{
			collisionArea.InputEvent += input_event;
		}
	}

	private float _size = 6;

	[Export]
	public float Size { get => _size; set { SetSize(value); } }

	[Export]
	public Material? Texture { get { return HexTex; } set { setMaterial(value); } }


	public HexGridProvider? _grid { get; set; }

	public int _x { get; set; }
	public int _y { get; set; }

	[Export]
	public HexGridProvider? Grid { get => _grid; set { UpdateGrid(value, X, Y); } }

	[Export]
	public int X { get => _x; set { UpdateGrid(_grid, value, Y); } }
	[Export]
	public int Y { get => _y; set { UpdateGrid(_grid, X, value); } }

	private StandardMaterial3D? HexTex => Hexagon.Mesh.SurfaceGetMaterial(0) as StandardMaterial3D;

	public void SetSize(float size)
	{
		size = Mathf.Max(size, 6);
		_size = size;
		Area3DShape.Radius = size;
		hexMesh.TopRadius = size;
		hexMesh.BottomRadius = size;
		clickedOnMesh.TopRadius = size;
		clickedOnMesh.BottomRadius = size;
	}

	private void Updated() => UpdateGrid(Grid, X, Y);
	private void UpdateGrid(HexGridProvider? grid, int x, int y)
	{
		_grid = grid;
		_x = x;
		_y = y;

		Name = $"Hextile_{x}_{y}";
		if (FindChild("label", owned: false) is Label3D label)
			label.Text = $"{x}, {y}";

		// ================================================================
		// ================================================================

		SetSize(Size);

		if (Grid == null)
			return;

		Transform = Grid.GetGridTransform(Size, x, y);
	}

	private void setMaterial(Material? texture)
	{
		if (texture == null)
			return;
		Hexagon.Mesh.SurfaceSetMaterial(0, texture);
	}


	#region Mouse Events
	public void input_event(Node camera, InputEvent @event, Vector3 eventPosition, Vector3 normal, long shapeIdx)
	{
		if (@event is InputEventMouseButton mouseButton && mouseButton.IsReleased() && mouseButton.ButtonIndex == MouseButton.Left)
			GlobalClickHelper.WasClicked(this);
		if (@event is InputEventMouseMotion)
			GlobalClickHelper.WasEntered(this);
		GetViewport().SetInputAsHandled();
	}
	void IClickable.OnClick()
	{
		ClickedOnMarker.Visible = true;
	}

	void IClickable.OnUnClick()
	{
		ClickedOnMarker.Visible = false;
	}

    void IMouseEnterable.OnEnter()
    {
		foreach (var track in TrainTracks.Values)
		{
			track.SetExtensionIndicator(true, true);
		}
    }
    void IMouseEnterable.OnLeave()
    {
        foreach (var track in TrainTracks.Values)
        {
            track.SetExtensionIndicator(false, false);
        }
    }
    #endregion

    #region Connections

    public static readonly IReadOnlyList<int> ConnectableNearNeighborRelativeIndexesFromNear = [2, 3, 4];
	public static readonly IReadOnlyList<int> ConnectableNearNeighborRelativeIndexesFromFar = [2, 3, 4, 5];
	public static readonly IReadOnlyList<int> ConnectableFarNeighborRelativeIndexesFromNear = [1, 2, 3, 4];
	public static readonly IReadOnlyList<int> ConnectableFarNeighborRelativeIndexesFromFar = [2, 3, 4];

	public const int MAX_CONNECTIONS_ON_SIDE = 3;

	public bool CanConnectOnSide(HextileConnection connection)
	{
		var count = TrainTracks.Keys.Where(x => x.Has(connection)).ToList();
        if (count.Count == 0)
			return true;
		if (count.Count >= MAX_CONNECTIONS_ON_SIDE)
			return false;
		if (count.Any(x => x.HasHeightDifference()))
			return false;
		return true;
	} 

	public Hextile? GetNearNeighbor(int i)
	{
		if (Grid?.GetNeighborCoordinate(X, Y, Mathf.PosMod(i, 6)) is not Vector2 p)
			return null;
		var name = $"Hextile_{p.X}_{p.Y}";
		return GetParent().GetChildrenByType<Hextile>().FirstOrDefault(x => x.Name == name);
	}

	public bool TryGetNearNeighbor(int i, [MaybeNullWhen(false)] out Hextile neighbor)
	{
		if (GetNearNeighbor(i) is Hextile hex)
		{
			neighbor = hex;
			return true;
		}
		neighbor = null;
		return false;
	}

	public Hextile? GetFarNeighbor(int i)
	{
		if (GetNearNeighbor(i) == null || GetNearNeighbor(i + CLOCKWISE) == null)
			return null;
		if (Grid?.GetNeighborCoordinate(X, Y, Mathf.PosMod(i, 6) + 6) is not Vector2 p)
			return null;
		var name = $"Hextile_{p.X}_{p.Y}";
		return GetParent().GetChildrenByType<Hextile>().FirstOrDefault(x => x.Name == name);
	}

	public bool TryGetFarNeighbor(int i, [MaybeNullWhen(false)] out Hextile neighbor)
	{
		if (GetFarNeighbor(i) is Hextile hex)
		{
			neighbor = hex;
			return true;
		}
		neighbor = null;
		return false;
	}

	/// <summary>
	/// will not actually validate if the given hex tile is a neighbor
	/// </summary>
	/// <param name="neighbor"></param>
	/// <returns></returns>
	public Vector3 GetNearNeighborSnapPoint(Hextile neighbor) => (neighbor.Position + Position) / 2;

	/// <summary>
	/// Does not check that the connection is valid
	/// </summary>
	/// <param name="from"></param>
	/// <param name="to"></param>
	/// <param name="displayType"></param>
	/// <returns></returns>
	public bool AddTrack(HextileConnection from, HextileConnection to, TrackDisplayType displayType)
	{
		var connection = new HextileConnectionDouble(from, to);

		var track = new TrackStraight()
		{
			Name = $"{Name}__{connection.One}__{connection.Two}",
		};
		var curve = new Curve3D();
		Hextile? hex1 = null;
		Hextile? hex2 = null;

        if (from.ConnectionType == HextileConnectionType.Near)
            TryGetNearNeighbor(from.Index, out hex1);
        else if (from.ConnectionType == HextileConnectionType.Far)
            TryGetFarNeighbor(from.Index, out hex1);

        if (to.ConnectionType == HextileConnectionType.Near)
            TryGetNearNeighbor(to.Index, out hex2);
        else if (to.ConnectionType == HextileConnectionType.Far)
            TryGetFarNeighbor(to.Index, out hex2);

		if (hex1 == null || hex2 == null)
			return false;

        var p1 = GetNearNeighborSnapPoint(hex1) - Position;
        var p2 = GetNearNeighborSnapPoint(hex2) - Position;
        curve.AddPoint(p1, @out: -p1 / 2);
        curve.AddPoint(p2, @in: -p2 / 2);

        track.SetCurve(curve, displayType);
		TrainTracks[connection] = track;
		AddChild(track);
		return true;
	}

	public bool CanAddTrack(HextileConnection from, HextileConnection to)
    {
        var connection = new HextileConnectionDouble(from, to);

        if (TrainTracks.ContainsKey(connection))
            return false;
		if (!CanConnectOnSide(from) || !CanConnectOnSide(to))
			return false;
		return true;
    }

	public void RemoveTrack(HextileConnection from, HextileConnection to)
    {
        var connection = new HextileConnectionDouble(from, to);
        if (TrainTracks.Remove(connection, out var value))
		{
			RemoveChild(value);
		}
	}

	#endregion
}
