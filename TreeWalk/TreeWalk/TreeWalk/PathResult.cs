namespace TreeWalk;

/// <summary>
/// 
/// </summary>
public class PathResult
{
    public required int Distance { get; init; }
    public required IReadOnlyList<string> Directions { get; init; }
}