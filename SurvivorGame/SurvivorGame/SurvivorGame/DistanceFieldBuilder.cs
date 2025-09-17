using SurvivorGame.Actors;

namespace SurvivorGame;

/// <summary>
/// 
/// </summary>
public class DistanceFieldBuilder
{
    private const int Inf = 1_000_000;

    // 多源 BFS（Chebyshev，八方向步長=1）
    public static int[,] Build(GameState s, IEnumerable<Point> sources)
    {
        var W = s.Config.Width;
        var H = s.Config.Height;
        var dist = new int[W, H];
        for (int x = 0; x < W; x++)
        for (int y = 0; y < H; y++)
            dist[x, y] = Inf;

        var q = new Queue<Point>();
        foreach (var src in sources)
        {
            if (!s.InBounds(src)) continue;
            dist[src.X, src.Y] = 0;
            q.Enqueue(src);
        }

        while (q.Count > 0)
        {
            var cur = q.Dequeue();
            var d = dist[cur.X, cur.Y];
            foreach (var nxt in s.Neighbors8InBounds(cur))
            {
                if (dist[nxt.X, nxt.Y] <= d + 1) continue;
                dist[nxt.X, nxt.Y] = d + 1;
                q.Enqueue(nxt);
            }
        }

        return dist;
    }

    public static int[,] BuildExitField(GameState s) => Build(s, s.Exits);

    public static int[,] BuildVisibleKillerField(GameState s, Survivor sv)
    {
        var visibleKillers = s.Killers.Where(k => k.Alive && Point.Chebyshev(k.Pos, sv.Pos) <= s.Config.SurvivorSight)
            .Select(k => k.Pos)
            .ToList();
        if (visibleKillers.Count == 0)
        {
            // 視為極安全場（距離很大）
            var dist = new int[s.Config.Width, s.Config.Height];
            for (int x = 0; x < s.Config.Width; x++)
            for (int y = 0; y < s.Config.Height; y++)
                dist[x, y] = Inf;
            return dist;
        }

        return Build(s, visibleKillers);
    }

    public static int[,] BuildVisibleSurvivorField(GameState s, Killer ki, IEnumerable<Point> targets)
    {
        // 鬼看得到的生存者 or last-seen or 出口
        return Build(s, targets);
    }
}