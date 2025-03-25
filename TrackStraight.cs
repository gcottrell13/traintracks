using Godot;
using System.Collections.Generic;
using System.Threading.Tasks;

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

	private Node3D TrackModel = new Node3D();

	public const float DoubleRailSize = 0.2f;
	public const float DoubleRailBarSize = 0.2f;
	public const float DoubleRailClip = 0.05f;

	private CoroutineHandle GenerationCoroutine;

	public const int SubdivideDepth = 0;

	public override void _Ready()
	{
		AddChild(TrackModel);
		GenerateRailGeometry();
	}

	public override void _Process(double delta)
	{
	}

	public void SetCurve(Curve3D? curve)
	{
		_curve = curve;
		switch (TrackType)
		{
			case TrackType.DoubleRail:
				{
					DoubleRailGeometry();
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
		Timing.StopCoroutine(GenerationCoroutine);
		GenerationCoroutine = Timing.RunCoroutine(this, GenerateDoubleRailAsync());
	}

	public IEnumerator<float> GenerateDoubleRailAsync()
	{
		// return;
		if (Curve == null)
			yield break;

		float minZ = -4.5f;
		float maxZ = 4.5f;

		if (Curve.PointCount < 2)
			yield break;
		minZ = 0;
		maxZ = Curve.GetBakedLength();

		if (maxZ < 1)
			yield break;

		yield return 0.1f;

		var fractional = maxZ % 1.0f;
		var partCount = Mathf.Abs(maxZ) + Mathf.Abs(minZ);
		if (fractional > 0.5)
		{
			fractional = -fractional;
			maxZ += 0.25f;
		}

		var existingParts = GetDoubleRailParts().GetEnumerator();
		for (var i = minZ; i <= (int)maxZ; i += 1 + (fractional / partCount))
		{
			var part = existingParts.MoveNext() ? existingParts.Current : CreateDoubleRailTrackPart();
			part.TargetZ = i + 0.5f;
			if (part.GetParent() == null)
			{
				TrackModel.AddChild(part);
				UpdateDoubleRailCurve();
				yield return 0.05f;
			}
		}
		while (existingParts.MoveNext())
		{
			TrackModel.RemoveChild(existingParts.Current);
		}
	}

	private void UpdateDoubleRailCurve()
	{
		foreach (var part in GetDoubleRailParts())
		{
			if (Curve == null)
			{
				part.Position = new Vector3(0, 0, part.TargetZ);
			}
			else if (part.TargetZ <= Curve.GetBakedLength())
			{
				part.SetDoubleTrailTransform3D(Curve.SampleBakedWithRotation(part.TargetZ, applyTilt: true));
			}
			else
			{
				part.GetParent()?.RemoveChild(part);
			}
				
		}
	}

	private IEnumerable<DoubleRailTrackPart> GetDoubleRailParts()
	{
		foreach (var child in TrackModel.GetChildren())
		{
			if (child is DoubleRailTrackPart part)
				yield return part;
			else
				TrackModel.RemoveChild(child);
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
			Position = new(0, DoubleRailBarSize / 2 - 0.05f, 0),
			CastShadow = GeometryInstance3D.ShadowCastingSetting.On,
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
			CastShadow = GeometryInstance3D.ShadowCastingSetting.On,
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
			Position = new(-1, leftRailMesh.Position.Y, 0),
			CastShadow = GeometryInstance3D.ShadowCastingSetting.On,
		};
		rightRailMesh.SetInstanceShaderParameter("start_time", Root.time);
		leftRailMesh.SetInstanceShaderParameter("start_time", Root.time);
		barMesh.SetInstanceShaderParameter("start_time", Root.time);

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
			Position += Vector3Helpers.RandomJitter(Position.Z);
		}
	}
}
