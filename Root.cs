using Godot;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static traintracks.MaterialCache.Images;


namespace traintracks;


public partial class Root : Node3D
{
	public static double TIME { get; private set; }
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
		Camera.Current = true;
	}

	public override void _Process(double delta)
	{
		TIME += delta;
		RenderingServer.GlobalShaderParameterSet("GlobalTime", TIME);

		var cameraMovement = Camera.Left * Input.GetAxis("ui_right", "ui_left") + Camera.Forward * Input.GetAxis("ui_down", "ui_up");

		if (!cameraMovement.IsZeroApprox())
		{
			Camera.OrbitPoint += cameraMovement * (float)delta * 20;
		}
	}

	public override void _Ready()
	{
		Task.Run(async () =>
		{
			await SetupTilesInGridAsync(10, 10);
			await attachNearAsync();
        });
		// Timing.RunCoroutine(this, SetupTilesInGrid(10, 10)).Then(attachNear());
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
					previousPositionsWithMultipleFreeDirections.PopRandom();
                    i--;
					continue;
				}
				break;
			}
			else if (availablePositions.Count > 1)
			{
				previousPositionsWithMultipleFreeDirections.Add(pos);
			}

			pos = availablePositions.PopRandom();

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
        var wait = 0.01f;
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
                    Texture = HexTile.GrassLg,
                };
                hextile.OnDisplayExtensions += OnDisplayExtensions;
				AddChild(hextile);
                yield return wait;
            }
        }
    }


    public async Task SetupTilesInGridAsync(int w, int h)
    {
        var wait = 10;
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
                    Texture = HexTile.GrassLg,
                };
                hextile.OnDisplayExtensions += OnDisplayExtensions;
				this.AddChildAsync(hextile);
                await Task.Delay(wait);
            }
        }
    }
    
	private async void OnDisplayExtensions(Hextile hextile)
	{
		await Task.Delay(1000);
		Camera.OrbitPoint = hextile.Position;
	}

	public IEnumerator<float> attachNear()
    {
		var hex = (Hextile)FindChild(Hextile.GetName(1, 1), owned: false);
        yield return 0.1f;
        hex.AddTrack(new(0, new(0), HextileConnectionType.Near), new(0, new(2), HextileConnectionType.Near), TrackDisplayType.Normal);
	}

	public async Task attachNearAsync()
    {
        var hex = (Hextile)FindChild(Hextile.GetName(1, 1), owned: false);
		await Task.Delay(100);
        hex.AddTrack(new(0, new(0), HextileConnectionType.Near), new(0, new(2), HextileConnectionType.Near), TrackDisplayType.Normal);
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
				Camera.AngleAround += mouseMotion.Relative.X * 0.01f;
				Camera.AngleHeight = Mathf.Clamp(Camera.AngleHeight + mouseMotion.Relative.Y * 0.01f, Mathf.Pi / 8, 1.5f);
				DidMoveFromRightClick = true;
			}
		}
		else if (@event is InputEventMouseButton mouseButton)
		{
			switch (mouseButton.ButtonIndex)
			{
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
