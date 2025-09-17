namespace SurvivorGame;

/// <summary>
/// 
/// </summary>
/// <param name="X"></param>
/// <param name="Y"></param>
public record Point(int X , int Y)
{
    private static readonly Point[] Neighbors8 = new[]
    {
        new Point(-1,-1), new Point(0,-1), new Point(1,-1),
        new Point(-1, 0),                  new Point(1, 0),
        new Point(-1, 1), new Point(0, 1), new Point(1, 1),
    };
    public IEnumerable<Point> Around8() => Neighbors8.Select(d => new Point(X + d.X, Y + d.Y));
    public static int Chebyshev(Point a, Point b) => Math.Max(Math.Abs(a.X - b.X), Math.Abs(a.Y - b.Y));
    public override string ToString() => $"({X},{Y})";
}