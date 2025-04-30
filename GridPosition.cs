namespace traintracks;

public record struct GridPosition(int X, int Y)
{
    public static GridPosition operator +(GridPosition a, GridPosition b) => new(a.X + b.X, a.Y + b.Y);
    public static GridPosition operator -(GridPosition a, GridPosition b) => new(a.X - b.X, a.Y - b.Y);
}
