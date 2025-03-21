using Godot;
using System.Collections.Generic;

namespace traintracks;

public static class MaterialCache
{
    private readonly static Dictionary<string, StandardMaterial3D> StandardMaterial3DCache = [];
    public static StandardMaterial3D GetMaterial(Texture2D texture)
    {
        var path = texture.ResourcePath;
        if (!StandardMaterial3DCache.TryGetValue(path, out var material))
        {
            material = new StandardMaterial3D
            {
                AlbedoTexture = texture
            };
            StandardMaterial3DCache[path] = material;
        }
        return material;
    }

    private static ShaderMaterial SetShaderParams(ShaderMaterial shader, Dictionary<string, Variant> parameters)
    {
        foreach (var key in parameters.Keys)
            shader.SetShaderParameter(key, parameters[key]);
        return shader;
    }

    public static class Images
    {
        public readonly static Material TrainTrackUV1 = GetMaterial(ResourceLoader.Load<Texture2D>("res://images/traintrack-uv1.png"));
        public readonly static ShaderMaterial TrainTrackUV2 = SetShaderParams(
            new ShaderMaterial() { Shader = ResourceLoader.Load<Shader>("res://models/doubleRail.gdshader") },
            new() { 
                { "texture_albedo", ResourceLoader.Load<Texture2D>("res://images/traintrack-uv2.png") } ,
                { "albedo", Vector4.One },
                { "uv1_scale", Vector3.One },
            }
            );
    }
}