using SurvivorGame.Configs;

namespace SurvivorGame.Tests;

/// <summary>
/// 統計穩健性（32–33）
/// 32) 相同參數下，多批次勝率差不應過大（穩定性）
/// 33) 單一旋鈕單調性：SurvivorSight 2 → 3 不應讓鬼勝率上升，且不應讓生存者平均得分下降
/// </summary>
public class MonteCarloSmokeTest
{
    private readonly record struct BatchStats(
        double KillerWinRate,
        double AvgSurvivorPoints,
        double AvgKillerPoints,
        double AvgRounds);

    private static BatchStats RunBatchForStats(GameConfig baseConfig, int episodes, int seedOffset)
    {
        int killerWins = 0;
        int totalSurvivorPoints = 0;
        int totalKillerPoints = 0;
        int totalRounds = 0;

        for (int i = 0; i < episodes; i++)
        {
            var config = new GameConfig
            {
                Width = baseConfig.Width,
                Height = baseConfig.Height,
                MaxRounds = baseConfig.MaxRounds,
                SurvivorCount = baseConfig.SurvivorCount,
                KillerCount = baseConfig.KillerCount,
                SurvivorSight = baseConfig.SurvivorSight,
                KillerSight = baseConfig.KillerSight,
                ExitCount = baseConfig.ExitCount,
                Seed = baseConfig.Seed + seedOffset + i,
                VerboseLog = false,
                PrintAsciiMapEachRound = false
            };

            var (winner, survivorScore, killerScore, rounds) = Simulator.RunOne(config);

            if (winner == "Killer")
            {
                killerWins++;
            }

            totalSurvivorPoints += survivorScore;
            totalKillerPoints += killerScore;
            totalRounds += rounds;
        }

        double winRate = (double)killerWins / episodes;
        double avgS = (double)totalSurvivorPoints / episodes;
        double avgK = (double)totalKillerPoints / episodes;
        double avgR = (double)totalRounds / episodes;

        return new BatchStats(winRate, avgS, avgK, avgR);
    }

    [Fact(DisplayName = "穩定性：同參數下不同隨機種子，鬼勝率差異不應過大", Skip = "需要時自行打開")]
    public void Stability_KillerWinRate_ShouldNotVaryTooMuchAcrossBatches()
    {
        // 基準配置：50x50、3v3、視野 5x5 / 7x7、回合 100（與主程式預設一致）
        var baseConfig = new GameConfig
        {
            Width = 50,
            Height = 50,
            MaxRounds = 100,
            SurvivorCount = 3,
            KillerCount = 3,
            SurvivorSight = 2,
            KillerSight = 3,
            ExitCount = 2,
            Seed = 1000
        };

        int episodes = 150; // 小批量即可觀察趨勢
        double allowedDiff = 0.20; // 允許批次間最大 20% 的波動

        var batchA = RunBatchForStats(baseConfig, episodes, seedOffset: 0);
        var batchB = RunBatchForStats(baseConfig, episodes, seedOffset: 10_000);

        // 鬼勝率不應差太多（避免回歸造成極端不穩定）
        double diff = Math.Abs(batchA.KillerWinRate - batchB.KillerWinRate);
        Assert.True(diff <= allowedDiff, $"Win rate drift too large: Δ={diff:0.000} (> {allowedDiff:0.00})");

        // 一些基本健全檢查
        Assert.InRange(batchA.AvgRounds, 1, baseConfig.MaxRounds);
        Assert.InRange(batchB.AvgRounds, 1, baseConfig.MaxRounds);
        Assert.True(batchA.AvgSurvivorPoints >= 0 && batchB.AvgSurvivorPoints >= 0,
            "Avg survivor points should be non-negative.");
        Assert.True(batchA.AvgKillerPoints >= 0 && batchB.AvgKillerPoints >= 0,
            "Avg killer points should be non-negative.");
    }

    [Fact(DisplayName = "單調性：SurvivorSight ↑ 時，鬼勝率不應上升；生存者平均分不應下降",Skip = "需要時自行打開")]
    public void Monotonicity_SurvivorSight_Increase_ShouldNotHelpKillers()
    {
        var baseConfigLowSight = new GameConfig
        {
            Width = 50,
            Height = 50,
            MaxRounds = 100,
            SurvivorCount = 3,
            KillerCount = 3,
            SurvivorSight = 2, // 5x5
            KillerSight = 3,   // 7x7
            ExitCount = 2,
            Seed = 2000
        };

        // 原本 with 改為 new 初始化
        var baseConfigHighSight = new GameConfig
        {
            Width = baseConfigLowSight.Width,
            Height = baseConfigLowSight.Height,
            MaxRounds = baseConfigLowSight.MaxRounds,
            SurvivorCount = baseConfigLowSight.SurvivorCount,
            KillerCount = baseConfigLowSight.KillerCount,
            SurvivorSight = 3, // 7x7 視野給生存者
            KillerSight = baseConfigLowSight.KillerSight,
            ExitCount = baseConfigLowSight.ExitCount,
            Seed = baseConfigLowSight.Seed
        };

        int episodes = 150;
        double epsilon = 0.05; // 允許小幅隨機抖動

        var low = RunBatchForStats(baseConfigLowSight, episodes, seedOffset: 0);
        var high = RunBatchForStats(baseConfigHighSight, episodes, seedOffset: 10_000);

        // 生存者視野變大，不應讓「鬼勝率」上升
        Assert.True(high.KillerWinRate <= low.KillerWinRate + epsilon,
            $"Killer win rate increased unexpectedly when SurvivorSight increased: low={low.KillerWinRate:0.000}, high={high.KillerWinRate:0.000}, eps={epsilon:0.00}");

        // 也不應讓生存者平均得分下降
        Assert.True(high.AvgSurvivorPoints + epsilon >= low.AvgSurvivorPoints,
            $"Avg survivor points dropped unexpectedly when SurvivorSight increased: low={low.AvgSurvivorPoints:0.000}, high={high.AvgSurvivorPoints:0.000}, eps={epsilon:0.00}");
    }
}