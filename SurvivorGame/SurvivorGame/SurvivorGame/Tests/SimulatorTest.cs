using SurvivorGame.Configs;
using SurvivorGame.Extension;

namespace SurvivorGame.Tests;

public class SimulatorTest
{
    [Fact]
    public void Test()
    {
        var config = new GameConfig
        {
            Width = 100,
            Height = 100,
            MaxRounds = 100,
            SurvivorCount = 3,
            KillerCount = 2,
            SurvivorSight = 2, // 5x5
            KillerSight = 3,   // 7x7
            ExitCount = 2,
            Seed = 42,
            VerboseLog = false,
            PrintAsciiMapEachRound = false
        };

        // 跑 300 場（可依你需求調整），輸出到 ./TestResult
        SimulatorExtensions.RunAndSaveBatch(config, episodes: 300, outputDir: "TestResult");

        Console.WriteLine("Batch done. See ./TestResult for NDJSON per episode, summary.csv, and aggregate.json");
    }
}