using SurvivorGame.Actors;
using SurvivorGame.Configs;
using SurvivorGame.Enums;

namespace SurvivorGame;

public class StructuredLogger(GameConfig cfg)
{
    public readonly System.Collections.Generic.List<GameEvent> Events = new System.Collections.Generic.List<GameEvent>();
    internal int CurrentEpisode { get; set; } = 0;
    
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

    public void Capture(Killer killer, Survivor victim, string phase)
    {
        Console.WriteLine($"[CAPTURE] phase={phase} killer={killer.Id}@{killer.Pos} victim={victim.Id}@{victim.Pos}");
        Events.Add(new GameEvent
        {
            Episode = CurrentEpisode,
            Round = 0, // 佔位，不用這行也行
            // 正確 round 由外層填（見 RunOneWithEvents），這裡先佔位
            Type = GameEventType.Capture,
            SurvivorId = victim.Id,
            KillerId = killer.Id,
            X = killer.Pos.X,
            Y = killer.Pos.Y
        });
    }

    public void Escape(Survivor survivor)
    {
        Console.WriteLine($"[ESCAPE] survivor={survivor.Id}@{survivor.Pos}");
        Events.Add(new GameEvent
        {
            Episode = CurrentEpisode,
            Round = 0, // 佔位，稍後由外層補 round
            Type = GameEventType.Escape,
            SurvivorId = survivor.Id,
            KillerId = null,
            X = survivor.Pos.X,
            Y = survivor.Pos.Y
        });
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