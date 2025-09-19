using SurvivorGame.Configs;
using SurvivorGame.Tests.Helper;

namespace SurvivorGame.Tests;

/// <summary>
/// 距離場與 BFS（14–15）
/// 14) 出口距離場正確性（Chebyshev + 多源）
/// 15) 可視鬼距離場（僅以「可見鬼」為多源；不可見鬼不影響）
/// </summary>
public class FieldBfsTest
{
    // 限制地圖範圍
    private const int Five = 5;

    [Fact(DisplayName = "D14. 出口距離場 = 對所有出口 Chebyshev 距離的最小值（多源 BFS）")]
    public void ExitDistanceField_EqualsMinChebyshevToAllExits()
    {
        // Arrange：5x5，小地圖兩個出口（對角）
        var config = new GameConfig
        {
            Width = Five, Height = Five,
            SurvivorCount = 0, KillerCount = 0,
            ExitCount = 0, // 我們用固定佈局自行放置
            Seed = 1
        };

        var exitA = new Point(0, 0);
        var exitB = new Point(4, 4);
        var gameState = TestBoardBuilder.BuildFixed(
            config,
            exits: new[] { exitA, exitB },
            survivors: Array.Empty<Point>(),
            killers: Array.Empty<Point>());

        // Act
        var exitField = DistanceFieldBuilder.BuildExitField(gameState);

        // Assert：每格應為 min( Chebyshev(·,exitA), Chebyshev(·,exitB) )
        for (int x = 0; x < config.Width; x++)
        {
            for (int y = 0; y < config.Height; y++)
            {
                var point = new Point(x, y);
                var expected = Math.Min(Point.Chebyshev(point, exitA), Point.Chebyshev(point, exitB));
                Assert.Equal(expected, exitField[x, y]);
            }
        }
    }

    [Fact(DisplayName = "D15a. 可視鬼距離場：只有可見鬼為多源；不可見鬼不影響")]
    public void VisibleKillerField_UsesOnlyVisibleKillers()
    {
        // Arrange：生存者在 (1,1)
        // 可見鬼在 (3,1) => Chebyshev=2（可見）
        // 不可見鬼在 (4,4) => Chebyshev=3（不可見，且在 5x5 界內）
        var config = new GameConfig
        {
            Width = Five, Height = Five,
            SurvivorCount = 0, KillerCount = 0,
            SurvivorSight = 2, // 5x5 視野
            KillerSight = 3,
            Seed = 2
        };

        var survivorPos = new Point(1, 1);
        var visibleKillerPos = new Point(3, 1); // 可見
        var invisibleKillerPos = new Point(4, 4); // 不可見
        var exitPoint = new Point(0, 0);

        var gameState = TestBoardBuilder.BuildFixed(
            config,
            exits: new[] { exitPoint },
            survivors: new[] { survivorPos },
            killers: new[] { visibleKillerPos, invisibleKillerPos });

        var survivor = gameState.Survivors.Single();

        // Act
        var killerField = DistanceFieldBuilder.BuildVisibleKillerField(gameState, survivor);

        // Assert：距離場應等於「到可見鬼」的 Chebyshev 距離（不可見鬼不影響）
        for (int x = 0; x < config.Width; x++)
        {
            for (int y = 0; y < config.Height; y++)
            {
                var point = new Point(x, y);
                var expected = Point.Chebyshev(point, visibleKillerPos);
                Assert.Equal(expected, killerField[x, y]);
            }
        }
    }

    [Fact(DisplayName = "D15b. 可視鬼距離場：多隻可見鬼時，取最小距離（多源）")]
    public void VisibleKillerField_TakesMinimumOverMultipleVisibleKillers()
    {
        // Arrange：生存者在中心 (2,2)，兩隻可見鬼在 (4,2) 與 (0,2)（皆距離=2）
        var config = new GameConfig
        {
            Width = Five, Height = Five,
            SurvivorCount = 0, KillerCount = 0,
            SurvivorSight = 2,
            KillerSight = 3,
            Seed = 3
        };

        var survivorPos = new Point(2, 2);
        var visibleKillerA = new Point(4, 2);
        var visibleKillerB = new Point(0, 2);
        var exitPoint = new Point(4, 4);

        var gameState = TestBoardBuilder.BuildFixed(
            config,
            exits: new[] { exitPoint },
            survivors: new[] { survivorPos },
            killers: new[] { visibleKillerA, visibleKillerB });

        var survivor = gameState.Survivors.Single();

        // Act
        var killerField = DistanceFieldBuilder.BuildVisibleKillerField(gameState, survivor);

        // Assert：每格 = min(距離到 A, 距離到 B)
        for (int x = 0; x < config.Width; x++)
        {
            for (int y = 0; y < config.Height; y++)
            {
                var point = new Point(x, y);
                var expected = Math.Min(Point.Chebyshev(point, visibleKillerA), Point.Chebyshev(point, visibleKillerB));
                Assert.Equal(expected, killerField[x, y]);
            }
        }
    }
}