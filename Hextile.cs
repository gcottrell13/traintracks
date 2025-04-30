using Godot;
using Godot.NativeInterop;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
namespace traintracks;


public partial class Hextile : Node3D, IClickable, IMouseEnterable, INode<Hextile, HextileConnection>
{
	public const int OPPOSITE = 3;
	public const int CLOCKWISE = 1;

	private Area3D collisionArea;

	private static readonly CylinderShape3D Area3DShape = new();

    private static readonly CylinderMesh hexMesh = new()
    {
        RadialSegments = 6,
        Rings = 0,
    };
    private static readonly CylinderMesh clickedOnMesh = new()
    {
        RadialSegments = 6,
        Rings = 0,
        Material = MaterialCache.Images.HexTile.GlowLg,
    };

    private MeshInstance3D Hexagon;
	private MeshInstance3D ClickedOnMarker;
	private Dictionary<HextileConnectionDouble, TrackStraight> TrainTracks = [];
    private Dictionary<HextileConnection, HashSet<HextileConnection>> Connections = [];

	private float _size = 6;

	[Export]
	public float Size { get => _size; set { SetSize(value); } }

	[Export]
	public Material? Texture { get { return HexTex; } set { setMaterial(value); } }


	public HexGridProvider? _grid { get; set; }

	public GridPosition Pos { get; private set; }

	[Export]
	public HexGridProvider? Grid { get => _grid; set { UpdateGrid(value, Pos); } }

	[Export]
	public int X { get => Pos.X; set { UpdateGrid(_grid, new(value, Y)); } }
	[Export]
	public int Y { get => Pos.Y; set { UpdateGrid(_grid, new(X, value)); } }

