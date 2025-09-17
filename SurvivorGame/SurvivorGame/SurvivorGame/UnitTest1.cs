using SurvivorGame.Configs;

namespace SurvivorGame;

public class UnitTest1
{
    [Fact]
    public void Test1()
    {
        var config = new GameConfig
        {
            Width = 50,
            Height = 50,
            MaxRounds = 100,
            SurvivorCount = 3,
            KillerCount = 3,
            SurvivorSight = 2, // 5x5
            KillerSight = 3,   // 7x7
            VerboseLog = true,
            PrintAsciiMapEachRound = false, // true 會列印每回合地圖（大量）
        };

        var (winner, sScore, kScore, rounds) = Simulator.RunOne(config);
        Console.WriteLine($"\nWinner={winner}, SScore={sScore}, KScore={kScore}, Rounds={rounds}");
        
        
        Assert.True(true);
    }
}