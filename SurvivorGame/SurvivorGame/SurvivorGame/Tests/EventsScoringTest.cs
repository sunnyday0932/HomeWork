using SurvivorGame.Configs;
using SurvivorGame.Tests.Helper;

namespace SurvivorGame.Tests;

/// <summary>
/// 事件與分數（29–31）
/// 29) 捕獲加分（任一捕獲發生 → KillerScore +1 且被捕生存者移除）
/// 30) 逃脫加分（生存者踏上出口 → SurvivorScore +1 且該生存者移除）
/// 31) 單回合多事件順序：鬼Phase捕獲 → 生存者Phase移動→先逃脫→再捕獲
/// </summary>
public class EventsScoringTest
{
    private const int Nine = 9;

    [Fact(DisplayName = "捕獲發生時：KillerScore +1 且生存者被移除")]
    public void Capture_IncrementsKillerScore_AndRemovesSurvivor()
    {
        // 佈局：鬼一步可在鬼Phase進到生存者格
        var config = new GameConfig
        {
            Width = 9, Height = 9,
            SurvivorCount = 0, KillerCount = 0,
            SurvivorSight = 2, KillerSight = 3,
            Seed = 901
        };

        var exitPoint = new Point(0, 0); // 無關
        var survivorPos = new Point(5, 5);
        var killerPos = new Point(4, 4); // 斜向一步可達 (5,5)

        var gameState = TestBoardBuilder.BuildFixed(
            config,
            exits: new[] { exitPoint },
            survivors: new[] { survivorPos },
            killers: new[] { killerPos }
        );

        var exitField = StepRunner.BuildExitField(gameState);

        // Act：執行鬼 Phase（預期會捕獲）
        StepRunner.RunKillerPhase(gameState, exitField);

        // Assert
        Assert.Equal(1, gameState.KillerScore);
        Assert.Equal(0, gameState.Survivors.Count(s => s.Alive));
    }

    [Fact(DisplayName = "逃脫發生時：SurvivorScore +1 且該生存者被移除")]
    public void Escape_IncrementsSurvivorScore_AndRemovesSurvivor()
    {
        // 佈局：生存者相鄰出口；鬼遠離不影響
        var config = new GameConfig
        {
            Width = 9, Height = 9,
            SurvivorCount = 0, KillerCount = 0,
            SurvivorSight = 2, KillerSight = 3,
            Seed = 902
        };

        var exitPoint = new Point(4, 4);
        var survivorPos = new Point(3, 3); // 下一步可踏上出口
        var killerPos = new Point(0, 8); // 遠處

        var gameState = TestBoardBuilder.BuildFixed(
            config,
            exits: new[] { exitPoint },
            survivors: new[] { survivorPos },
            killers: new[] { killerPos }
        );

        var exitField = StepRunner.BuildExitField(gameState);

        // Act：只跑生存者 Phase（測純逃脫）
        StepRunner.RunSurvivorPhase(gameState, exitField);

        // Assert
        Assert.Equal(1, gameState.SurvivorScore);
        Assert.Equal(0, gameState.Survivors.Count(s => s.Alive));
        Assert.Equal(0, gameState.KillerScore);
    }

    [Fact(DisplayName = "同回合有捕獲與逃脫：順序為『鬼Phase捕獲 → 生存者Phase先逃脫 → 再捕獲』")]
    public void TurnOrder_WithBothCaptureAndEscape_InSameRound()
    {
        // 佈局：
        // - S1 在出口旁，生存者Phase時應逃脫 +1
        // - S2 將在鬼Phase被 K1 捕獲 +1
        // 確保兩事件同回合內都會發生，並檢視最終分數與存活情況。
        var config = new GameConfig
        {
            Width = Nine, Height = Nine,
            SurvivorCount = 0, KillerCount = 0,
            SurvivorSight = 2, KillerSight = 3,
            Seed = 903
        };

        var exitPoint = new Point(4, 4);

        var s1Start = new Point(3, 3); // 相鄰出口，會逃脫
        var s2Start = new Point(6, 6); // 會被 K1 捕獲
        var k1Start = new Point(5, 5); // 斜向一步可入 (6,6)

        var gameState = TestBoardBuilder.BuildFixed(
            config,
            exits: new[] { exitPoint },
            survivors: new[] { s1Start, s2Start },
            killers: new[] { k1Start }
        );

        var exitField = StepRunner.BuildExitField(gameState);

        // === 鬼 Phase：預期 K1 捕獲 S2 ===
        StepRunner.RunKillerPhase(gameState, exitField);

        // 中場檢查（鬼Phase之後）
        Assert.Equal(1, gameState.KillerScore);
        Assert.Equal(1, gameState.Survivors.Count(s => s.Alive)); // S2 被移除，S1 還在

        // === 生存者 Phase：S1 先移動踏出出口 → 逃脫 +1（若其他生存者移動踩到鬼才會再被捕）===
        StepRunner.RunSurvivorPhase(gameState, exitField);

        // 回合末狀態：S1 逃脫、S2 被捕；生存者全移除
        Assert.Equal(1, gameState.SurvivorScore); // 先逃脫
        Assert.Equal(1, gameState.KillerScore); // 之前的捕獲
        Assert.Equal(0, gameState.Survivors.Count(s => s.Alive));

        // 額外確認：S1 確實是因為踏出口而消失（非被捕）
        // （因我們先在鬼Phase已完成一次捕獲，生存者Phase的 ResolveEscapes 先於「生踩鬼」捕獲）
        // 這裡用「鬼分數未增加到 2」間接驗證逃脫優先。
        Assert.Equal(1, gameState.KillerScore);
    }
}