using SurvivorGame.Actors;

namespace SurvivorGame;

/// <summary>
/// Vision
/// </summary>
public class Vision
{
    public static IEnumerable<Survivor> VisibleSurvivors(GameState s, Killer k)
        => s.Survivors.Where(a => a.Alive && Point.Chebyshev(a.Pos, k.Pos) <= s.Config.KillerSight);

    public static IEnumerable<Killer> VisibleKillers(GameState s, Survivor sv)
        => s.Killers.Where(a => a.Alive && Point.Chebyshev(a.Pos, sv.Pos) <= s.Config.SurvivorSight);
}