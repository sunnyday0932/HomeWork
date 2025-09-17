using SurvivorGame.Actors;

namespace SurvivorGame;

public class SurvivorPolicy
{
     public static Point DecideNext(GameState s, Survivor sv, int[,] dExit)
        {
            var dKiller = DistanceFieldBuilder.BuildVisibleKillerField(s, sv);

            // 更新 last-seen（若看見任一鬼）
            var visible = Vision.VisibleKillers(s, sv).ToList();
            if (visible.Count > 0)
            {
                // 選最近的一隻更新
                var nearest = visible.OrderBy(k => Point.Chebyshev(k.Pos, sv.Pos)).First();
                sv.LastSeenKillerPos = nearest.Pos;
                sv.LastSeenKillerRound = s.Round;
            }

            // 候選（含原地）
            var candidates = s.Neighbors8InBounds(sv.Pos).Append(sv.Pos);

            // 出口優先（相鄰即逃）
            foreach (var n in candidates)
                if (s.IsExit(n)) return n;

            Point best = sv.Pos;
            double bestScore = double.NegativeInfinity;

            foreach (var n in candidates)
            {
                // 邊界檢查已處理；生-生不可同格交給解算器
                int dx = dExit[n.X, n.Y];
                int dk = dKiller[n.X, n.Y];

                // 近身強懲（除非是出口；上面已處理出口）
                if (dk <= s.Config.StrongCloseDanger)
                {
                    // 但若現位置更危險，允許離開
                    int curDk = dKiller[sv.Pos.X, sv.Pos.Y];
                    if (dk < curDk) { /* 允許 */ }
                    else { continue; } // 等效禁止朝危險靠近
                }

                double score = 0;
                score += s.Config.Alpha_Exit * (-dx);
                score += s.Config.Beta_Safety * SafeTransform(dk);
                score += s.Config.Omega_Margin * (SafeTransform(dk) + dx); // dk - dx，但dk經過安全變換

                // 未知區微懲罰（n 的 5x5 若無可見鬼）
                if (!AnyVisibleInWindow(s, n))
                    score -= s.Config.UnknownAreaPenalty;

                // last-seen 陰影
                if (sv.LastSeenKillerPos is Point kp)
                {
                    int r = Math.Min(s.Round - sv.LastSeenKillerRound, s.Config.LastSeenShadowCap);
                    if (Point.Chebyshev(n, kp) <= r) score -= s.Config.LastSeenShadowPenalty;
                }

                // 一步預測：可見鬼下一步若能貼臉
                if (visible.Any() && MightBeAdjacentNextStep(n, visible))
                    score -= s.Config.OneStepLookaheadPenalty;

                if (score > bestScore)
                {
                    bestScore = score;
                    best = n;
                }
            }

            return best;

            static int SafeTransform(int v) => v >= 1_000_000 ? 1000 : v; // 看不到鬼 => 視為很安全
            bool AnyVisibleInWindow(GameState s2, Point center)
            {
                // 檢查以 center 為中心的 5x5 是否能看到至少一隻鬼（相當於：距離≤2 的圈內是否存在鬼）
                return s2.Killers.Any(k => k.Alive && Point.Chebyshev(k.Pos, center) <= s2.Config.SurvivorSight);
            }
            bool MightBeAdjacentNextStep(Point n, List<Killer> vis)
            {
                foreach (var k in vis)
                {
                    // 鬼下一步八方向+原地
                    foreach (var km in s.Neighbors8InBounds(k.Pos).Append(k.Pos))
                        if (Point.Chebyshev(km, n) <= 1) return true;
                }
                return false;
            }
        }
}