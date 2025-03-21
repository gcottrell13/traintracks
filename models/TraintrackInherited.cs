using Godot;
using System;

namespace traintracks.models;

public partial class TraintrackInherited : Node3D
{
	public MeshInstance3D? Cube_002 => GetChild(0) as MeshInstance3D;
}
