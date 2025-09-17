using SurvivorGame.Actors;

namespace SurvivorGame;

public class KillerPolicy
{
     public static Point DecideNext(GameState s, Killer ki, int[,] dExit)
        {
            // 目標選擇：視野內生存者 → 最近；否則 last-seen；再不然巡邏最近出口
            var visible = Vision.VisibleSurvivors(s, ki).ToList();
            IEnumerable<Point> targets;
            if (visible.Count > 0)
            {
                var nearest = visible.OrderBy(a => Point.Chebyshev(a.Pos, ki.Pos)).First();
                ki.LastSeenSurvivorPos = nearest.Pos;
                ki.LastSeenSurvivorRound = s.Round;
                targets = new[] { nearest.Pos };
            }
            else if (ki.LastSeenSurvivorPos is Point ls && s.Round - ki.LastSeenSurvivorRound <= s.Config.LastSeenTimeout)
            {
                targets = new[] { ls };
            }
            else
            {
                // 巡邏最近出口
                var ex = s.Exits.OrderBy(e => Point.Chebyshev(e, ki.Pos)).First();
                targets = new[] { ex };
            }

            var dTarget = DistanceFieldBuilder.BuildVisibleSurvivorField(s, ki, targets);

            // 候選（含原地）
            var candidates = s.Neighbors8InBounds(ki.Pos).Append(ki.Pos);

            Point best = ki.Pos;
            double bestScore = double.NegativeInfinity;

            foreach (var n in candidates)
            {
                double score = 0;
                score += -dTarget[n.X, n.Y]; // 縮短到目標距離

                // spacing：避免鬼擠在一起
                int minDistToOther = s.Killers.Where(k => k.Alive && k.Id != ki.Id)
                                              .Select(k => Point.Chebyshev(k.Pos, n))
                                              .DefaultIfEmpty(999).Min();
                if (minDistToOther <= 1) score -= s.Config.KillerSpacingPenalty;

                // 簡單的攔截加成：靠近「任一生存者→最近出口」路徑就加分（POC近似：靠近出口）
                var nearestExit = s.Exits.OrderBy(e => Point.Chebyshev(e, n)).First();
                score += s.Config.KillerInterceptBonus * (1.0 / (1 + Point.Chebyshev(n, nearestExit)));

                if (score > bestScore) { bestScore = score; best = n; }
            }

            return best;
        }
}