using SurvivorGame.Actors;
using SurvivorGame.Configs;
using SurvivorGame.Enums;

namespace SurvivorGame.Tests.Helper;

/// <summary>
/// 直接以固定佈局建立 GameState，避免隨機初始化。
/// </summary>
public class TestBoardBuilder
{
    public static GameState BuildFixed(
        GameConfig config,
        IEnumerable<Point> exits,
        IEnumerable<Point> survivors,
        IEnumerable<Point> killers)
    {
        var gameState = new GameState(config);

        // 佈置出口
        foreach (var exitPoint in exits)
        {
            gameState.Map[exitPoint.X, exitPoint.Y] = TileType.Exit;
            gameState.Exits.Add(exitPoint);
        }

        // 佈置生存者
        int survivorId = 0;
        foreach (var pos in survivors)
        {
            var survivor = new Survivor(survivorId++, pos);
            gameState.Survivors.Add(survivor);
        }

        // 佈置鬼
        int killerId = 0;
        foreach (var pos in killers)
        {
            var killer = new Killer(killerId++, pos);
            gameState.Killers.Add(killer);
        }

        // 簡單檢查：所有座標在界內、初始互不重疊
        var all = gameState.Survivors.Select(x => x.Pos)
            .Concat(gameState.Killers.Select(x => x.Pos))
            .ToList();
        if (all.Distinct().Count() != all.Count)
        {
            throw new ArgumentException("Initial actors overlap in BuildFixed().");
        }
        foreach (var p in all.Concat(gameState.Exits))
        {
            if (!gameState.InBounds(p))
            {
                throw new ArgumentException($"Point {p} out of bounds in BuildFixed().");
            }
        }

        // 從第 1 回合開始（與主模擬一致）
        gameState.Round = 1;
        return gameState;
    }
}