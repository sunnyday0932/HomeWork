using SurvivorGame.Configs;
using SurvivorGame.Tests.Helper;

namespace SurvivorGame.Tests;

/// <summary>
/// 生存者策略（Scoring & Moves, 16–20）
/// 16) 出口相鄰即選出口
/// 17) 近身強懲避免自殺靠近（D_killer ≤ 2 的候選格不被選）
/// 18) 一步預測避免貼臉（可見鬼下一步可能貼臉的格會被避開）
/// 19) 未知區微懲罰：沒有可視鬼的 5x5 區會被小幅扣分
/// 20) Last-seen 陰影懲罰：候選格落在 last-seen 擴散半徑內會被避開
/// </summary>
public class SurvivorPolicyTest
{
    private const int Nine = 9;
    private const int Eleven = 11;

    [Fact(DisplayName = "出口相鄰 ⇒ SurvivorPolicy 應優先選出口格")]
    public void AdjacentToExit_PicksExitImmediately()
    {
        // Arrange：生存者在 (3,3)，出口在 (4,4)，鬼遠離不影響
        var config = new GameConfig
        {
            Width = Nine, Height = Nine,
            SurvivorCount = 0, KillerCount = 0,
            SurvivorSight = 2, KillerSight = 3,
            Seed = 10
        };

        var exitPoint = new Point(4, 4);
        var survivorPos = new Point(3, 3);
        var killerPos = new Point(0, 0); // 遠處，不影響決策
        var gameState = TestBoardBuilder.BuildFixed(
            config,
            exits: new[] { exitPoint },
            survivors: new[] { survivorPos },
            killers: new[] { killerPos }
        );

        var survivor = gameState.Survivors.Single();
        var exitField = DistanceFieldBuilder.BuildExitField(gameState);

        // Act
        var next = SurvivorPolicy.DecideNext(gameState, survivor, exitField);

        // Assert：直接選出口
        Assert.Equal(exitPoint, next);
    }

    [Fact(DisplayName = "近身強懲：不會主動選擇 D_killer ≤ 2 的危險候選格（非出口）")]
    public void StrongClosePenalty_AvoidsDangerousCell()
    {
        // Arrange：生存者 (5,5)，可見鬼 (7,5) → 若往 (6,5) 則 D_killer=1（危險）
        var config = new GameConfig
        {
            Width = Eleven, Height = Eleven,
            SurvivorCount = 0, KillerCount = 0,
            SurvivorSight = 2, KillerSight = 3,
            Seed = 11
        };

        var exitPoint = new Point(10, 10); // 遠方出口
        var survivorPos = new Point(5, 5);
        var killerPos = new Point(7, 5); // 可見，距離=2
        var dangerousCandidate = new Point(6, 5); // 往這步會與鬼 Chebyshev=1

        var gameState = TestBoardBuilder.BuildFixed(
            config,
            exits: new[] { exitPoint },
            survivors: new[] { survivorPos },
            killers: new[] { killerPos }
        );

        var survivor = gameState.Survivors.Single();
        var exitField = DistanceFieldBuilder.BuildExitField(gameState);

        // Act
        var next = SurvivorPolicy.DecideNext(gameState, survivor, exitField);

        // Assert：不應選擇危險格（非出口）
        Assert.NotEqual(dangerousCandidate, next);
    }

    [Fact(DisplayName = "一步預測：會避開鬼下一步可能貼臉的位置")]
    public void OneStepLookahead_AvoidsPotentialAdjacentNextTurn()
    {
        // Arrange：生存者 (5,3)，可見鬼 (3,3)；若生存者走到 (4,3)，鬼下一步可到 (4,3) 貼臉
        var config = new GameConfig
        {
            Width = Nine, Height = Nine,
            SurvivorCount = 0, KillerCount = 0,
            SurvivorSight = 2, KillerSight = 3,
            Seed = 12
        };

        var exitPoint = new Point(8, 8); // 出口在遠方，不直接干擾該案例
        var survivorPos = new Point(5, 3);
        var killerPos = new Point(3, 3); // 可見（距離=2）
        var riskyStep = new Point(4, 3); // 介於兩者之間

        var gameState = TestBoardBuilder.BuildFixed(
            config,
            exits: new[] { exitPoint },
            survivors: new[] { survivorPos },
            killers: new[] { killerPos }
        );

        var survivor = gameState.Survivors.Single();
        var exitField = DistanceFieldBuilder.BuildExitField(gameState);

        // Act
        var next = SurvivorPolicy.DecideNext(gameState, survivor, exitField);

        // Assert：不應選擇 riskyStep（鬼下一步可能貼臉的位置）
        Assert.NotEqual(riskyStep, next);
    }

