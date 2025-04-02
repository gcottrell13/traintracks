using Godot;

namespace traintracks;

public interface IClickable
{
    void OnClick();
    void OnUnClick();
}

public interface IMouseEnterable
{
    void OnEnter();
    void OnLeave();
}

public static class GlobalClickHelper
{
    public static IClickable? LastClicked { get; private set; }

    public static IMouseEnterable? LastEntered { get; private set; }

    public static void WasClicked(IClickable? shape, bool reClick = true)
    {
        if (!reClick && shape == LastClicked)
            return;
        LastClicked?.OnUnClick();
        LastClicked = shape;
        shape?.OnClick();
    }

    public static void WasEntered(IMouseEnterable? shape, bool reEnter = false)
    {
        if (!reEnter && shape == LastEntered)
            return;
        LastEntered?.OnLeave();
        LastEntered = shape;
        shape?.OnEnter();
    }
}
