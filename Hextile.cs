using Godot;
using System.Collections.Generic;

namespace traintracks;

[Tool]
public partial class Hextile : Node3D, IClickable
{
	private static readonly float sqrt_3_over_2 = Mathf.Sqrt(3) / 2;

	private static readonly Shape3D ShapecastShape = new SphereShape3D() { Radius = 0.5f };

	public Hextile()
	{
		for (var i = 0; i < 6; i++)
		{
			var close = new CustomShapecast3D(new ShapeCast3D()
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
	}

	public readonly List<CustomShapecast3D> CloseShapecasts = [];

	public override void _Ready()
	{
		var area3d = GetNode<Area3D>("Area3D");
		area3d.InputEvent += input_event;
		FindNeighbors();
	}

	public class CustomShapecast3D(ShapeCast3D shape)
	{
		public ShapeCast3D Shape = shape;
		public float SideNum;
		public float RadiusCoefficient = 1;
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

	public Dictionary<string, Hextile?> Neighbors = [];

	private MeshInstance3D? _hexagon;
	private MeshInstance3D? Hexagon {
		get
		{
			_hexagon ??= FindChild("Hexagon") as MeshInstance3D;
			return _hexagon;
		} 
		set { _hexagon = value; } 
	}
	private StandardMaterial3D? HexTex => Hexagon?.GetSurfaceOverrideMaterial(0) as StandardMaterial3D;

	public void SetSize(float size)
	{
		_size = Mathf.Max(size, 6);
		var collisionRadius = size * sqrt_3_over_2;

		PutShapecastsInPosition(collisionRadius);
		(GetNode<Area3D>("Area3D").GetNode<CollisionShape3D>("CollisionShape3D").Shape as CylinderShape3D).Radius = size;
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
		if (Engine.IsEditorHint() && Grid != null) Grid.Changed -= Updated;

		_grid = grid;
		_x = x;
		_y = y;

		// ================================================================
		// ================================================================

		SetSize(Size);

		if (Grid == null)
			return;

		if (Engine.IsEditorHint()) Grid.Changed += Updated;
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
		foreach (var child in GetChildren())
		{
			if (child is not ShapeCast3D shapecast)
				continue;
			Neighbors[child.Name] = GetNeighbor(shapecast);
		}
	}

	private Hextile? GetNeighbor(ShapeCast3D shapecast)
	{
		if (!shapecast.IsInsideTree())
			return null;

		shapecast.ForceShapecastUpdate();
		var count = shapecast.GetCollisionCount();
		for (var i = 0; i < count; i++)
		{
			var collision = shapecast.GetCollider(i);
			if (collision is Area3D area && area.GetParent() is Hextile hex && hex != this)
			{
				shapecast.DebugShapeCustomColor = new Color(0, 1, 0);
				return hex;
			}
		}
		shapecast.DebugShapeCustomColor = new Color(0, 0, 0);
		return null;
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
