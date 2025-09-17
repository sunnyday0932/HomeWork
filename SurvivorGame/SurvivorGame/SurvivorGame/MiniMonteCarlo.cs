using SurvivorGame.Configs;

namespace SurvivorGame;

public class MiniMonteCarlo
{
    public static void RunBatch(GameConfig cfg, int episodes)
    {
        int killerWins = 0, survivorPoints = 0, killerPoints = 0;
        var dur = new List<int>();
        for (int i = 0; i < episodes; i++)
        {
            cfg.Seed = cfg.Seed + 1;
            cfg.VerboseLog = false;
            cfg.PrintAsciiMapEachRound = false;
            
            var (winner, sScore, kScore, rounds) = Simulator.RunOne(cfg);
            if (winner == "Killer") killerWins++;
            survivorPoints += sScore;
            killerPoints += kScore;
            dur.Add(rounds);
        }

        Console.WriteLine($"\n--- Batch Result ({episodes} games) ---");
        Console.WriteLine($"Killer win rate: {(double)killerWins / episodes:P1}");
        Console.WriteLine($"Avg survivor points: {(double)survivorPoints / episodes:0.00}");
        Console.WriteLine($"Avg killer points: {(double)killerPoints / episodes:0.00}");
        Console.WriteLine($"Avg rounds: {dur.Average():0.0}");
    }
}