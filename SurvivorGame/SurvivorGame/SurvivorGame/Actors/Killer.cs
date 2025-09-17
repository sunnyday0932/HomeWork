namespace SurvivorGame.Actors;

/// <summary>
/// 
/// </summary>
/// <param name="id"></param>
/// <param name="pos"></param>
public class Killer(int id, Point pos) : Actor(id, pos)
{
    public Point? LastSeenSurvivorPos { get; set; }
    public int LastSeenSurvivorRound { get; set; } = -9999;
}