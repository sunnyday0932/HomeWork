using SurvivorGame.Actors;
using SurvivorGame.Configs;

namespace SurvivorGame;

public class Simulator
{
     public static (string Winner, int SurvivorScore, int KillerScore, int Rounds) RunOne(GameConfig cfg)
        {
            var state = new GameState(cfg);
            Setup.Initialize(state);
            var exitsField = DistanceFieldBuilder.BuildExitField(state);

            for (state.Round = 1; state.Round <= cfg.MaxRounds; state.Round++)
            {
                state.Log.RoundHeader(state.Round);

                // === Killer Phase ===
                var killerMoves = new List<(Killer actor, Point target, double score)>();
                foreach (var k in state.Killers.Where(k => k.Alive))
                {
                    var target = KillerPolicy.DecideNext(state, k, exitsField);
                    var sc = -Point.Chebyshev(target, k.Pos); // 僅供解算排序用
                    killerMoves.Add((k, target, sc));
                }
                var killerDecided = MoveConflictResolver.ResolveNoOverlapSwap(state, killerMoves);
                // 套用 & 捕獲 #1（嘗試進入生存者格）
                ApplyMovesAndCapture(state, killerDecided, killersMove: true);
                if (!state.Survivors.Any(sv => sv.Alive))
                {
                    state.Log.RoundFooter(state);
                    return ("Killer", state.SurvivorScore, state.KillerScore, state.Round);
                }

                // === Survivor Phase ===
                var survivorMoves = new List<(Survivor actor, Point target, double score)>();
                foreach (var sv in state.Survivors.Where(sv => sv.Alive))
                {
                    var target = SurvivorPolicy.DecideNext(state, sv, exitsField);
                    // 用 scoring 近似值：優先靠近出口
                    var sc = -Point.Chebyshev(target, sv.Pos);
                    survivorMoves.Add((sv, target, sc));
                }
                var survivorDecided = MoveConflictResolver.ResolveNoOverlapSwap(state, survivorMoves);
                ApplyMoves<Actor>(state, survivorDecided); // 先移動
                ResolveEscapes(state);               // 逃脫
                ResolveCaptureOnKillerTiles(state);  // 被捕 #2（生踩到鬼）
                state.Log.RoundFooter(state);

                if (!state.Survivors.Any(sv => sv.Alive))
                    return ("Killer", state.SurvivorScore, state.KillerScore, state.Round);

                if (cfg.PrintAsciiMapEachRound) StructuredLogger.PrintAscii(state);
            }

            // 到時仍有生存者 => 鬼勝
            return ("Killer", state.SurvivorScore, state.KillerScore, state.Round);
        }

        private static void ApplyMoves<T>(GameState s, Dictionary<int, Point> decided) where T : Actor
        {
            foreach (var kv in decided)
            {
                var actor = typeof(T) == typeof(Killer)
                    ? (Actor)s.Killers.First(a => a.Id == kv.Key)
                    : (Actor)s.Survivors.First(a => a.Id == kv.Key);
                actor.Pos = kv.Value;
            }
        }

        private static void ApplyMovesAndCapture(GameState s, Dictionary<int, Point> killerMoves, bool killersMove)
        {
            // 鬼套用移動
            foreach (var kv in killerMoves)
            {
                var k = s.Killers.First(a => a.Id == kv.Key);
                k.Pos = kv.Value;
            }
            // 捕獲 #1：鬼移動後若停在生存者原格 => 生被移除
            foreach (var k in s.Killers.Where(k => k.Alive))
            {
                var victims = s.Survivors.Where(sv => sv.Alive && sv.Pos.Equals(k.Pos)).ToList();
                foreach (var v in victims)
                {
                    v.Alive = false;
                    s.KillerScore++;
                    s.Log.Capture(k, v, phase: "KillerPhase");
                }
            }
        }

        private static void ResolveEscapes(GameState s)
        {
            foreach (var sv in s.Survivors.Where(sv => sv.Alive && s.IsExit(sv.Pos)).ToList())
            {
                sv.Alive = false;
                s.SurvivorScore++;
                s.Log.Escape(sv);
            }
        }

        private static void ResolveCaptureOnKillerTiles(GameState s)
        {
            foreach (var sv in s.Survivors.Where(sv => sv.Alive).ToList())
            {
                if (s.Killers.Any(k => k.Alive && k.Pos.Equals(sv.Pos)))
                {
                    sv.Alive = false;
                    s.KillerScore++;
                    var k = s.Killers.First(k => k.Alive && k.Pos.Equals(sv.Pos));
                    s.Log.Capture(k, sv, phase: "SurvivorPhase");
                }
            }
        }
}