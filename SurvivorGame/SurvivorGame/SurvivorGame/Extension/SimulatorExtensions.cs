using System.Globalization;
using SurvivorGame.Actors;
using SurvivorGame.Configs;

namespace SurvivorGame.Extension;

public static class SimulatorExtensions
{
    /// <summary>
    /// 跑單場並回傳：勝者 / 分數 / 回合數 / 逐回合事件。
    /// 此函式為保留你原有 RunOne 的語意，僅在事件產生時把 Round/EP 填正。
    /// </summary>
    private static (string Winner, int SurvivorScore, int KillerScore, int Rounds, List<GameEvent> Events)
        RunOneWithEvents(GameConfig config, int episode)
    {
        var state = new GameState(config);
        state.Log.CurrentEpisode = episode;

        Setup.Initialize(state);
        var exitsField = DistanceFieldBuilder.BuildExitField(state);

        // 本地方法：把剛新增進 Events 的 round 填正
        void StampRoundOnNewEvents(int round)
        {
            // 從尾端往回補到第一個尚未 stamp 的事件
            for (int i = state.Log.Events.Count - 1; i >= 0; i--)
            {
                if (state.Log.Events[i].Round == 0)
                {
                    state.Log.Events[i].Round = round;
                    state.Log.Events[i].Episode = episode;
                }
                else
                {
                    break;
                }
            }
        }

        for (state.Round = 1; state.Round <= config.MaxRounds; state.Round++)
        {
            // === Killer Phase ===
            var killerMoves = new List<(Killer actor, Point target, double score)>();
            foreach (var killer in state.Killers.Where(k => k.Alive))
            {
                var target = KillerPolicy.DecideNext(state, killer, exitsField);
                var sc = -Point.Chebyshev(target, killer.Pos);
                killerMoves.Add((killer, target, sc));
            }

            var killerDecided = MoveConflictResolver.ResolveNoOverlapSwap(state, killerMoves);

            // 套用 & 捕獲 #1
            foreach (var kv in killerDecided)
            {
                var killer = state.Killers.First(a => a.Id == kv.Key);
                killer.Pos = kv.Value;
            }

            foreach (var killer in state.Killers.Where(k => k.Alive))
            {
                var victims = state.Survivors.Where(sv => sv.Alive && sv.Pos.Equals(killer.Pos)).ToList();
                foreach (var victim in victims)
                {
                    victim.Alive = false;
                    state.KillerScore++;
                    state.Log.Capture(killer, victim, phase: "KillerPhase");
                    StampRoundOnNewEvents(state.Round);
                }
            }

            if (!state.Survivors.Any(sv => sv.Alive))
            {
                state.Log.RoundFooter(state);
                return ("Killer", state.SurvivorScore, state.KillerScore, state.Round, state.Log.Events.ToList());
            }

            // === Survivor Phase ===
            var survivorMoves = new List<(Survivor actor, Point target, double score)>();
            foreach (var survivor in state.Survivors.Where(sv => sv.Alive))
            {
                var target = SurvivorPolicy.DecideNext(state, survivor, exitsField);
                var sc = -Point.Chebyshev(target, survivor.Pos);
                survivorMoves.Add((survivor, target, sc));
            }

            var survivorDecided = MoveConflictResolver.ResolveNoOverlapSwap(state, survivorMoves);

            // 移動
            foreach (var kv in survivorDecided)
            {
                var survivor = state.Survivors.First(a => a.Id == kv.Key);
                survivor.Pos = kv.Value;
            }

            // 逃脫
            foreach (var survivor in state.Survivors.Where(sv => sv.Alive && state.IsExit(sv.Pos)).ToList())
            {
                survivor.Alive = false;
                state.SurvivorScore++;
                state.Log.Escape(survivor);
                StampRoundOnNewEvents(state.Round);
            }

            // 被捕 #2（生踩鬼）
            foreach (var survivor in state.Survivors.Where(sv => sv.Alive).ToList())
            {
                if (state.Killers.Any(k => k.Alive && k.Pos.Equals(survivor.Pos)))
                {
                    survivor.Alive = false;
                    state.KillerScore++;
                    var killer = state.Killers.First(k => k.Alive && k.Pos.Equals(survivor.Pos));
                    state.Log.Capture(killer, survivor, phase: "SurvivorPhase");
                    StampRoundOnNewEvents(state.Round);
                }
            }

            state.Log.RoundFooter(state);

            if (!state.Survivors.Any(sv => sv.Alive))
            {
                var winner =
                    (state.SurvivorScore >= state.Config.SurvivorCount)
                        ? "Survivor" // 全員逃脫
                        : "Killer"; // 被抓光或其它情形

                return (winner, state.SurvivorScore, state.KillerScore, state.Round, state.Log.Events.ToList());
            }
        }

        // 第 100 回合：規則=鬼勝
        return ("Killer", state.SurvivorScore, state.KillerScore, config.MaxRounds, state.Log.Events.ToList());
    }

