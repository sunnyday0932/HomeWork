using SurvivorGame.Configs;

namespace SurvivorGame.Tests;

public class TestHelpers
{
    /// <summary>
    /// 依給定 config 建立遊戲狀態，並套用隨機化的 Setup.Initialize。
    /// 用於驗證初始化不變式。
    /// </summary>
    public static GameState BuildInitializedState(GameConfig cfg)
    {
        var state = new GameState(cfg);
        Setup.Initialize(state);
        return state;
    }

    /// <summary>
    /// 取得所有角色位置（存活者），依照 類型→Id 穩定排序。
    /// </summary>
    public static IReadOnlyList<(string kind, int id, Point pos)> GetAllActorPositions(GameState s)
    {
        var survivors = s.Survivors.Where(x => x.Alive).OrderBy(x => x.Id)
            .Select(x => (kind: "S", id: x.Id, pos: x.Pos));
        var killers = s.Killers.Where(x => x.Alive).OrderBy(x => x.Id)
            .Select(x => (kind: "K", id: x.Id, pos: x.Pos));
        return survivors.Concat(killers).ToList();
    }

    public static bool InBounds(GameState s, Point p)
        => p.X >= 0 && p.Y >= 0 && p.X < s.Config.Width && p.Y < s.Config.Height;

    public static int Chebyshev(Point a, Point b) => Point.Chebyshev(a, b);
}