using SurvivorGame.Actors;
using SurvivorGame.Configs;
using SurvivorGame.Enums;

namespace SurvivorGame;

public class GameState
{
    public GameConfig Config { get; }
    public TileType[,] Map { get; }
    public List<Point> Exits { get; } = new();
    public List<Survivor> Survivors { get; } = new();
    public List<Killer> Killers { get; } = new();
    public int Round { get; set; } = 0;
    public int SurvivorScore { get; set; } = 0;
    public int KillerScore { get; set; } = 0;
    public Random Rng { get; }
    public StructuredLogger Log { get; }

    public GameState(GameConfig cfg)
    {
        Config = cfg;
        Map = new TileType[cfg.Width, cfg.Height];
        Rng = new Random(cfg.Seed);
        Log = new StructuredLogger(cfg);
    }

    public bool InBounds(Point p) => p.X >= 0 && p.Y >= 0 && p.X < Config.Width && p.Y < Config.Height;
    public bool IsExit(Point p) => Map[p.X, p.Y] == TileType.Exit;
    public IEnumerable<Point> Neighbors8InBounds(Point p) => p.Around8().Where(InBounds);
}