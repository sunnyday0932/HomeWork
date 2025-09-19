using SurvivorGame.Configs;
using SurvivorGame.Tests.Helper;

namespace SurvivorGame.Tests;

/// <summary>
/// 鬼策略（21–24）
/// 21) 視野內選最近生存者
/// 22) 視野外時使用 last-seen 位置追擊
/// 23) 超過 timeout 後改巡邏出口（朝最近出口前進）
/// 24) spacing penalty 促使選擇不緊鄰其他鬼的候選步（在等距縮短目標時）
/// </summary>
public class KillerPolicyTest
{
    [Fact(DisplayName = "F21. 視野內有多名生存者時，鎖定最近者並朝其方向縮短距離")]
    public void VisibleTargets_SelectsNearestSurvivor()
    {
        // 幾何：Killer=(4,4)，兩名生存者在可見範圍內：
        // Snear=(6,4) 距離=2；Sfar=(8,8) 距離=4。預期鬼的下一步會縮短到 Snear 的距離。
        var config = new GameConfig
        {
            Width = 15, Height = 15,
            SurvivorCount = 0, KillerCount = 0,
            SurvivorSight = 2, KillerSight = 3, // 7x7 視野
            Seed = 210
        };

        var exitPoint = new Point(0, 0); // 無關
        var killerStart = new Point(4, 4);
        var sNear = new Point(6, 4); // dist=2
        var sFar = new Point(8, 8); // dist=4

        var gameState = TestBoardBuilder.BuildFixed(
            config,
            exits: new[] { exitPoint },
            survivors: new[] { sNear, sFar },
            killers: new[] { killerStart }
        );

        var killer = gameState.Killers.Single();
        var exitField = DistanceFieldBuilder.BuildExitField(gameState);

        var prevDistToNear = Point.Chebyshev(killer.Pos, sNear);
        var prevDistToFar = Point.Chebyshev(killer.Pos, sFar);

        var next = KillerPolicy.DecideNext(gameState, killer, exitField);

        var newDistToNear = Point.Chebyshev(next, sNear);
        var newDistToFar = Point.Chebyshev(next, sFar);

        // 斷言：對最近者距離必須縮短 1；不會為了遠者而放棄縮短最近者
        Assert.Equal(prevDistToNear - 1, newDistToNear);
        Assert.True(newDistToFar >= prevDistToFar - 1);
    }

    [Fact(DisplayName = "F22. 看不見時，使用 last-seen 位置作為追擊目標")]
    public void UsesLastSeenPosition_WhenSurvivorOutOfSight()
    {
        // 幾何：Killer=(4,4)，先在 Round t 看見生存者 at (6,4)；
        // 下一回合生存者移至視野外，但在 timeout 內 ⇒ 應朝 last-seen (6,4) 前進（x 增加）。
        var config = new GameConfig
        {
            Width = 15, Height = 15,
            SurvivorCount = 0, KillerCount = 0,
            SurvivorSight = 2, KillerSight = 3,
            LastSeenTimeout = 5,
            Seed = 220
        };

        var exitPoint = new Point(0, 0);
        var killerStart = new Point(4, 4);
        var survivorSeen = new Point(6, 4); // 可見
        var survivorOutOfFov = new Point(12, 12); // 與 killer 距離 > 3，移至視野外

        var gameState = TestBoardBuilder.BuildFixed(
            config,
            exits: new[] { exitPoint },
            survivors: new[] { survivorSeen },
            killers: new[] { killerStart }
        );

        var killer = gameState.Killers.Single();
        var survivor = gameState.Survivors.Single();
        var exitField = DistanceFieldBuilder.BuildExitField(gameState);

        // 第一次決策（看見）
        var first = KillerPolicy.DecideNext(gameState, killer, exitField);
        Assert.True(first.X >= killer.Pos.X, "When target is at (6,4), the first step should not move left.");

        // 套用一步並更新 last-seen（視為 Round t）
        killer.Pos = first;
        killer.LastSeenSurvivorPos = survivorSeen;
        killer.LastSeenSurvivorRound = gameState.Round;

        // 生存者移到視野外，回合+1（仍在 timeout 內）
        gameState.Round += 1;
        survivor.Pos = survivorOutOfFov;

        var second = KillerPolicy.DecideNext(gameState, killer, exitField);
        // 仍應朝 last-seen (6,4) 前進（東向或東北/東南）
        Assert.True(second.X >= killer.Pos.X, "Should keep moving toward last-seen (increase X toward 6).");
    }

