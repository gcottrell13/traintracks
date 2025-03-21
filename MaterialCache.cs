using Godot;
using System.Collections.Generic;

namespace traintracks;

public static class MaterialCache
{
    private static Dictionary<string, StandardMaterial3D> Cache = new();
    public static StandardMaterial3D? GetMaterial(Texture2D? texture)
    {
        if (texture == null)
            return null;
        var path = texture.ResourcePath;
        if (!Cache.TryGetValue(path, out var material))
        {
            material = new StandardMaterial3D();
            material.AlbedoTexture = texture;
            Cache[path] = material;
        }
        return material;
    }

    public static class Images
    {
        public static StandardMaterial3D? TrainTrackUV1 => GetMaterial(ResourceLoader.Load("res://images/traintrack-uv1.png") as Texture2D);
    }
}