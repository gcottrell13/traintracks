using Godot;
using System;

namespace traintracks;

public partial class RailExtendIndicator : Node3D
{
	public Area3D ClickableArea => (Area3D)FindChild("Area3D");

	[Signal]
	public delegate void OnClickEventHandler(RailExtendIndicator node);

	public HextileConnection Connection;

	public override void _Ready()
	{
		ClickableArea.InputEvent += click;
	}

	public void click(Node camera, InputEvent @event, Vector3 eventPosition, Vector3 normal, long shapeIdx)
	{
		if (@event is InputEventMouseButton mouseButton && mouseButton.IsReleased() && mouseButton.ButtonIndex == MouseButton.Left)
			EmitSignalOnClick(this);
	}
}