    /// <summary>
    /// 跑多場並把結果寫入 ./TestResult
    /// </summary>
    public static void RunAndSaveBatch(GameConfig baseConfig, int episodes, string outputDir = "TestResult")
    {
        if (!Directory.Exists(outputDir))
        {
            Directory.CreateDirectory(outputDir);
        }

        var csvPath = Path.Combine(outputDir, "summary.csv");
        using var sw = new StreamWriter(csvPath, false);
        sw.WriteLine("episode,winner,survivorScore,killerScore,rounds");

        int killerWins = 0;
        int sumS = 0;
        int sumK = 0;
        int sumRounds = 0;

        for (int i = 0; i < episodes; i++)
        {
            var config = new GameConfig
            {
                Width = baseConfig.Width,
                Height = baseConfig.Height,
                MaxRounds = baseConfig.MaxRounds,
                SurvivorCount = baseConfig.SurvivorCount,
                KillerCount = baseConfig.KillerCount,
                SurvivorSight = baseConfig.SurvivorSight,
                KillerSight = baseConfig.KillerSight,
                ExitCount = baseConfig.ExitCount,
                Seed = baseConfig.Seed + i,
                VerboseLog = false,
                PrintAsciiMapEachRound = false
            };

            var (winner, sScore, kScore, rounds, events) = RunOneWithEvents(config, i);

            // 逐回合事件 NDJSON
            var ndjson = Path.Combine(outputDir, $"episode_{i:D4}.ndjson");
            using (var ew = new StreamWriter(ndjson, false))
            {
                for (int j = 0; j < events.Count; j++)
                {
                    ew.WriteLine(events[j].ToString());
                }
            }

            // 摘要 CSV
            sw.WriteLine($"{i},{winner},{sScore},{kScore},{rounds}");

            if (winner == "Killer")
            {
                killerWins++;
            }

            sumS += sScore;
            sumK += kScore;
            sumRounds += rounds;
        }

        double killerWinRate = (double)killerWins / episodes;
        double avgS = (double)sumS / episodes;
        double avgK = (double)sumK / episodes;
        double avgR = (double)sumRounds / episodes;

        // aggregate.json
        var aggregatePath = Path.Combine(outputDir, "aggregate.json");
        using (var aw = new StreamWriter(aggregatePath, false))
        {
            var whichHigher = killerWinRate > 0.5 ? "Killer" : (killerWinRate < 0.5 ? "Survivor" : "Tie");
            aw.WriteLine("{");
            aw.WriteLine($"  \"episodes\": {episodes},");
            aw.WriteLine($"  \"killer_win_rate\": {killerWinRate.ToString("0.000", CultureInfo.InvariantCulture)},");
            aw.WriteLine($"  \"avg_survivor_points\": {avgS.ToString("0.000", CultureInfo.InvariantCulture)},");
            aw.WriteLine($"  \"avg_killer_points\": {avgK.ToString("0.000", CultureInfo.InvariantCulture)},");
            aw.WriteLine($"  \"avg_rounds\": {avgR.ToString("0.0", CultureInfo.InvariantCulture)},");
            aw.WriteLine($"  \"which_side_higher\": \"{whichHigher}\"");
            aw.WriteLine("}");
        }
    }

