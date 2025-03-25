using Godot;
using System.Collections.Generic;


namespace traintracks;


[Tool]
public partial class Root : Node3D
{
	public static double time;

	public override void _EnterTree()
	{
		AddChild(Timing.CreateTree(this));
	}

	public override void _ExitTree()
	{
		Timing.RemoveTree(this);
	}

	public override void _Process(double delta)
	{
		time += delta;
		RenderingServer.GlobalShaderParameterSet("GlobalTime", time);
	}

	public override void _Ready()
	{
		//Timing.RunCoroutine(this, SetupGrid(3, 3));
		Timing.RunCoroutine(this, SetupRandomWalk(20));
		//Timing.RunCoroutine(this, attach("Hextile2", 2, 4));
	}

	public IEnumerator<float> SetupRandomWalk(int length)
	{
		var tiles = SetupTilesRandomWalk(length);
		yield return 0.1f;
		PlaceTracks(tiles);
	}

	public IEnumerable<Hextile> SetupTilesRandomWalk(int length)
	{
		var rng = new RandomNumberGenerator();

		var pos = new Vector2(0, 0);
		var previousPositionsWithMultipleFreeDirections = new List<Vector2>();
		var tiles = new Dictionary<Vector2, Hextile>();
		for (var i = 0; i < length; i++)
		{
			var availablePositions = new List<Vector2>();
			if (!tiles.ContainsKey(pos + Vector2.Up)) availablePositions.Add(pos + Vector2.Up);
			if (!tiles.ContainsKey(pos + Vector2.Down)) availablePositions.Add(pos + Vector2.Down);
			if (!tiles.ContainsKey(pos + Vector2.Left)) availablePositions.Add(pos + Vector2.Left);
			if (!tiles.ContainsKey(pos + Vector2.Right)) availablePositions.Add(pos + Vector2.Right);
			if (availablePositions.Count == 0)
			{
				if (previousPositionsWithMultipleFreeDirections.Count != 0)
				{
					var index = rng.RandiRange(0, previousPositionsWithMultipleFreeDirections.Count - 1);
					pos = previousPositionsWithMultipleFreeDirections[index];
					previousPositionsWithMultipleFreeDirections.RemoveAt(index);
					i--;
					continue;
				}
				break;
			}
			else if (availablePositions.Count > 1)
			{
				previousPositionsWithMultipleFreeDirections.Add(pos);
			}

			pos = availablePositions[rng.RandiRange(0, availablePositions.Count - 1)];

			var hextile = GD.Load<PackedScene>("res://hextile.tscn").Instantiate<Hextile>();
			hextile.X = (int)pos.X;
			hextile.Y = (int)pos.Y;
			hextile.Grid = GD.Load<HexGridProvider>("res://hexGridProvider-test.tres");
			hextile.Texture = MaterialCache.Images.GrassHexLg;
			tiles[pos] = hextile;
			AddChild(hextile);
		}
		return tiles.Values;
	}

	public IEnumerator<float> SetupGrid(int w, int h)
	{
		var tiles = SetupTilesInGrid(w, h);
		yield return 0.1f;
		PlaceTracks(tiles);
	}

	public IEnumerable<Hextile> SetupTilesInGrid(int w, int h)
	{
		var grid = new HexGridProvider();
		grid.GridType = GridType.OffsetEvenX;

		for (var i = 0; i < w; i++)
		{
			for (var j = 0; j < h; j++)
			{
				var hextile = GD.Load<PackedScene>("res://hextile.tscn").Instantiate<Hextile>();
				hextile.X = i;
				hextile.Y = j;
				hextile.Grid = grid;
				hextile.Size = 7;
				hextile.Texture = MaterialCache.Images.GrassHexLg;
				AddChild(hextile);
				yield return hextile;
			}
		}
	}

	public void PlaceTracks(IEnumerable<Hextile> tiles)
	{
		foreach (var tile in tiles)
		{
			//for (var i = 0; i < 3; i++)
			//{
			//	Timing.RunCoroutine(this, attachNear(tile, i, i + 3));
			//}

			//for (var i = 0; i < 6; i++)
			//{
			//	Timing.RunCoroutine(this, attachNear(tile, i, i + 4));
			//}

			for (var i = 0; i < 6; i++)
			{
				Timing.RunCoroutine(this, attachFar(tile, i, i + 1));
				Timing.RunCoroutine(this, attachFar(tile, i, i + 2));
				Timing.RunCoroutine(this, attachFar(tile, i, i + 3));
				Timing.RunCoroutine(this, attachFar(tile, i, i + 4));
			}
		}
	}

	public IEnumerator<float> attachNear(Hextile hex, int one1, int two2)
	{
		if (hex.TryGetNearNeighbor(one1, out var hex1) && hex.TryGetNearNeighbor(two2, out var hex2))
		{
			var t = new TrackStraight();
			AddChild(t);
			yield return 0.1f;
			var curve = new Curve3D();
			// t.Position = hex.Position + Vector3.Up;
			var p1 = hex.GetNearNeighborSnapPoint(hex1);
			var p2 = hex.GetNearNeighborSnapPoint(hex2);
			curve.AddPoint(p1, @out: (hex.Position - p1) / 2);
			curve.AddPoint(p2, @in: (hex.Position - p2) / 2);
			t.SetCurve(curve);
		}
	}

	public IEnumerator<float> attachFar(Hextile hex, int one1, int two2)
	{
		if (hex.TryGetNearNeighbor(one1, out var hex1))
		{
			if (hex.TryGetFarNeighbor(two2, out var hex22))
			{
				var t = new TrackStraight();
				AddChild(t);
				yield return 0.1f;
				var curve = new Curve3D();
				// t.Position = hex.Position + Vector3.Up;
				var p1 = hex.GetNearNeighborSnapPoint(hex1);
				var p2 = hex.GetNearNeighborSnapPoint(hex22);
				curve.AddPoint(p1, @out: (hex.Position - p1) / 2);
				curve.AddPoint(p2, @in: (hex.Position - p2) / 2);
				t.SetCurve(curve);
			}
		}
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		base._UnhandledInput(@event);
		if (@event is InputEventMouseButton mouseButton && mouseButton.Pressed && mouseButton.ButtonIndex == MouseButton.Left) 
			GlobalClickHelper.WasClicked(null);
	}
}