	private StandardMaterial3D? HexTex => Hexagon.Mesh.SurfaceGetMaterial(0) as StandardMaterial3D;

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
                Name = "label",
                Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
                Text = "HEX",
                Scale = new(10, 10, 10),
                Position = new(0, 5, 0),
            });
        }
    }

    public override void _Ready()
    {
        if (!Engine.IsEditorHint())
        {
            collisionArea.InputEvent += InputEvent;
        }
    }

    public static string GetName(int x, int y) => $"Hextile_{x}_{y}";
    public static string GetName(float x, float y) => GetName((int)x, (int)y);

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

	private void Updated() => UpdateGrid(Grid, Pos);
	private void UpdateGrid(HexGridProvider? grid, GridPosition pos)
	{
		_grid = grid;
        Pos = pos;

        var (x, y) = pos;
		Name = GetName(x, y);
		if (FindChild("label", owned: false) is Label3D label)
			label.Text = $"{x}, {y}";

		// ================================================================
		// ================================================================

		SetSize(Size);

		if (Grid == null)
			return;

		Transform = Grid.GetGridTransform(Size, pos);
	}

	private void setMaterial(Material? texture)
	{
		if (texture == null)
			return;
		Hexagon.Mesh.SurfaceSetMaterial(0, texture);
	}

    #region Extending

    static TrackStraight? CurrentExtendingTrack;
    static HextileConnection CurrentExtendingDirection;
    static int CurrentExtensionHeight;
    static int StartingExtensionHeight;
    static readonly List<Hextile> HighlightedNeighborsForExtension = [];
    static Hextile? AddTrackOntoThisHex;

    [Signal]
	public delegate void OnChooseExtensionEventHandler(Hextile node);

    public void Highlight(bool h = true)
    {
        ClickedOnMarker.Visible = h;
    }

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void UnHighlight() => Highlight(false);

    private void DisplayExtenderArrows()
    {
        var alreadyDisplayed = new HashSet<HextileConnection>();
        foreach (var pair in TrainTracks)
        {
            var (connection, track) = pair;

            var displayStart = !alreadyDisplayed.Contains(connection.One)
                && TryGetNearNeighbor(connection.One.Index, out var startNeighbor)
                && startNeighbor.CanConnectOnSide(connection.One.Opposite);

            var displayEnd = !alreadyDisplayed.Contains(connection.Two)
                && TryGetNearNeighbor(connection.Two.Index, out var endNeighbor)
                && endNeighbor.CanConnectOnSide(connection.Two.Opposite);

            if (displayStart) alreadyDisplayed.Add(connection.One);
            if (displayEnd) alreadyDisplayed.Add(connection.Two);

            track.SetExtensionIndicator(displayStart ? connection.One : default, displayEnd ? connection.Two : default);
            if (displayStart || displayEnd)
                track.ClickExtender += DisplayExtensionPossibilities;
        }
    }

    private void DisplayExtensionPossibilities(TrackStraight track, RailExtendIndicator rei) => DisplayExtensionPossibilities(track, rei.Connection);

    private void DisplayExtensionPossibilities(TrackStraight track, HextileConnection dir)
    {
        ResetConnectionState();

        var neighbor = dir.ConnectionType switch
        {
            HextileConnectionType.Far => GetFarNeighbor(dir.Index),
            HextileConnectionType.Near => GetNearNeighbor(dir.Index),
        };

        if (neighbor == null)
            return;

        var opposite = dir.Opposite;

        var farNeighbors = opposite.ConnectionType switch
        {
            HextileConnectionType.Near => ConnectableFarNeighborRelativeIndexesFromNear,
            HextileConnectionType.Far => ConnectableFarNeighborRelativeIndexesFromFar,
        };

        var nearNeighbors = opposite.ConnectionType switch
        {
            HextileConnectionType.Near => ConnectableNearNeighborRelativeIndexesFromNear,
            HextileConnectionType.Far => ConnectableNearNeighborRelativeIndexesFromFar,
        };

        foreach (var far in farNeighbors)
        {
            var c = new HextileConnection(CurrentExtensionHeight, dir.Index + far, opposite.ConnectionType);
            var c2 = new HextileConnection(CurrentExtensionHeight, opposite.Index + far, HextileConnectionType.Far);
            if (neighbor.GetFarNeighbor(opposite.Index + far) is Hextile farNeighbor 
                && farNeighbor.CanConnectOnSide(c)
                && !neighbor.TrainTracks.ContainsKey(new HextileConnectionDouble(c2, opposite)))
                HighlightedNeighborsForExtension.Add(farNeighbor);
        }

        foreach (var near in nearNeighbors)
        {
            var c = new HextileConnection(CurrentExtensionHeight, dir.Index + near, opposite.ConnectionType);
            var c2 = new HextileConnection(CurrentExtensionHeight, opposite.Index + near, HextileConnectionType.Near);
            if (neighbor.GetNearNeighbor(opposite.Index + near) is Hextile nearNeighbor
                && nearNeighbor.CanConnectOnSide(c)
                && !neighbor.TrainTracks.ContainsKey(new HextileConnectionDouble(c2, opposite)))
                HighlightedNeighborsForExtension.Add(nearNeighbor);
        }

        foreach (var n in HighlightedNeighborsForExtension)
        {
            n.Highlight();
            n.OnChooseExtension += OnChooseExtensionHandler;
        }

        if (HighlightedNeighborsForExtension.Count > 0)
        {
            CurrentExtendingTrack = track;
            CurrentExtendingDirection = dir;
            CurrentExtensionHeight = dir.Height;
            StartingExtensionHeight = dir.Height;
            AddTrackOntoThisHex = neighbor;

            EmitSignalOnDisplayExtensions(neighbor);
        }
    }

    private void UnhighlightAllConnections()
    {
        foreach (var n in HighlightedNeighborsForExtension)
            n.UnHighlight();
    }

    private void UnsubscribeAllChooseExtensions()
    {
        foreach (var n in HighlightedNeighborsForExtension)
        {
            n.OnChooseExtension -= OnChooseExtensionHandler;
        }
    }

    private void OnChooseExtensionHandler(Hextile node)
    {
        UnsubscribeAllChooseExtensions();
        if (AddTrackOntoThisHex == null)
            return;
        AddTrackOntoThisHex.RemovePreviewTrack();
        if (AddTrackOntoThisHex.ExtendTrackToHex(node, TrackDisplayType.Normal, out var c) is TrackStraight t)
            AddTrackOntoThisHex.DisplayExtensionPossibilities(t, c);
    }

    private TrackStraight? ExtendTrackToHex(Hextile node, TrackDisplayType dtype, out HextileConnection c)
    {
        c = default;
        if (Grid == null)
            return null;

        var o = CurrentExtendingDirection.Opposite;
        var index = Grid.GetNeighborIndex(X, node.X, Y, node.Y);
        var type = index >= 6 ? HextileConnectionType.Far : HextileConnectionType.Near;
        c = new(CurrentExtensionHeight, new(index), type);
        return AddTrack(o, c, dtype);
    }

    private TrackStraight? ExtendTrackToHex(Hextile node, TrackDisplayType dtype) => ExtendTrackToHex(node, dtype, out _);

    private void ResetConnectionState()
    {
        AddTrackOntoThisHex?.RemovePreviewTrack();
        CurrentExtendingDirection = default;
        CurrentExtendingTrack?.GetParent<Hextile>().UnsubscribeAllChooseExtensions();
        CurrentExtendingTrack = default;
        AddTrackOntoThisHex = default;
        UnhighlightAllConnections();
        HighlightedNeighborsForExtension.Clear();
    }

    [Signal]
    public delegate void OnDisplayExtensionsEventHandler(Hextile hex);

    #endregion


    #region Mouse Events
    public void InputEvent(Node camera, InputEvent @event, Vector3 eventPosition, Vector3 normal, long shapeIdx)
	{
		if (@event is InputEventMouseButton mouseButton && mouseButton.IsReleased() && mouseButton.ButtonIndex == MouseButton.Left)
			GlobalClickHelper.WasClicked(this);
		if (@event is InputEventMouseMotion)
			GlobalClickHelper.WasEntered(this);
		GetViewport().SetInputAsHandled();
	}
	void IClickable.OnClick()
	{
		if (HighlightedNeighborsForExtension.Contains(this))
			EmitSignalOnChooseExtension(this);
        else
            ResetConnectionState();
    }

	void IClickable.OnUnClick()
	{
		UnhighlightAllConnections();
    }

    void IMouseEnterable.OnEnter()
    {
        if (HighlightedNeighborsForExtension.Contains(this))
        {
            AddTrackOntoThisHex?.RemovePreviewTrack();
            AddTrackOntoThisHex?.ExtendTrackToHex(this, TrackDisplayType.Preview);
        }
        else if (HighlightedNeighborsForExtension.Count == 0) // if (mode == add-track)
        {
            DisplayExtenderArrows();
        }
    }
    void IMouseEnterable.OnLeave()
    {
        foreach (var track in TrainTracks.Values)
        {
            track.SetExtensionIndicator(default, default);
			track.ClickExtender -= DisplayExtensionPossibilities;
        }
        if (HighlightedNeighborsForExtension.Contains(this))
        {
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

	public Hextile? GetNearNeighbor(NeighborId i)
	{
		if (Grid?.GetNeighborCoordinate(X, Y, (int)i) is not GridPosition p)
			return null;
		var name = GetName(p.X, p.Y);
		return GetParent().GetChildrenByType<Hextile>().FirstOrDefault(x => x.Name == name);
	}

	public bool TryGetNearNeighbor(NeighborId i, [MaybeNullWhen(false)] out Hextile neighbor)
	{
		if (GetNearNeighbor(i) is Hextile hex)
		{
			neighbor = hex;
			return true;
		}
		neighbor = null;
		return false;
	}

	public Hextile? GetFarNeighbor(NeighborId i)
	{
		if (GetNearNeighbor(i) == null || GetNearNeighbor(-i) == null)
			return null;
		if (Grid?.GetNeighborCoordinate(X, Y, (int)i + 6) is not GridPosition p)
			return null;
        var name = GetName(p.X, p.Y);
        return GetParent().GetChildrenByType<Hextile>().FirstOrDefault(x => x.Name == name);
	}

	public bool TryGetFarNeighbor(NeighborId i, [MaybeNullWhen(false)] out Hextile neighbor)
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
	public TrackStraight? AddTrack(HextileConnection from, HextileConnection to, TrackDisplayType displayType)
	{
		var connection = new HextileConnectionDouble(from, to);

        if (TrainTracks.ContainsKey(connection))
            return null;

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
			return null;

        var track = new TrackStraight()
        {
            Name = $"{Name}__Track__{connection.One}__{connection.Two}",
        };
        var curve = new Curve3D();

        var p1 = GetNearNeighborSnapPoint(hex1) - Position;
        var p2 = GetNearNeighborSnapPoint(hex2) - Position;
        curve.AddPoint(p1, @out: -p1 / 2);
        curve.AddPoint(p2, @in: -p2 / 2);

        track.SetCurve(curve, displayType);
		TrainTracks[connection] = track;
        this.AddChildAsync(track);

        RecordConnection(from, to);
        RecordConnection(to, from);

        return track;
	}

    /// <summary>
    /// Add the connection record for pathfinding purposes
    /// </summary>
    /// <param name="from"></param>
    /// <param name="to"></param>
    private void RecordConnection(HextileConnection from, HextileConnection to)
    {
        if (Connections.TryGetValue(from, out var connections))
            connections.Add(to);
        else
            Connections[from] = [to];
    }

    /// <summary>
    /// Checks if placement is possible
    /// </summary>
    /// <param name="from"></param>
    /// <param name="to"></param>
    /// <returns></returns>
	public bool CanAddTrack(HextileConnection from, HextileConnection to)
    {
        var connection = new HextileConnectionDouble(from, to);

        if (TrainTracks.ContainsKey(connection))
            return false;
		if (!CanConnectOnSide(from) || !CanConnectOnSide(to))
			return false;
		return true;
    }

    /// <summary>
    /// Deletes the track (if it exists)
    /// </summary>
    /// <param name="from"></param>
    /// <param name="to"></param>
	public void RemoveTrack(HextileConnection from, HextileConnection to)
    {
        var connection = new HextileConnectionDouble(from, to);
        if (TrainTracks.Remove(connection, out var value))
		{
			RemoveChild(value);
            RemoveConnectionRecord(from, to);
            RemoveConnectionRecord(to, from);
        }
	}

    /// <summary>
    /// Removes the pathfinding connection record
    /// </summary>
    /// <param name="from"></param>
    /// <param name="to"></param>
    public void RemoveConnectionRecord(HextileConnection from, HextileConnection to)
    {
        if (Connections.TryGetValue(from, out var connections))
            connections.Remove(to);
    }

    /// <summary>
    /// During building, remove the preview track
    /// </summary>
	public void RemovePreviewTrack()
	{
		foreach (var pair in TrainTracks)
		{
			if (pair.Value.DisplayType == TrackDisplayType.Preview)
            {
                TrainTracks.Remove(pair.Key);
                RemoveChild(pair.Value);
				return;
            }
		}
	}


    #endregion



    float INode<Hextile, HextileConnection>.Weight => 1;
    ICollection<(INode<Hextile, HextileConnection> neighbor, HextileConnection connection)> INode<Hextile, HextileConnection>.Neighbors(HextileConnection from)
    {
        if (!Connections.TryGetValue(from.Opposite, out var v))
            return [];
        return v.Select(x => (this as INode<Hextile, HextileConnection>, x)).ToList();
    }
}
