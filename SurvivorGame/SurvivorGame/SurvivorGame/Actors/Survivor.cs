namespace SurvivorGame.Actors;

/// <summary>
/// Survivor
/// </summary>
/// <param name="id"></param>
/// <param name="pos"></param>
public class Survivor(int id, Point pos) : Actor(id, pos)
{
    public Point? LastSeenKillerPos { get; set; }
    public int LastSeenKillerRound { get; set; } = -9999;
}