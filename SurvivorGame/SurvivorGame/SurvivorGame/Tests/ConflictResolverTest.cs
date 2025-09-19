using SurvivorGame.Actors;
using SurvivorGame.Configs;
using SurvivorGame.Tests.Helper;

namespace SurvivorGame.Tests;

/// <summary>
/// 衝突解算（25–28）
/// 25) 同格競爭：高分者得
/// 26) 同分 tie-break by Id
/// 27) 禁止對向交換（swap）
/// 28) 跟隨鏈（目前程式尚未支援一般循環；附上 Skip 的期望測試 + 目前行為測試）
/// </summary>
public class ConflictResolverTest
{
    [Fact(DisplayName = "同格競爭：高分者佔位，其他人原地")]
    public void HigherScoreWins_OnSameTargetCell()
    {
        // 小盤面，兩名生存者搶同一格
        var config = new GameConfig
        {
            Width = 7, Height = 7,
            SurvivorCount = 0, KillerCount = 0,
            Seed = 250
        };

        var exitPoints = Array.Empty<Point>();
        var survivors = new[] { new Point(3, 3), new Point(5, 3) };
        var killers = Array.Empty<Point>();

        var gameState = TestBoardBuilder.BuildFixed(config, exitPoints, survivors, killers);
        var survivorA = gameState.Survivors[0]; // Id=0
        var survivorB = gameState.Survivors[1]; // Id=1

        var targetCell = new Point(4, 3); // 兩人都想進來
        var submissions = new List<(Survivor actor, Point target, double score)>
        {
            (survivorA, targetCell, score: 0.8),
            (survivorB, targetCell, score: 0.2)
        };

        var decided = MoveConflictResolver.ResolveNoOverlapSwap(gameState, submissions);

        Assert.Equal(targetCell, decided[survivorA.Id]); // 高分者得
        Assert.Equal(survivorB.Pos, decided[survivorB.Id]); // 低分者原地
    }

    [Fact(DisplayName = "同分 tie-break：Id 較小者佔位")]
    public void TieBreakById_WhenScoresEqual_OnSameTargetCell()
    {
        var config = new GameConfig
        {
            Width = 7, Height = 7,
            SurvivorCount = 0, KillerCount = 0,
            Seed = 260
        };

        var gameState = TestBoardBuilder.BuildFixed(
            config,
            exits: Array.Empty<Point>(),
            survivors: new[] { new Point(3, 3), new Point(5, 3) },
            killers: Array.Empty<Point>());

        var survivorA = gameState.Survivors[0]; // Id=0
        var survivorB = gameState.Survivors[1]; // Id=1

        var targetCell = new Point(4, 3);
        var submissions = new List<(Survivor actor, Point target, double score)>
        {
            (survivorA, targetCell, score: 1.0),
            (survivorB, targetCell, score: 1.0) // 同分
        };

        var decided = MoveConflictResolver.ResolveNoOverlapSwap(gameState, submissions);

        Assert.Equal(targetCell, decided[survivorA.Id]); // Id 小者贏
        Assert.Equal(survivorB.Pos, decided[survivorB.Id]); // 另一位原地
    }

    [Fact(DisplayName = " 禁止對向交換：兩人互換彼此格子 ⇒ 皆原地")]
    public void NoSwap_BothStay_WhenOppositeExchange()
    {
        var config = new GameConfig
        {
            Width = 7, Height = 7,
            SurvivorCount = 0, KillerCount = 0,
            Seed = 270
        };

        var gameState = TestBoardBuilder.BuildFixed(
            config,
            exits: Array.Empty<Point>(),
            survivors: new[] { new Point(3, 3), new Point(4, 3) },
            killers: Array.Empty<Point>());

        var a = gameState.Survivors[0];
        var b = gameState.Survivors[1];

        var submissions = new List<(Survivor actor, Point target, double score)>
        {
            (a, b.Pos, score: 1.0),
            (b, a.Pos, score: 1.0)
        };

        var decided = MoveConflictResolver.ResolveNoOverlapSwap(gameState, submissions);

        Assert.Equal(a.Pos, decided[a.Id]); // 原地
        Assert.Equal(b.Pos, decided[b.Id]); // 原地
    }

    // === 28. 跟隨鏈（期望 vs 目前行為） ===

    [Fact(DisplayName = "(expectation). 三人循環跟隨應阻擋（目前程式尚未支援）",
        Skip = "MoveConflictResolver 尚未實作一般循環（>2）偵測/回退；此測試為未來功能預留")]
    public void FollowChain_ShouldBePrevented_WhenThreeCycle()
    {
        // 期望行為（尚未實作）：
        // A: (3,3) -> B.pos
        // B: (4,3) -> C.pos
        // C: (5,3) -> A.pos
        // 三人形成 3-cycle，應回退以避免循環（例如讓後序者原地）。
        var config = new GameConfig { Width = 9, Height = 9, SurvivorCount = 0, KillerCount = 0, Seed = 280 };
        var gameState = TestBoardBuilder.BuildFixed(
            config,
            exits: Array.Empty<Point>(),
            survivors: new[] { new Point(3, 3), new Point(4, 3), new Point(5, 3) },
            killers: Array.Empty<Point>());

        var a = gameState.Survivors[0];
        var b = gameState.Survivors[1];
        var c = gameState.Survivors[2];

        var submissions = new List<(Survivor actor, Point target, double score)>
        {
            (a, b.Pos, 1.0),
            (b, c.Pos, 1.0),
            (c, a.Pos, 1.0)
        };

        var decided = MoveConflictResolver.ResolveNoOverlapSwap(gameState, submissions);

        // 期望（尚未實作）：至少一人被回退，避免三人互換結束狀態
        Assert.NotEqual(b.Pos, decided[a.Id]);
        Assert.NotEqual(c.Pos, decided[b.Id]);
        Assert.NotEqual(a.Pos, decided[c.Id]);
    }

    [Fact(DisplayName = "(current). 目前行為：三人循環跟隨會被允許（未阻擋）")]
    public void FollowChain_CurrentlyAllowed_ThreeCycle()
    {
        // 目前版本只阻擋「兩人對向交換」，不處理一般循環。
        var config = new GameConfig { Width = 9, Height = 9, SurvivorCount = 0, KillerCount = 0, Seed = 281 };
        var gameState = TestBoardBuilder.BuildFixed(
            config,
            exits: Array.Empty<Point>(),
            survivors: new[] { new Point(3, 3), new Point(4, 3), new Point(5, 3) },
            killers: Array.Empty<Point>());

        var a = gameState.Survivors[0];
        var b = gameState.Survivors[1];
        var c = gameState.Survivors[2];

        var submissions = new List<(Survivor actor, Point target, double score)>
        {
            (a, b.Pos, 1.0),
            (b, c.Pos, 1.0),
            (c, a.Pos, 1.0)
        };

        var decided = MoveConflictResolver.ResolveNoOverlapSwap(gameState, submissions);

        // 驗證目前的實際行為（允許 3-cycle）：每個人都移動到下家的起點
        Assert.Equal(b.Pos, decided[a.Id]);
        Assert.Equal(c.Pos, decided[b.Id]);
        Assert.Equal(a.Pos, decided[c.Id]);
    }
}