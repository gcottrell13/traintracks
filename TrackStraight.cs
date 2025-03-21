using Godot;

namespace traintracks;

public enum TrackType
{
	OldRail = 1,
}

[Tool]
public partial class TrackStraight : Node3D
{

	[Export]
	public TrackType TrackType { get; set; } = TrackType.OldRail;

	[Export]
	public Curve3D? Curve { get; set; }

	private Node3D? trackModel => (Node3D)FindChild("track");

	public void GenerateRailGeometry()
	{
		switch (TrackType)
		{
			case TrackType.OldRail:
				{
					_oldRailGeometry();
					break;
				}
		}
	}

	public override void _Ready()
	{
		base._Ready();
		GenerateRailGeometry();
	}

	private void _oldRailGeometry()
	{
		if (trackModel == null)
			return;
		foreach (var child in trackModel.GetChildren())
			trackModel.RemoveChild(child);

		var boxMesh = new BoxMesh() {
			SubdivideDepth = 3,
		};
		var box = new MeshInstance3D() { Mesh = boxMesh };
		trackModel.AddChild(box);
		box.SetSurfaceOverrideMaterial(0, MaterialCache.Images.TrainTrackUV1);
	}
}
