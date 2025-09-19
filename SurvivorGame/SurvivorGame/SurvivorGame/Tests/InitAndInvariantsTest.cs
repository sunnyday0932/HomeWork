using SurvivorGame.Configs;
using SurvivorGame.Tests.Helper;

namespace SurvivorGame.Tests;

public class InitAndInvariantsTest
{
    [Fact(DisplayName = "初始化後：所有出口與角色皆在地圖邊界內")]
    public void AllEntitiesAreWithinBounds_AfterInitialization()
    {
        // Arrange
        var gameConfig = new GameConfig
        {
            Width = 50,
            Height = 50,
            SurvivorCount = 3,
            KillerCount = 3,
            ExitCount = 2,
            Seed = 1234
        };

        // Act
        var gameState = TestHelpers.BuildInitializedState(gameConfig);

        // Assert
        foreach (var point in gameState.Exits)
        {
            Assert.True(TestHelpers.InBounds(gameState, point), $"Exit {point} out of bounds");
        }


        foreach (var survivor in gameState.Survivors)
        {
            Assert.True(TestHelpers.InBounds(gameState, survivor.Pos), $"Survivor#{survivor.Id} at {survivor.Pos} out of bounds");
        }


        foreach (var killer in gameState.Killers)
        {
            Assert.True(TestHelpers.InBounds(gameState, killer.Pos), $"Killer#{killer.Id} at {killer.Pos} out of bounds");
        }
    }

    [Theory(DisplayName = "出口數量正確且不重疊")]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public void ExitCountIsCorrect_AndUnique(int exitCount)
    {
        // Arrange
        var config = new GameConfig
        {
            Width = 30, Height = 30,
            SurvivorCount = 2, KillerCount = 2,
            ExitCount = exitCount,
            Seed = 4321
        };

        // Act
        var gameState = TestHelpers.BuildInitializedState(config);

        // Assert: 數量
        Assert.Equal(exitCount, gameState.Exits.Count);

        // 位置唯一
        var unique = gameState.Exits.Distinct().Count();
        Assert.Equal(gameState.Exits.Count, unique);

        // 都是 Exit 標記
        foreach (var exitPoint in gameState.Exits)
        {
            Assert.True(gameState.IsExit(exitPoint), $"Exit list contains non-exit at {exitPoint}");
        }
    }

    [Fact(DisplayName = "出生距離約束：與出口/敵對/同隊的最小距離皆滿足")]
    public void SpawnRespectsMinDistances()
    {
        // Arrange
            var config = new GameConfig
            {
                Width = 40, Height = 40,
                SurvivorCount = 3, KillerCount = 3,
                ExitCount = 3,
                MinDistToExit = 8,
                MinDistEnemy = 4,
                MinDistAlly = 2,
                Seed = 9876
            };

            // Act
            var gameState = TestHelpers.BuildInitializedState(config);

            // Assert: 生存者與出口距離
            foreach (var survivor in gameState.Survivors)
            {
                var minExitDistance = gameState.Exits.Min(exitPoint => TestHelpers.Chebyshev(exitPoint, survivor.Pos));
                Assert.True(minExitDistance >= config.MinDistToExit,
                    $"Survivor#{survivor.Id} too close to exit: {minExitDistance} < {config.MinDistToExit}");
            }

            // 鬼與出口距離
            foreach (var killer in gameState.Killers)
            {
                var minExitDistance = gameState.Exits.Min(exitPoint => TestHelpers.Chebyshev(exitPoint, killer.Pos));
                Assert.True(minExitDistance >= config.MinDistToExit,
                    $"Killer#{killer.Id} too close to exit: {minExitDistance} < {config.MinDistToExit}");
            }

            // 生-鬼 最小距離
            foreach (var survivor in gameState.Survivors)
            {
                var minEnemyDistance = gameState.Killers.Min(killer => TestHelpers.Chebyshev(killer.Pos, survivor.Pos));
                Assert.True(minEnemyDistance >= config.MinDistEnemy,
                    $"S#{survivor.Id} too close to any killer: {minEnemyDistance} < {config.MinDistEnemy}");
            }

            // 生-生 同隊最小距離
            for (int i = 0; i < gameState.Survivors.Count; i++)
            for (int j = i + 1; j < gameState.Survivors.Count; j++)
            {
                var distance = TestHelpers.Chebyshev(gameState.Survivors[i].Pos, gameState.Survivors[j].Pos);
                Assert.True(distance >= config.MinDistAlly,
                    $"Survivors too close: d={distance} < {config.MinDistAlly}");
            }

            // 鬼-鬼 同隊最小距離
            for (int i = 0; i < gameState.Killers.Count; i++)
            for (int j = i + 1; j < gameState.Killers.Count; j++)
            {
                var distance = TestHelpers.Chebyshev(gameState.Killers[i].Pos, gameState.Killers[j].Pos);
                Assert.True(distance >= config.MinDistAlly,
                    $"Killers too close: d={distance} < {config.MinDistAlly}");
            }
    }

    [Fact(DisplayName = "初始化後所有角色互不重疊（不可同格）")]
    public void NoOverlappingActors_OnInitialization()
    {
        // Arrange
        var config = new GameConfig
        {
            Width = 50, Height = 50,
            SurvivorCount = 3, KillerCount = 3,
            ExitCount = 2,
            Seed = 2468
        };

        // Act
        var gameState = TestHelpers.BuildInitializedState(config);

        // Assert
        var positions = TestHelpers.GetAllActorPositions(gameState).Select(x => x.pos).ToList();
        var distinct = positions.Distinct().Count();
        Assert.Equal(positions.Count, distinct);
    }

    [Fact(DisplayName = "相同 Seed 可重現：兩次初始化的佈局一致")]
    public void SameSeed_ProducesIdenticalInitialLayout()
    {
        // Arrange
        var config = new GameConfig
        {
            Width = 40, Height = 40,
            SurvivorCount = 2, KillerCount = 2,
            ExitCount = 2,
            Seed = 13579
        };

        // Act
        var gameState1 = TestHelpers.BuildInitializedState(config);
        var gameState2 = TestHelpers.BuildInitializedState(config);

        // Assert：出口順序與座標相同
        Assert.Equal(gameState1.Exits.Count, gameState2.Exits.Count);
        for (int i = 0; i < gameState1.Exits.Count; i++)
        {
            Assert.Equal(gameState1.Exits[i], gameState2.Exits[i]);
        }


        // 角色位置（依 Id）相同
        Assert.Equal(gameState1.Survivors.Count, gameState2.Survivors.Count);
        for (int i = 0; i < gameState1.Survivors.Count; i++)
        {
            Assert.Equal(gameState1.Survivors[i].Pos, gameState2.Survivors[i].Pos);
        }

        Assert.Equal(gameState1.Killers.Count, gameState2.Killers.Count);
        for (int i = 0; i < gameState1.Killers.Count; i++)
        {
            Assert.Equal(gameState1.Killers[i].Pos, gameState2.Killers[i].Pos);
        }

    }
}