    [Fact(DisplayName = "F23. 超過 last-seen timeout 後，改朝最近出口前進")]
    public void PatrolsExit_AfterLastSeenTimeout()
    {
        // 幾何：Killer=(4,4)，最近出口在 (10,4)（東方）。超過 timeout 後應朝出口方向移動（x 增加）。
        var config = new GameConfig
        {
            Width = 15, Height = 15,
            SurvivorCount = 0, KillerCount = 0,
            SurvivorSight = 2, KillerSight = 3,
            LastSeenTimeout = 2,
            Seed = 230
        };

        var exitPoint = new Point(10, 4);
        var killerStart = new Point(4, 4);

        var gameState = TestBoardBuilder.BuildFixed(
            config,
            exits: new[] { exitPoint },
            survivors: Array.Empty<Point>(),
            killers: new[] { killerStart }
        );

        var killer = gameState.Killers.Single();
        var exitField = DistanceFieldBuilder.BuildExitField(gameState);

        // 模擬：曾看過生存者，但已經超時
        killer.LastSeenSurvivorPos = new Point(6, 6);
        killer.LastSeenSurvivorRound = 1;
        gameState.Round = killer.LastSeenSurvivorRound + config.LastSeenTimeout + 1;

        var step = KillerPolicy.DecideNext(gameState, killer, exitField);

        // 應朝最近出口 (10,4) 前進：x 不應減少，且到出口距離應縮小
        Assert.True(step.X >= killer.Pos.X, "Should move east toward the nearest exit.");
        var prev = Point.Chebyshev(killer.Pos, exitPoint);
        var now = Point.Chebyshev(step, exitPoint);
        Assert.Equal(prev - 1, now);
    }

    [Fact(DisplayName = "F24. Spacing penalty：等距縮短目標時，偏好不緊鄰其他鬼的候選步")]
    public void SpacingPenalty_PrefersNonAdjacentCandidate_WhenDistanceGainIsEqual()
    {
        // 幾何：
        // - 目標生存者 T=(5,3)
        // - Killer A at (2,2) 有兩個「等價縮短」候選：
        //     A1=(3,2) 與 A2=(3,3) 到 T 距離同為 2（皆從 3 縮到 2）
        // - 另一隻 Killer B at (2,4)
        //   * A1=(3,2) 與 B 距離 = max(|3-2|,|2-4|)=2（不觸發 spacing penalty）
        //   * A2=(3,3) 與 B 距離 = max(1,1)=1（觸發 spacing penalty）
        // 預期：Killer A 應選 A1=(3,2) 而不是 A2=(3,3)。
        var config = new GameConfig
        {
            Width = 9, Height = 9,
            SurvivorCount = 0, KillerCount = 0,
            SurvivorSight = 2, KillerSight = 3,
            KillerSpacingPenalty = 1.0, // 放大影響，確保傾向明顯
            KillerInterceptBonus = 0.0, // 關掉額外干擾
            Seed = 240
        };

        var exitPoint = new Point(0, 0); // 無關
        var target    = new Point(5, 4); //  選 (5,4) 為了排除 (3,1) 的同分候選
        var killerA   = new Point(2, 2);
        var killerB   = new Point(2, 4);

        var gameState = TestBoardBuilder.BuildFixed(
            config,
            exits:     new[] { exitPoint },
            survivors: new[] { target },
            killers:   new[] { killerA, killerB }
        );

        var killerARef = gameState.Killers.First(k => k.Id == 0);
        var exitField  = DistanceFieldBuilder.BuildExitField(gameState);

        var next = KillerPolicy.DecideNext(gameState, killerARef, exitField);

        Assert.Equal(new Point(3, 2), next);    // 偏好不緊鄰 B 的候選
        Assert.NotEqual(new Point(3, 3), next); // 會觸發 spacing 懲罰的候選
    }
}