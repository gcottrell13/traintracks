using Godot;
using Godot.Collections;

namespace traintracks;

[Tool]
public partial class Hextile : Node3D
{

	[Export]
	public Texture2D? Texture { get { return HexTex?.AlbedoTexture; } set { setMaterial(value); } }

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

	public Hextile()
	{
	}

	private void setMaterial(Texture2D? texture)
	{
		if (texture == null || Hexagon == null)
			return;
		Hexagon.SetSurfaceOverrideMaterial(0, MaterialCache.GetMaterial(texture));
	}
}
