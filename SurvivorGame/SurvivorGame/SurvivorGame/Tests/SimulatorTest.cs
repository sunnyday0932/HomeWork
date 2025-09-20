using SurvivorGame.Configs;
using SurvivorGame.Extension;

namespace SurvivorGame.Tests;

public class SimulatorTest
{
    [Fact (DisplayName = "單執行緒模擬", Skip = ("需要的時候自行打開"))]
    public void SimulatorSingleThread()
    {
        var config = new GameConfig
        {
            Width = 100,
            Height = 100,
            MaxRounds = 100,
            SurvivorCount = 3,
            KillerCount = 1,
            SurvivorSight = 2, // 5x5
            KillerSight = 3, // 7x7
            ExitCount = 2,
            Seed = 102,
            VerboseLog = false,
            PrintAsciiMapEachRound = false
        };

        var episodes = 300;
        SimulatorExtensions.RunAndSaveBatch(config, episodes: episodes, outputDir: $"../../../../docs/Episode{episodes}_Killer{config.KillerCount}_Survivor_{config.SurvivorCount}_Seed_{config.Seed}_Result");
    }
    
    [Fact (DisplayName = "多執行緒模擬")]
    public void SimulatorMultiThread()
    {
        var config = new GameConfig
        {
            Width = 100,
            Height = 100,
            MaxRounds = 100,
            SurvivorCount = 3,
            KillerCount = 1,
            SurvivorSight = 2, // 5x5
            KillerSight = 3,   // 7x7
            ExitCount = 2,
            Seed = 942,
            VerboseLog = false,
            PrintAsciiMapEachRound = false
        };

        var episodes = 300;
        SimulatorExtensions.RunAndSaveBatchParallel(config, episodes: episodes, outputDir: $"../../../../docs/Episode{episodes}_Killer{config.KillerCount}_Survivor_{config.SurvivorCount}_Seed_{config.Seed}_Result");
    }
}