using SurvivorGame.Actors;

namespace SurvivorGame;

public class KillerPolicy
{
     public static Point DecideNext(GameState gamestate, Killer killer, int[,] dExit)
        {
            // 目標選擇：視野內生存者 → 最近；否則 last-seen；再不然巡邏最近出口
            var visible = Vision.VisibleSurvivors(gamestate, killer).ToList();
            IEnumerable<Point> targets;
            if (visible.Count > 0)
            {
                var nearest = visible.OrderBy(a => Point.Chebyshev(a.Pos, killer.Pos)).First();
                killer.LastSeenSurvivorPos = nearest.Pos;
                killer.LastSeenSurvivorRound = gamestate.Round;
                targets = new[] { nearest.Pos };
            }
            else if (killer.LastSeenSurvivorPos is Point ls && gamestate.Round - killer.LastSeenSurvivorRound <= gamestate.Config.LastSeenTimeout)
            {
                targets = new[] { ls };
            }
            else
            {
                // 巡邏最近出口
                var ex = gamestate.Exits.OrderBy(e => Point.Chebyshev(e, killer.Pos)).First();
                targets = new[] { ex };
            }

            var dTarget = DistanceFieldBuilder.BuildVisibleSurvivorField(gamestate, killer, targets);

            // 候選（含原地）
            var candidates = gamestate.Neighbors8InBounds(killer.Pos).Append(killer.Pos);

            Point best = killer.Pos;
            double bestScore = double.NegativeInfinity;

            foreach (var n in candidates)
            {
                double score = 0;
                score += -dTarget[n.X, n.Y]; // 縮短到目標距離

                // spacing：避免鬼擠在一起
                int minDistToOther = gamestate.Killers.Where(k => k.Alive && k.Id != killer.Id)
                                              .Select(k => Point.Chebyshev(k.Pos, n))
                                              .DefaultIfEmpty(999).Min();
                if (minDistToOther <= 1) score -= gamestate.Config.KillerSpacingPenalty;

                // 簡單的攔截加成：靠近「任一生存者→最近出口」路徑就加分（POC近似：靠近出口）
                var nearestExit = gamestate.Exits.OrderBy(e => Point.Chebyshev(e, n)).First();
                score += gamestate.Config.KillerInterceptBonus * (1.0 / (1 + Point.Chebyshev(n, nearestExit)));

                if (score > bestScore) { bestScore = score; best = n; }
            }

            return best;
        }
}