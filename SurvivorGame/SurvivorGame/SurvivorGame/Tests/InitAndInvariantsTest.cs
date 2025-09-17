using SurvivorGame.Configs;

namespace SurvivorGame.Tests;

public class InitAndInvariantsTest
{
    [Fact(DisplayName = "初始化後：所有出口與角色皆在地圖邊界內")]
    public void AllEntitiesAreWithinBounds_AfterInitialization()
    {
        // Arrange
        var cfg = new GameConfig
        {
            Width = 50,
            Height = 50,
            SurvivorCount = 3,
            KillerCount = 3,
            ExitCount = 2,
            Seed = 1234
        };

        // Act
        var s = TestHelpers.BuildInitializedState(cfg);

        // Assert
        foreach (var e in s.Exits)
            Assert.True(TestHelpers.InBounds(s, e), $"Exit {e} out of bounds");

        foreach (var sv in s.Survivors)
            Assert.True(TestHelpers.InBounds(s, sv.Pos), $"Survivor#{sv.Id} at {sv.Pos} out of bounds");

        foreach (var k in s.Killers)
            Assert.True(TestHelpers.InBounds(s, k.Pos), $"Killer#{k.Id} at {k.Pos} out of bounds");
    }

    [Theory(DisplayName = "出口數量正確且不重疊")]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public void ExitCountIsCorrect_AndUnique(int exitCount)
    {
        // Arrange
        var cfg = new GameConfig
        {
            Width = 30,
            Height = 30,
            SurvivorCount = 2,
            KillerCount = 2,
            ExitCount = exitCount,
            Seed = 4321
        };

        // Act
        var s = TestHelpers.BuildInitializedState(cfg);

        // Assert: 數量
        Assert.Equal(exitCount, s.Exits.Count);

        // 位置唯一
        var unique = s.Exits.Distinct().Count();
        Assert.Equal(s.Exits.Count, unique);

        // 都是 Exit 標記
        foreach (var e in s.Exits)
            Assert.True(s.IsExit(e), $"Exit list contains non-exit at {e}");
    }

    [Fact(DisplayName = "出生距離約束：與出口/敵對/同隊的最小距離皆滿足")]
    public void SpawnRespectsMinDistances()
    {
        // Arrange
        var cfg = new GameConfig
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
        var s = TestHelpers.BuildInitializedState(cfg);

        // Assert: 生存者與出口距離
        foreach (var sv in s.Survivors)
        {
            var minDExit = s.Exits.Min(e => TestHelpers.Chebyshev(e, sv.Pos));
            Assert.True(minDExit >= cfg.MinDistToExit,
                $"Survivor#{sv.Id} too close to exit: {minDExit} < {cfg.MinDistToExit}");
        }

        // 鬼與出口距離
        foreach (var k in s.Killers)
        {
            var minDExit = s.Exits.Min(e => TestHelpers.Chebyshev(e, k.Pos));
            Assert.True(minDExit >= cfg.MinDistToExit,
                $"Killer#{k.Id} too close to exit: {minDExit} < {cfg.MinDistToExit}");
        }

        // 生-鬼 最小距離
        foreach (var sv in s.Survivors)
        {
            var mindenemy = s.Killers.Min(k => TestHelpers.Chebyshev(k.Pos, sv.Pos));
            Assert.True(mindenemy >= cfg.MinDistEnemy,
                $"S#{sv.Id} too close to any killer: {mindenemy} < {cfg.MinDistEnemy}");
        }

        // 生-生 同隊最小距離
        for (int i = 0; i < s.Survivors.Count; i++)
        for (int j = i + 1; j < s.Survivors.Count; j++)
        {
            var d = TestHelpers.Chebyshev(s.Survivors[i].Pos, s.Survivors[j].Pos);
            Assert.True(d >= cfg.MinDistAlly, $"Survivors too close: d={d} < {cfg.MinDistAlly}");
        }

        // 鬼-鬼 同隊最小距離
        for (int i = 0; i < s.Killers.Count; i++)
        for (int j = i + 1; j < s.Killers.Count; j++)
        {
            var d = TestHelpers.Chebyshev(s.Killers[i].Pos, s.Killers[j].Pos);
            Assert.True(d >= cfg.MinDistAlly, $"Killers too close: d={d} < {cfg.MinDistAlly}");
        }
    }

    [Fact(DisplayName = "初始化後所有角色互不重疊（不可同格）")]
    public void NoOverlappingActors_OnInitialization()
    {
        // Arrange
        var cfg = new GameConfig
        {
            Width = 50, Height = 50,
            SurvivorCount = 3, KillerCount = 3,
            ExitCount = 2,
            Seed = 2468
        };

        // Act
        var s = TestHelpers.BuildInitializedState(cfg);

        // Assert
        var positions = TestHelpers.GetAllActorPositions(s).Select(x => x.pos).ToList();
        var distinct = positions.Distinct().Count();
        Assert.Equal(positions.Count, distinct);
    }

    [Fact(DisplayName = "相同 Seed 可重現：兩次初始化的佈局一致")]
    public void SameSeed_ProducesIdenticalInitialLayout()
    {
        // Arrange
        var cfg = new GameConfig
        {
            Width = 40,
            Height = 40,
            SurvivorCount = 2,
            KillerCount = 2,
            ExitCount = 2,
            Seed = 13579
        };

        // Act
        var s1 = TestHelpers.BuildInitializedState(cfg);
        var s2 = TestHelpers.BuildInitializedState(cfg);

        // Assert：出口順序與座標相同
        Assert.Equal(s1.Exits.Count, s2.Exits.Count);
        for (int i = 0; i < s1.Exits.Count; i++)
            Assert.Equal(s1.Exits[i], s2.Exits[i]);

        // 角色位置（依 Id）相同
        Assert.Equal(s1.Survivors.Count, s2.Survivors.Count);
        for (int i = 0; i < s1.Survivors.Count; i++)
            Assert.Equal(s1.Survivors[i].Pos, s2.Survivors[i].Pos);

        Assert.Equal(s1.Killers.Count, s2.Killers.Count);
        for (int i = 0; i < s1.Killers.Count; i++)
            Assert.Equal(s1.Killers[i].Pos, s2.Killers[i].Pos);
    }
}