    [Fact(DisplayName = "未知區微懲罰：沒有可視鬼的 5x5 區域的候選格會被小幅扣分")]
    public void UnknownAreaPenalty_ShiftsChoiceTowardInformativeArea()
    {
        // 幾何：Survivor=(4,4)，Exit=(4,8)；Invisible Killer=(1,4)。
        // 候選：A=(3,5)、B=(5,5)：
        // - 兩者到 Exit 的 Chebyshev 距離皆為 3（對稱）。
        // - 對 Survivor 而言，Killer 距離=3 ⇒ 看不到（D_killer 對兩者等價且極大）。
        // - 以候選格為中心的 5x5 來看：
        //   * A=(3,5) 到 Killer=(1,4) 距離 max(|3-1|,|5-4|)=2 ⇒ 5x5 視窗內「有鬼」→ 無 unknown penalty
        //   * B=(5,5) 到 Killer=(1,4) 距離 max(4,1)=4 ⇒ 視窗外 → 會吃 unknown penalty
        var config = new GameConfig
        {
            Width = Nine, Height = Nine,
            SurvivorCount = 0, KillerCount = 0,
            SurvivorSight = 2, KillerSight = 3,
            UnknownAreaPenalty = 0.5,
            Omega_Margin = 0.0,
            Seed = 19
        };

        var exitPoint = new Point(4, 8);
        var survivorPos = new Point(4, 4);
        var invisibleKiller = new Point(1, 4);

        var gameState = TestBoardBuilder.BuildFixed(
            config,
            exits: new[] { exitPoint },
            survivors: new[] { survivorPos },
            killers: new[] { invisibleKiller }
        );

        var survivor = gameState.Survivors.Single();
        var exitField = DistanceFieldBuilder.BuildExitField(gameState);

        var candidateA = new Point(3, 5); // 應被選
        var candidateB = new Point(5, 5); // 應被排除（unknown penalty）

        var next = SurvivorPolicy.DecideNext(gameState, survivor, exitField);

        Assert.Equal(candidateA, next);
        Assert.NotEqual(candidateB, next);
    }

    [Fact(DisplayName = "Last-seen 陰影：候選格在陰影半徑內時會被避開")]
    public void LastSeenShadow_DiscouragesStepsInsideShadowRadius()
    {
        // 幾何：Survivor=(4,4)，Exit=(4,8)；此刻地圖上沒有可見鬼。
        // 設定 last-seen 在 (6,4)，Round=10、LastSeenRound=8 ⇒ 陰影半徑 R=2。
        // 候選：Outside=(3,5)、Inside=(5,5)
        // - 到 Exit 距離皆為 3（對稱）
        // - 到 last-seen=(6,4)：Outside 距離= max(3,1)=3 > R ⇒ 陰影外；Inside 距離= max(1,1)=1 ≤ R ⇒ 陰影內
        var config = new GameConfig
        {
            Width = Nine, Height = Nine,
            SurvivorCount = 0, KillerCount = 0,
            SurvivorSight = 2, KillerSight = 3,
            LastSeenShadowCap = 5,
            Omega_Margin = 0.0,
            Seed = 20
        };

        var exitPoint = new Point(4, 8);
        var survivorPos = new Point(4, 4);

        var gameState = TestBoardBuilder.BuildFixed(
            config,
            exits: new[] { exitPoint },
            survivors: new[] { survivorPos },
            killers: Array.Empty<Point>()
        );

        var survivor = gameState.Survivors.Single();

        gameState.Round = 10;
        survivor.LastSeenKillerPos = new Point(6, 4);
        survivor.LastSeenKillerRound = 8; // → R = 2

        var exitField = DistanceFieldBuilder.BuildExitField(gameState);

        var outsideShadowCandidate = new Point(3, 5); // 應被選
        var insideShadowCandidate = new Point(5, 5); // 應被排除

        var next = SurvivorPolicy.DecideNext(gameState, survivor, exitField);

        Assert.Equal(outsideShadowCandidate, next);
        Assert.NotEqual(insideShadowCandidate, next);
    }
}