    /// <summary>
    /// 多執行緒批次模擬：每個 episode 各自計算與輸出事件檔，最後彙整 summary 與 aggregate。
    /// </summary>
    public static void RunAndSaveBatchParallel(
        GameConfig baseConfig,
        int episodes,
        string outputDir = "TestResult",
        int? maxDegreeOfParallelism = null)
    {
        if (!Directory.Exists(outputDir))
        {
            Directory.CreateDirectory(outputDir);
        }

        var episodeResults = new EpisodeResult[episodes];

        int degree = maxDegreeOfParallelism ?? Environment.ProcessorCount;

        Parallel.For(0, episodes, new ParallelOptions { MaxDegreeOfParallelism = degree }, episode =>
        {
            // 為每個 episode 建立獨立的 GameConfig（避免共享狀態）
            var config = new GameConfig
            {
                Width = baseConfig.Width,
                Height = baseConfig.Height,
                MaxRounds = baseConfig.MaxRounds,
                SurvivorCount = baseConfig.SurvivorCount,
                KillerCount = baseConfig.KillerCount,
                SurvivorSight = baseConfig.SurvivorSight,
                KillerSight = baseConfig.KillerSight,
                ExitCount = baseConfig.ExitCount,
                Seed = baseConfig.Seed + episode, // 固定映射：可重現
                VerboseLog = false,
                PrintAsciiMapEachRound = false
            };

            // 執行單場並立刻輸出該場事件（避免大量記憶體累積）
            var (winner, sScore, kScore, rounds, events) = SimulatorExtensions.RunOneWithEvents(config, episode);

            string ndjsonPath = Path.Combine(outputDir, $"episode_{episode:D4}.ndjson");
            using (var ew = new StreamWriter(ndjsonPath, false))
            {
                for (int i = 0; i < events.Count; i++)
                {
                    ew.WriteLine(events[i].ToString());
                }
            }

            // 暫存本場摘要（陣列元素互不衝突，無需 lock）
            episodeResults[episode] = new EpisodeResult
            {
                Episode = episode,
                Winner = winner,
                SurvivorScore = sScore,
                KillerScore = kScore,
                Rounds = rounds
            };
        });

        // === 彙整輸出（單執行緒、避免併寫） ===
        string csvPath = Path.Combine(outputDir, "summary.csv");
        using (var sw = new StreamWriter(csvPath, false))
        {
            sw.WriteLine("episode,winner,survivorScore,killerScore,rounds");
            for (int i = 0; i < episodeResults.Length; i++)
            {
                var r = episodeResults[i];
                sw.WriteLine($"{r.Episode},{r.Winner},{r.SurvivorScore},{r.KillerScore},{r.Rounds}");
            }
        }

        int killerWins = 0;
        int sumS = 0;
        int sumK = 0;
        int sumRounds = 0;

        for (int i = 0; i < episodeResults.Length; i++)
        {
            var r = episodeResults[i];
            if (r.Winner == "Killer")
            {
                killerWins++;
            }

            sumS += r.SurvivorScore;
            sumK += r.KillerScore;
            sumRounds += r.Rounds;
        }

        double killerWinRate = (double)killerWins / episodes;
        double avgS = (double)sumS / episodes;
        double avgK = (double)sumK / episodes;
        double avgR = (double)sumRounds / episodes;
        string whichHigher = killerWinRate > 0.5 ? "Killer" : (killerWinRate < 0.5 ? "Survivor" : "Tie");

        string aggregatePath = Path.Combine(outputDir, "aggregate.json");
        using (var aw = new StreamWriter(aggregatePath, false))
        {
            aw.WriteLine("{");
            aw.WriteLine($"  \"episodes\": {episodes},");
            aw.WriteLine($"  \"killer_win_rate\": {killerWinRate.ToString("0.000", CultureInfo.InvariantCulture)},");
            aw.WriteLine($"  \"avg_survivor_points\": {avgS.ToString("0.000", CultureInfo.InvariantCulture)},");
            aw.WriteLine($"  \"avg_killer_points\": {avgK.ToString("0.000", CultureInfo.InvariantCulture)},");
            aw.WriteLine($"  \"avg_rounds\": {avgR.ToString("0.0", CultureInfo.InvariantCulture)},");
            aw.WriteLine($"  \"which_side_higher\": \"{whichHigher}\"");
            aw.WriteLine("}");
        }
    }
}