namespace SurvivorGame;

/// <summary>
/// 
/// </summary>
public class MoveConflictResolver
{
     public static Dictionary<int, Point> ResolveNoOverlapSwap<T>(GameState s, IEnumerable<(T actor, Point target, double score)> submissions)
            where T : Actor
        {
            // 1) 同格競爭：以 score 高者贏，score 相同以 id 小者
            var byCell = new Dictionary<Point, List<(T actor, Point target, double score)>>();
            foreach (var m in submissions)
            {
                if (!byCell.TryGetValue(m.target, out var list))
                    byCell[m.target] = list = new();
                list.Add(m);
            }

            var result = new Dictionary<int, Point>();
            var winners = new HashSet<int>();
            foreach (var kv in byCell)
            {
                var winner = kv.Value
                    .OrderByDescending(x => x.score)
                    .ThenBy(x => x.actor.Id)
                    .First();

                result[winner.actor.Id] = winner.target;
                winners.Add(winner.actor.Id);
            }

            // 2) 失敗者原地
            foreach (var m in submissions)
                if (!winners.Contains(m.actor.Id))
                    result[m.actor.Id] = m.actor.Pos;

            // 3) 禁止對向交換：A:X->Y + B:Y->X => 都原地
            var toRevert = new HashSet<int>();
            foreach (var a in submissions)
            {
                foreach (var b in submissions)
                {
                    if (a.actor.Id >= b.actor.Id) continue;
                    var aTarget = result[a.actor.Id];
                    var bTarget = result[b.actor.Id];
                    if (a.actor.Pos.Equals(bTarget) && b.actor.Pos.Equals(aTarget))
                    {
                        toRevert.Add(a.actor.Id);
                        toRevert.Add(b.actor.Id);
                    }
                }
            }
            foreach (var id in toRevert)
            {
                // 原地
                var actor = submissions.First(x => x.actor.Id == id).actor;
                result[id] = actor.Pos;
            }

            return result;
        }
}