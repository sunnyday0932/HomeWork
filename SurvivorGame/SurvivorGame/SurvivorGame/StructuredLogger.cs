using SurvivorGame.Actors;
using SurvivorGame.Configs;

namespace SurvivorGame;

public class StructuredLogger(GameConfig cfg)
{
    public void RoundHeader(int round)
    {
        if (cfg.VerboseLog) Console.WriteLine($"\n=== Round {round} ===");
    }

    public void RoundFooter(GameState s)
    {
        if (cfg.VerboseLog)
        {
            Console.WriteLine($"Scores => Survivor:{s.SurvivorScore} Killer:{s.KillerScore} SurvivorsAlive:{s.Survivors.Count(x=>x.Alive)}");
        }
    }

    public void Capture(Killer k, Survivor v, string phase)
    {
        Console.WriteLine($"[CAPTURE] phase={phase} killer={k.Id}@{k.Pos} victim={v.Id}@{v.Pos}");
    }

    public void Escape(Survivor sv)
    {
        Console.WriteLine($"[ESCAPE] survivor={sv.Id}@{sv.Pos}");
    }

    public static void PrintAscii(GameState s)
    {
        var grid = new char[s.Config.Width, s.Config.Height];
        for (int y = 0; y < s.Config.Height; y++)
        for (int x = 0; x < s.Config.Width; x++)
            grid[x, y] = '.';

        foreach (var e in s.Exits)
        {
            grid[e.X, e.Y] = 'E';
        }

        foreach (var k in s.Killers.Where(k => k.Alive))
        {
            grid[k.Pos.X, k.Pos.Y] = char.ToUpperInvariant((char)('a' + k.Id));
        }

        foreach (var v in s.Survivors.Where(v => v.Alive))
        {
            grid[v.Pos.X, v.Pos.Y] = char.ToLowerInvariant((char)('a' + v.Id));
        }
        
        Console.WriteLine();
        for (int y = 0; y < s.Config.Height; y++)
        {
            for (int x = 0; x < s.Config.Width; x++) Console.Write(grid[x, y]);
            Console.WriteLine();
        }
    }
}