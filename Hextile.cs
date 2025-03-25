using Godot;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace traintracks;

[Tool]
public partial class Hextile : Node3D, IClickable
{
    public static readonly IReadOnlyList<int> ConnectableNearNeighborRelativeIndexesFromNear = [2, 3, 4];
    public static readonly IReadOnlyList<int> ConnectableNearNeighborRelativeIndexesFromFar = [2, 3, 4, 5];
    public static readonly IReadOnlyList<int> ConnectableFarNeighborRelativeIndexesFromNear = [1, 2, 3, 4];
    public static readonly IReadOnlyList<int> ConnectableFarNeighborRelativeIndexesFromFar = [2, 3, 4];

    private static readonly float sqrt_3_over_2 = Mathf.Sqrt(3) / 2;

	private Area3D collisionArea;

	private static readonly CylinderShape3D Area3DShape = new CylinderShape3D();

	public Hextile()
	{
		collisionArea = new Area3D();
		AddChild(collisionArea);
		collisionArea.AddChild(new CollisionShape3D() { Shape = Area3DShape });

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

	private MeshInstance3D? _hexagon;
	private MeshInstance3D? Hexagon {
		get
		{
			_hexagon ??= FindChild("Hexagon") as MeshInstance3D;
			return _hexagon;
		} 
		set { _hexagon = value; } 
	}
	private StandardMaterial3D? HexTex => Hexagon?.Mesh.SurfaceGetMaterial(0) as StandardMaterial3D;

	public void SetSize(float size)
	{
		_size = Mathf.Max(size, 6);
		var collisionRadius = size * sqrt_3_over_2;
		Area3DShape.Radius = size;
		if (Hexagon?.Mesh is CylinderMesh c)
		{
			c.TopRadius = size;
			c.BottomRadius = size;
			Hexagon.Rotation = new (0, Mathf.Pi / 6, 0);
		}
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
		if (texture == null || Hexagon == null)
			return;
		Hexagon.Mesh.SurfaceSetMaterial(0, texture);
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
		if (GetNearNeighbor(i) == null || GetNearNeighbor(i + 1) == null)
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


	#region Click
	public void input_event(Node camera, InputEvent @event, Vector3 eventPosition, Vector3 normal, long shapeIdx)
	{
		if (@event is InputEventMouseButton mouseButton && mouseButton.Pressed && mouseButton.ButtonIndex == MouseButton.Left)
			GlobalClickHelper.WasClicked(this);
	}
	void IClickable.OnClick()
	{
		GetNode<Node3D>("ClickedOnIcon").Visible = true;
	}

	void IClickable.OnUnClick()
	{
		GetNode<Node3D>("ClickedOnIcon").Visible = false;
	}
	#endregion
}
