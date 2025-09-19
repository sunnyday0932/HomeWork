using SurvivorGame.Actors;

namespace SurvivorGame.Tests.Helper;

/// <summary>
/// 在固定的 GameState 上，單獨執行 Killer Phase / Survivor Phase 的小工具。
/// 專供測試「事件順序」與「回合收尾」。
/// </summary>
public class StepRunner
{
    public static int[,] BuildExitField(GameState gameState)
    {
        return DistanceFieldBuilder.BuildExitField(gameState);
    }

    public static void RunKillerPhase(GameState gameState, int[,] exitField)
    {
        var killerMoves = new List<(Killer actor, Point target, double score)>();
        foreach (var killer in gameState.Killers.Where(k => k.Alive))
        {
            var target = KillerPolicy.DecideNext(gameState, killer, exitField);
            var sc = -Point.Chebyshev(target, killer.Pos);
            killerMoves.Add((killer, target, sc));
        }

        var decided = MoveConflictResolver.ResolveNoOverlapSwap(gameState, killerMoves);
        // 套用移動
        foreach (var kv in decided)
        {
            var killer = gameState.Killers.First(a => a.Id == kv.Key);
            killer.Pos = kv.Value;
        }

        // 捕獲 #1：鬼停到生存者所在格 -> 生存者移除 +1 分
        foreach (var killer in gameState.Killers.Where(k => k.Alive))
        {
            var victims = gameState.Survivors.Where(sv => sv.Alive && sv.Pos.Equals(killer.Pos)).ToList();
            foreach (var survivor in victims)
            {
                survivor.Alive = false;
                gameState.KillerScore++;
            }
        }
    }

    public static void RunSurvivorPhase(GameState gameState, int[,] exitField)
    {
        var survivorMoves = new List<(Survivor actor, Point target, double score)>();
        foreach (var survivor in gameState.Survivors.Where(sv => sv.Alive))
        {
            var target = SurvivorPolicy.DecideNext(gameState, survivor, exitField);
            var sc = -Point.Chebyshev(target, survivor.Pos);
            survivorMoves.Add((survivor, target, sc));
        }

        var decided = MoveConflictResolver.ResolveNoOverlapSwap(gameState, survivorMoves);

        // 先套用移動
        foreach (var kv in decided)
        {
            var survivor = gameState.Survivors.First(a => a.Id == kv.Key);
            survivor.Pos = kv.Value;
        }

        // 先判逃脫
        foreach (var survivor in gameState.Survivors.Where(sv => sv.Alive && gameState.IsExit(sv.Pos)).ToList())
        {
            survivor.Alive = false;
            gameState.SurvivorScore++;
        }

        // 再判與鬼同格的被捕
        foreach (var survivor in gameState.Survivors.Where(sv => sv.Alive).ToList())
        {
            if (gameState.Killers.Any(k => k.Alive && k.Pos.Equals(survivor.Pos)))
            {
                survivor.Alive = false;
                gameState.KillerScore++;
            }
        }
    }

    public static string DecideWinnerOnTimeout(GameState gameState)
    {
        // 規則：時間到且仍有生存者 => 鬼勝；若提早無生存者 => 鬼勝（但本函數只用於 timeout 場景）
        if (gameState.Survivors.Any(sv => sv.Alive))
        {
            return "Killer";
        }

        return "Survivor"; // 理論上 timeout 時通常不會全逃，但保留語義完整性
    }
}