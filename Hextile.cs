using Godot;
using Godot.Collections;

namespace traintracks;

[Tool]
public partial class Hextile : Node3D
{
	private static readonly float sqrt_3_over_2 = Mathf.Sqrt(3) / 2;
	~Hextile()
	{
		if (Engine.IsEditorHint() && Grid != null) Grid.OnUpdated -= Updated;
	}

	public override void _Ready()
	{
		base._Ready();
	}

	private float _size = 6;

	[Export]
	public float Size { get => _size; set { SetSize(value); } }

	[Export]
	public Texture2D? Texture { get { return HexTex?.AlbedoTexture; } set { setMaterial(value); } }


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
		(GetNode<Area3D>("Area3D").GetNode<CollisionShape3D>("CollisionShape3D").Shape as CylinderShape3D).Radius = collisionRadius;
		if (Hexagon?.Mesh is CylinderMesh c)
		{
			c.TopRadius = Size;
			c.BottomRadius = Size;
			Hexagon.Rotation = new (0, Mathf.Pi / 6, 0);
		}
	}

	private void Updated() => UpdateGrid(Grid, X, Y);
	private void UpdateGrid(HexGridProvider? grid, int x, int y)
	{
		if (Engine.IsEditorHint() && Grid != null) Grid.OnUpdated -= Updated;

		_grid = grid;
		_x = x;
		_y = y;

		if (Grid == null)
			return;

		if (Engine.IsEditorHint()) Grid.OnUpdated += Updated;
		Transform = Grid.GetGridTransform(Size, x, y);

		// ================================================================
		// ================================================================

		CallDeferred(nameof(FindNeighbors));
	}

	private void setMaterial(Texture2D? texture)
	{
		if (texture == null || Hexagon == null)
			return;
		Hexagon.SetSurfaceOverrideMaterial(0, MaterialCache.GetMaterial(texture));
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
		var count = 0;
		var f = 2 * Mathf.Pi / 6;
		foreach (var child in GetChildren())
		{
			if (child is not ShapeCast3D shapecast)
				continue;
			shapecast.TargetPosition = new Vector3(Mathf.Sin(count * f), 0, Mathf.Cos(count * f)) * radius + Vector3.Up;
			count++;
		}
	}
}
