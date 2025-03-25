using Godot;
using System.Collections.Generic;
using System.Xml.Linq;


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
		Timing.RunCoroutine(this, Setup(3, 3));
		//Timing.RunCoroutine(this, attach("Hextile2", 2, 4));
	}

	public IEnumerator<float> Setup(int w, int h)
	{
		for (var i = 0; i < w; i++)
		{
			for (var j = 0; j < h; j++)
			{
				var hextile = GD.Load<PackedScene>("res://hextile.tscn").Instantiate<Hextile>();
				hextile.X = i;
				hextile.Y = j;
				hextile.Grid = GD.Load<HexGridProvider>("res://hexGridProvider-test.tres");
				hextile.Texture = MaterialCache.Images.GrassHexLg;
				AddChild(hextile);
			}
		}
		yield return 0.1f;
	}

	public IEnumerator<float> attach(string hexName, int one1, int two2)
	{
		var t = new TrackStraight();
		AddChild(t);
		yield return 0.1f;
		var hex = GetNode<Hextile>(hexName);
		var one = hex.CloseShapecasts[one1];
		var two = hex.CloseShapecasts[two2];
		var curve = new Curve3D();
		// t.Position = hex.Position + Vector3.Up;
		curve.AddPoint(one.Shape.GlobalPosition, @out: -one.Shape.Position);
		curve.AddPoint(two.Shape.GlobalPosition, @in: -two.Shape.Position);
		t.SetCurve(curve);
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		base._UnhandledInput(@event);
		if (@event is InputEventMouseButton mouseButton && mouseButton.Pressed && mouseButton.ButtonIndex == MouseButton.Left) 
			GlobalClickHelper.WasClicked(null);
	}
}
