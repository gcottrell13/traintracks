using Godot;
using System.Collections.Generic;
using static Godot.BaseMaterial3D;

namespace traintracks;

public static class MaterialCache
{
    private static ShaderMaterial SetShaderParams(ShaderMaterial shader, Dictionary<string, Variant> parameters)
    {
        foreach (var key in parameters.Keys)
            shader.SetShaderParameter(key, parameters[key]);
        return shader;
    }

    public static class Images
    {
        public static class DoubleRailTrack
        {
            public readonly static ShaderMaterial Bar_TimedGlow = SetShaderParams(
                new ShaderMaterial() { Shader = ResourceLoader.Load<Shader>("res://models/doubleRail.gdshader") },
                new() {
                { "texture_albedo", ResourceLoader.Load<Texture2D>("res://images/traintrack-uv1.png") } ,
                { "albedo", Vector4.One },
                { "uv1_scale", Vector3.One },
                }
            );
            public readonly static ShaderMaterial Rail_TimedGlow = SetShaderParams(
                new ShaderMaterial() { Shader = ResourceLoader.Load<Shader>("res://models/doubleRail.gdshader") },
                new() {
                { "texture_albedo", ResourceLoader.Load<Texture2D>("res://images/traintrack-uv2.png") } ,
                { "albedo", Vector4.One },
                { "uv1_scale", Vector3.One },
                }
            );

            public readonly static StandardMaterial3D Bar = new()
            {
                AlbedoTexture = GD.Load<Texture2D>("res://images/traintrack-uv1.png"),
            };

            public readonly static StandardMaterial3D Rail = new()
            {
                AlbedoTexture = GD.Load<Texture2D>("res://images/traintrack-uv2.png"),
            };

            public readonly static StandardMaterial3D Ghost = new()
            {
                AlbedoTexture = GD.Load<Texture2D>("res://images/track-ghost.png"),
                Transparency = TransparencyEnum.Alpha,
            };

            public readonly static StandardMaterial3D Preview = new()
            {
                AlbedoTexture = GD.Load<Texture2D>("res://images/track-ghost.png"),
                AlbedoColor = new Color(0, 0, 1),
                Transparency = TransparencyEnum.Alpha,
            };
        }

        public static class HexTile
        {
            public readonly static StandardMaterial3D GrassLg = new()
            {
                AlbedoTexture = GD.Load<Texture2D>("res://images/grass-hex-2-lg.png"),
            };
            public readonly static StandardMaterial3D GlowLg = new()
            {
                AlbedoTexture = GD.Load<Texture2D>("res://images/hex-glow.png"),
                Transparency = TransparencyEnum.Alpha,
                Emission = new(1, 1, 0),
                EmissionEnabled = true,
                CullMode = CullModeEnum.Disabled,
            };
        }

    }
}