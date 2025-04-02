using Godot;
using System.Collections.Generic;
using System.Linq;


namespace traintracks;


public partial class Root : Node3D
{
	public static double time;
	public readonly OrbitCamera3D Camera;

	public Root()
	{
		Camera = new()
		{
			Distance = 10,
			AngleHeight = Mathf.Pi / 4,
		};

		AddChild(Camera);
	}

	public override void _EnterTree()
	{
		AddChild(Timing.CreateTree(this));
		Camera.Current = true;
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
		Timing.RunCoroutine(this, SetupTilesInGrid(10, 10));
		//Timing.RunCoroutine(this, SetupRandomWalk(20));
		//Timing.RunCoroutine(this, attach("Hextile2", 2, 4));
	}

	public IEnumerator<float> SetupRandomWalk(int length)
	{
		var tiles = SetupTilesRandomWalk(length).ToList();
		yield return 0.1f;
	}

	public IEnumerable<Hextile> SetupTilesRandomWalk(int length)
	{
		var grid = new HexGridProvider
		{
			GridType = GridType.OffsetEvenX,
			Transform = Transform3D.Identity,
		};
		
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

			var hextile = new Hextile
			{
				X = (int)pos.X,
				Y = (int)pos.Y,
				Grid = grid,
				Texture = MaterialCache.Images.HexTile.GrassLg,
			};
			tiles[pos] = hextile;
			AddChild(hextile);
		}
		return tiles.Values;
	}

	public IEnumerator<float> SetupTilesInGrid(int w, int h)
	{
		var grid = new HexGridProvider
		{
			GridType = GridType.OffsetEvenX,
		};

		for (var i = 0; i < w; i++)
		{
			for (var j = 0; j < h; j++)
			{
				var hextile = new Hextile
				{
					X = i - w / 2,
					Y = j - h / 2,
					Grid = grid,
					Texture = MaterialCache.Images.HexTile.GrassLg,
				};
				GD.Print($"{hextile} - {hextile.Position}");
				AddChild(hextile);
				yield return 0.01f;


				for (var k = 0; k < 3; k++)
				{
					Timing.RunCoroutine(this, attachNear(hextile, k, k + 3));
				}
			}
		}
	}

	public void PlaceTracks(IEnumerable<Hextile> tiles)
	{
		foreach (var tile in tiles)
		{
			for (var i = 0; i < 3; i++)
			{
				Timing.RunCoroutine(this, attachNear(tile, i, i + 3));
			}

			//for (var i = 0; i < 6; i++)
			//{
			//	Timing.RunCoroutine(this, attachNear(tile, i, i + 4));
			//}

			//for (var i = 0; i < 6; i++)
			//{
			//	Timing.RunCoroutine(this, attachFar(tile, i, i + 1));
			//	Timing.RunCoroutine(this, attachFar(tile, i, i + 2));
			//	Timing.RunCoroutine(this, attachFar(tile, i, i + 3));
			//	Timing.RunCoroutine(this, attachFar(tile, i, i + 4));
			//}
		}
	}

	public IEnumerator<float> attachNear(Hextile hex, int one1, int two2)
	{
		hex.AddTrack(new(0, (byte)one1, HextileConnectionType.Near), new(0, (byte)two2, HextileConnectionType.Near), TrackDisplayType.Ghost);
		yield return 0.1f;
	}

	public bool DidMoveFromRightClick = false;

	public override void _Input(InputEvent @event)
	{
		if (Engine.IsEditorHint())
			return;
		if (@event is InputEventMouseMotion mouseMotion)
		{
			if (!mouseMotion.Relative.IsZeroApprox() && Input.IsMouseButtonPressed(MouseButton.Right))
			{
				Camera.AngleAround -= mouseMotion.Relative.X * 0.01f;
				Camera.AngleHeight = Mathf.Clamp(Camera.AngleHeight + mouseMotion.Relative.Y * 0.01f, Mathf.Pi / 8, 1.5f);
				DidMoveFromRightClick = true;
			}
		}
		else if (@event is InputEventMouseButton mouseButton)
		{
			switch (mouseButton.ButtonIndex)
			{
				case MouseButton.WheelDown or MouseButton.WheelUp:
					{
						var dir = mouseButton.ButtonIndex == MouseButton.WheelDown ? 1 : -1;
						if (Camera.Projection == Camera3D.ProjectionType.Perspective)
						{
							Camera.Distance = Mathf.Clamp(Camera.Distance + mouseButton.Factor * dir, 10, 300);
						}
						else if (Camera.Projection == Camera3D.ProjectionType.Orthogonal)
						{
							Camera.Size = Mathf.Clamp(Camera.Size + dir * mouseButton.Factor, 10, 500);
						}

						break;
					}
				case MouseButton.Right:
					{
						if (mouseButton.IsReleased())
						{
							if (DidMoveFromRightClick)
								DidMoveFromRightClick = false;
							else
							{ /* cancel current action */ }
						}
						break;
					}
			}
		}
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (Engine.IsEditorHint())
			return;
		if (@event is InputEventMouseButton mouseButton && mouseButton.IsReleased() && mouseButton.ButtonIndex == MouseButton.Left)
		{
			GlobalClickHelper.WasClicked(null);
		}
	}
}
