using Godot;

namespace traintracks;

public static class Vector3Helpers
{
    public static Vector3 RandomJitter(float seed) => new Vector3(0.001f * Mathf.Sin(seed), 0.001f * Mathf.Sin(seed), 0.001f * Mathf.Sin(seed));
}
