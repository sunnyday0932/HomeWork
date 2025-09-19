using SurvivorGame.Configs;
using SurvivorGame.Tests.Helper;

namespace SurvivorGame.Tests;

/// <summary>
/// 視野規則（10~13）
/// 10) 生存者視野 5×5（Chebyshev ≤ 2 可見）
/// 11) 鬼視野 7×7（Chebyshev ≤ 3 可見）
/// 12) 看不到任何鬼時，BuildVisibleKillerField 應為超大安全值
/// 13) Last-seen 與 timeout：鬼在看不見生存者超過 timeout 後，改以出口為目標
/// </summary>
public class VisionTest
{
    // 限定地圖範圍
    private const int Nine = 9;

    [Theory(DisplayName = "SurvivorSight 5×5：距離 2 可見、距離 3 不可見")]
    [InlineData(2, true)]
    [InlineData(3, false)]
    public void SurvivorSight_5x5_Chebyshev2Visible3NotVisible(int distance, bool expectedVisible)
    {
        // Arrange
        var config = new GameConfig
        {
            Width = Nine, Height = Nine,
            SurvivorCount = 0, KillerCount = 0,
            SurvivorSight = 2, // 5x5
            KillerSight = 3, // 7x7（此測試無關）
            Seed = 1
        };

        var survivorPos = new Point(4, 4);
        var killerPos = new Point(4 + distance, 4);
        var exits = new[] { new Point(0, 0) };

        var gameState = TestBoardBuilder.BuildFixed(
            config,
            exits,
            survivors: new[] { survivorPos },
            killers: new[] { killerPos }
        );

        // Act
        var survivor = gameState.Survivors.Single();
        var visible = Vision.VisibleKillers(gameState, survivor).Any();

        // Assert
        Assert.Equal(expectedVisible, visible);
    }

    [Theory(DisplayName = "KillerSight 7×7：距離 3 可見、距離 4 不可見")]
    [InlineData(3, true)]
    [InlineData(4, false)]
    public void KillerSight_7x7_Chebyshev3Visible4NotVisible(int distance, bool expectedVisible)
    {
        // Arrange
        var config = new GameConfig
        {
            Width = Nine, Height = Nine,
            SurvivorCount = 0, KillerCount = 0,
            SurvivorSight = 2,
            KillerSight = 3, // 7x7
            Seed = 2
        };

        var killerPos = new Point(4, 4);
        var survivorPos = new Point(4 + distance, 4);
        var exits = new[] { new Point(0, 0) };

        var gameState = TestBoardBuilder.BuildFixed(
            config,
            exits,
            survivors: new[] { survivorPos },
            killers: new[] { killerPos }
        );

        // Act
        var killer = gameState.Killers.Single();
        var visible = Vision.VisibleSurvivors(gameState, killer).Any();

        // Assert
        Assert.Equal(expectedVisible, visible);
    }

    [Fact(DisplayName = "看不到任何鬼時，D_killer 場為超大安全值（近似無限）")]
    public void KillerField_IsVeryLarge_WhenNoKillerVisible()
    {
        // Arrange
        var config = new GameConfig
        {
            Width = Nine, Height = Nine,
            SurvivorCount = 0, KillerCount = 0,
            SurvivorSight = 2, // 5x5 視野小，讓鬼超出視野
            KillerSight = 3,
            Seed = 3
        };

        var survivorPos = new Point(4, 4);
        var killerPos = new Point(0, 8); // 與生存者距離 Chebyshev=4+ => 超過視野
        var exits = new[] { new Point(8, 8) };

        var gameState = TestBoardBuilder.BuildFixed(
            config,
            exits,
            survivors: new[] { survivorPos },
            killers: new[] { killerPos }
        );

        var survivor = gameState.Survivors.Single();

        // Act
        var dKiller = DistanceFieldBuilder.BuildVisibleKillerField(gameState, survivor);

        // Assert：任一格應該都是很大的值（我們在實作中用 1_000_000）
        // 為了降低耦合，這裡只檢查「足夠大」門檻，例如 > 100_000。
        for (int x = 0; x < config.Width; x++)
        {
            for (int y = 0; y < config.Height; y++)
            {
                Assert.True(dKiller[x, y] > 100_000,
                    $"Expected very large safe distance at ({x},{y}), got {dKiller[x, y]}");
            }
        }
    }

    [Fact(DisplayName = "Killer Last-seen 與 Timeout：超時後改以出口為目標移動")]
    public void KillerUsesExitAfterLastSeenTimeout()
    {
        // Arrange：設計幾何讓方向可明顯區分
        // 地圖中心 (4,4) 放鬼；生存者「北方」(4,1) 先讓鬼看到一次；
        // 出口「東方」(8,4)。timeout 前鬼應往北走；timeout 後應往東走。
        var config = new GameConfig
        {
            Width = Nine, Height = Nine,
            SurvivorCount = 0, KillerCount = 0,
            SurvivorSight = 2,
            KillerSight = 3,
            LastSeenTimeout = 2, // 短一點，方便測
            Seed = 4
        };

        var exitPoint = new Point(8, 4); // 東方出口
        var killerStart = new Point(4, 4); // 中心
        var seenSurvivorPos = new Point(4, 1); // 北方（可見距離 3）
        var outOfSightPos = new Point(0, 0); // 之後移出視野

        var gameState = TestBoardBuilder.BuildFixed(
            config,
            exits: new[] { exitPoint },
            survivors: new[] { seenSurvivorPos },
            killers: new[] { killerStart }
        );
        var killer = gameState.Killers.Single();

        // 先讓鬼看到一次（Round 1）
        var exitField = StepRunner.BuildExitField(gameState);
        var firstStepTowardSeen = KillerPolicy.DecideNext(gameState, killer, exitField);
        // 應朝北（y-1）或（x不變,y-1 或 斜向北）以縮短到 (4,1)
        Assert.True(firstStepTowardSeen.Y <= killer.Pos.Y,
            $"Expected first move to reduce Y toward north; got {firstStepTowardSeen}");

        // 套用這一步，並將生存者移到視野外
        killer.Pos = firstStepTowardSeen;
        gameState.Survivors.Single().Pos = outOfSightPos;

        // 模擬 timeout：推進回合數超過 LastSeenTimeout
        gameState.Round += config.LastSeenTimeout + 1;

        // 再決策一次：現在應該以出口為目標，朝東移動（x+1 方向）
        var stepAfterTimeout = KillerPolicy.DecideNext(gameState, killer, exitField);

        Assert.True(stepAfterTimeout.X >= killer.Pos.X,
            $"Expected step after timeout to move east (toward exit at {exitPoint}), got {stepAfterTimeout} from {killer.Pos}");
    }
}