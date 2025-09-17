using SurvivorGame.Actors;
using SurvivorGame.Enums;

namespace SurvivorGame;

/// <summary>
/// 
/// </summary>
public class Setup
{
    public static void Initialize(GameState s)
        {
            // 出口：放在四角或邊界分散
            var cfg = s.Config;
            var corners = new[]
            {
                new Point(1,1),
                new Point(cfg.Width-2,1),
                new Point(1,cfg.Height-2),
                new Point(cfg.Width-2,cfg.Height-2),
            };
            foreach (var e in corners.Take(cfg.ExitCount))
            {
                s.Map[e.X, e.Y] = TileType.Exit;
                s.Exits.Add(e);
            }

            // 隨機放生存者與鬼（滿足距離約束）
            var occupied = new HashSet<Point>();
            PlaceActors<Survivor>(s, cfg.SurvivorCount, occupied, allyMin: cfg.MinDistAlly);
            PlaceActors<Killer>(s, cfg.KillerCount, occupied, allyMin: cfg.MinDistAlly);
        }

        private static void PlaceActors<T>(GameState s, int count, HashSet<Point> occupied, int allyMin) where T : Actor
        {
            int placed = 0;
            int guard = 0;
            while (placed < count)
            {
                guard++; if (guard > 1_000_000) throw new Exception("Spawn failed");

                var p = new Point(s.Rng.Next(0, s.Config.Width), s.Rng.Next(0, s.Config.Height));
                if (occupied.Contains(p)) continue;
                if (s.IsExit(p)) continue;

                // 與出口距離限制
                if (s.Exits.Any(e => Point.Chebyshev(e, p) < s.Config.MinDistToExit)) continue;

                // 與敵對最小距離
                if (typeof(T) == typeof(Survivor))
                {
                    if (s.Killers.Any(k => Point.Chebyshev(k.Pos, p) < s.Config.MinDistEnemy)) continue;
                    if (s.Survivors.Any(a => Point.Chebyshev(a.Pos, p) < allyMin)) continue;
                    var sv = new Survivor(placed, p);
                    s.Survivors.Add(sv);
                }
                else
                {
                    if (s.Survivors.Any(k => Point.Chebyshev(k.Pos, p) < s.Config.MinDistEnemy)) continue;
                    if (s.Killers.Any(a => Point.Chebyshev(a.Pos, p) < allyMin)) continue;
                    var ki = new Killer(placed, p);
                    s.Killers.Add(ki);
                }

                occupied.Add(p);
                placed++;
            }
        }
}