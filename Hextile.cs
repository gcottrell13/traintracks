using Godot;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace traintracks;

[Tool]
public partial class Hextile : Node3D, IClickable
{
	private static readonly float sqrt_3_over_2 = Mathf.Sqrt(3) / 2;

	private Area3D collisionArea;

	private static readonly Shape3D ShapecastShape = new SphereShape3D() { Radius = 0.5f };
	private static readonly CylinderShape3D Area3DShape = new CylinderShape3D();

	public Hextile()
	{
		for (var i = 0; i < 6; i++)
		{
			var close = new CustomShapecast3D(i, new ShapeCast3D()
			{
				Enabled = true,
				Shape = ShapecastShape,
				ExcludeParent = true,
				CollideWithAreas = true,
				CollideWithBodies = false,
				Visible = true,
			})
			{
				SideNum = i + 0.5f,
				RadiusCoefficient = 1 / sqrt_3_over_2,
			};
			AddChild(close.Shape);
			CloseShapecasts.Add(close);
		}

		collisionArea = new Area3D();
		AddChild(collisionArea);
		collisionArea.AddChild(new CollisionShape3D() { Shape = Area3DShape });
	}

	public readonly List<CustomShapecast3D> CloseShapecasts = [];

	public override void _Ready()
	{
		if (!Engine.IsEditorHint())
		{
			var area3d = GetNode<Area3D>("Area3D");
			area3d.InputEvent += input_event;
		}
		FindNeighbors();
	}

	public class CustomShapecast3D(int id, ShapeCast3D shape)
	{
		public ShapeCast3D Shape = shape;
		public float SideNum;
		public float RadiusCoefficient = 1;
		public int Id { get; private set; } = id;
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

	public DefaultDictionary<int, List<Hextile>> Neighbors = new(() => []);

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

		PutShapecastsInPosition(collisionRadius);
		Area3DShape.Radius = size;
		if (Hexagon?.Mesh is CylinderMesh c)
		{
			c.TopRadius = size;
			c.BottomRadius = size;
			Hexagon.Rotation = new (0, Mathf.Pi / 6, 0);
		}
		CallDeferred(nameof(FindNeighbors));
	}

	private void Updated() => UpdateGrid(Grid, X, Y);
	private void UpdateGrid(HexGridProvider? grid, int x, int y)
	{
		_grid = grid;
		_x = x;
		_y = y;

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

	private void FindNeighbors()
	{
		Neighbors.Clear();
		foreach (var shapecast in CloseShapecasts)
		{
			var neighbors = GetNeighbors(shapecast.Shape).ToList();
			Neighbors[shapecast.Id] = neighbors;
			shapecast.Shape.DebugShapeCustomColor = neighbors.Count switch
			{
				1 => new Color(0, 1, 0),
				2 => new Color(1, 1, 1),
				_ => new Color(0, 0, 0),
			};
		}
	}

	public Hextile? GetNearNeighbor(int i)
	{
		return Neighbors[Mathf.PosMod(i, 6)].Intersect(Neighbors[Mathf.PosMod(i - 1, 6)]).FirstOrDefault();
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

	/// <summary>
	/// will not actually validate if the given hex tile is a neighbor
	/// </summary>
	/// <param name="neighbor"></param>
	/// <returns></returns>
	public Vector3 GetNearNeighborSnapPoint(Hextile neighbor) => (neighbor.Position + Position) / 2;

	private IEnumerable<Hextile> GetNeighbors(ShapeCast3D shapecast)
	{
		if (!shapecast.IsInsideTree())
			yield break;

		shapecast.ForceShapecastUpdate();
		var count = shapecast.GetCollisionCount();
		for (var i = 0; i < count; i++)
		{
			var collision = shapecast.GetCollider(i);
			if (collision is Area3D area && area.GetParent() is Hextile hex && hex != this)
			{
				yield return hex;
			}
		}
	}

	private void PutShapecastsInPosition(float radius)
	{
		var f = 2 * Mathf.Pi / 6;
		foreach (var shapecast in CloseShapecasts)
		{
			shapecast.Shape.Position = new Vector3(Mathf.Sin(shapecast.SideNum * f), 0, Mathf.Cos(shapecast.SideNum * f)) * radius * shapecast.RadiusCoefficient;
			shapecast.Shape.TargetPosition = Vector3.Zero;
			shapecast.Shape.Owner = this;
		}
	}

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
