using Godot;

namespace traintracks;

public enum TrackType
{
	MonoRail = 1,
	DoubleRail = 2,
}

[Tool]
public partial class TrackStraight : Node3D
{

	private TrackType _trackType = TrackType.DoubleRail;

	[Export]
	public TrackType TrackType { get => _trackType; set { _trackType = value; GenerateRailGeometry(); } }

	private float? _lastCurveLength;
	private Curve3D? _curve;
	[Export]
	public Curve3D? Curve { get => _curve; set { SetCurve(value); } }

	private Node3D? TrackModel => (Node3D)FindChild("track");

	public const float DoubleRailSize = 0.2f;
	public const float DoubleRailBarSize = 0.2f;
	public const float DoubleRailClip = 0.05f;

	public const int SubdivideDepth = 3;

	public const float MinZ = -4;
	public const float MaxZ = 5;

	public override void _Ready()
	{
		base._Ready();
		GenerateRailGeometry();
	}

	public override void _Process(double delta)
	{
		base._Process(delta);

		var newCurve = Curve?.GetBakedLength();

		if (newCurve != _lastCurveLength)
		{
			SetCurve(Curve);
			_lastCurveLength = newCurve;
		}
	}

	public void SetCurve(Curve3D? curve)
	{
		_curve = curve;
		switch (TrackType)
		{
			case TrackType.DoubleRail:
				{
					UpdateDoubleRailCurve();
					break;
				}
		}
	}

	public void GenerateRailGeometry()
	{
		switch (TrackType)
		{
			case TrackType.DoubleRail:
				{
					DoubleRailGeometry();
					break;
				}
		}
	}

	private void DoubleRailGeometry()
	{
		if (TrackModel == null)
			return;
		foreach (var child in TrackModel.GetChildren())
			TrackModel.RemoveChild(child);

		for (var i = MinZ; i <= MaxZ; i++)
		{
			var part = CreateDoubleRailTrackPart();
			TrackModel.AddChild(part);
			part.TargetZ = i;
		}
	}

	private void UpdateDoubleRailCurve()
	{
		if (TrackModel == null)
			return;

		foreach (var child in TrackModel.GetChildren())
		{
			if (child is not DoubleRailTrackPart part)
				continue;
			//var step = 1.0f / SubdivideDepth;

			//for (var f = -0.5f; f <= 0.5f; f += step)
			//{

			//}
			if (Curve == null)
			{
				part.SetDoubleTrailTransform3D(Transform3D.Identity);
			}
			else
			{
				var s = (part.TargetZ - MinZ) / (MaxZ - MinZ);
				var len = Curve.GetBakedLength();
				part.SetDoubleTrailTransform3D(Curve.SampleBakedWithRotation(s * len, applyTilt: true));
			}
				
		}
	}

	private static DoubleRailTrackPart CreateDoubleRailTrackPart()
	{
		var barMesh = new MeshInstance3D
		{
			Name = "BarMesh",
			Mesh = new BoxMesh()
			{
				Size = new(2.5f, DoubleRailBarSize, DoubleRailBarSize),
				Material = MaterialCache.Images.TrainTrackUV1,
			},
			Position = new(0, DoubleRailBarSize / 2 - 0.05f, 0)
		};
		var leftRailMesh = new MeshInstance3D()
		{
			Name = "LeftRailMesh",
			Mesh = new BoxMesh()
			{
				SubdivideDepth = SubdivideDepth,
				Size = new(DoubleRailSize, DoubleRailSize, 1),
				Material = MaterialCache.Images.TrainTrackUV2,
			}, 
			Position = new(1, barMesh.Position.Y + DoubleRailSize - DoubleRailClip, 0),
		};
		var rightRailMesh = new MeshInstance3D
		{
			Name = "RightRailMesh",
			Mesh = new BoxMesh()
			{
				SubdivideDepth = SubdivideDepth,
				Size = new(DoubleRailSize, DoubleRailSize, 1),
				Material = MaterialCache.Images.TrainTrackUV2,
			},
			Position = new(-1, leftRailMesh.Position.Y, 0)
		};
		return new DoubleRailTrackPart().SetMeshes(leftRailMesh, rightRailMesh, barMesh);
	}

	private partial class DoubleRailTrackPart : Node3D
	{
		public MeshInstance3D? leftRailMesh;
		public MeshInstance3D? rightRailMesh;
		public MeshInstance3D? barMesh;
		public float TargetZ;

		public DoubleRailTrackPart SetMeshes(MeshInstance3D left, MeshInstance3D right, MeshInstance3D bar)
		{
			leftRailMesh = left;
			rightRailMesh = right;
			barMesh = bar;
			AddChild(leftRailMesh);
			AddChild(rightRailMesh);
			AddChild(barMesh);
			return this;
		}

		public void SetDoubleTrailTransform3D(Transform3D transform)
		{
			Transform = transform;

		}
	}
}
