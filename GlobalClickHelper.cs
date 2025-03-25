using Godot;

namespace traintracks;

public interface IClickable
{
    void OnClick();
    void OnUnClick();
}

public static class GlobalClickHelper
{
    public static IClickable? LastClicked { get; private set; }

    public static void WasClicked(IClickable? shape)
    {
        LastClicked?.OnUnClick();
        LastClicked = shape;
        shape?.OnClick();
    }
}
