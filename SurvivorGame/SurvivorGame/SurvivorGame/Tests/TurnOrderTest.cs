using SurvivorGame.Configs;
using SurvivorGame.Tests.Helper;

namespace SurvivorGame.Tests;


/// <summary>
/// 回合流程與事件順序（6~9）
/// 6) 鬼先行（鬼 Phase 可直接捕獲）
/// 7) 生存者相鄰出口且出口上有鬼 → 生存者先逃脫
/// 8) 鬼 Phase 的捕獲
/// 9) 時間到（MaxRounds）鬼勝
/// </summary>
public class TurnOrderTest
{
    [Fact(DisplayName = "鬼先行：鬼 Phase 可直接捕獲，生存者在該回合不再行動")]
    public void KillerMovesFirst_AndCanCapture_BeforeSurvivorActs()
    {
        // 佈局：鬼一步可進入生存者格；確保生存者在該回合前半就被抓
        var config = new GameConfig
        {
            Width = 9, Height = 9,
            SurvivorCount = 0, KillerCount = 0,
            Seed = 1
        };

        var exitPos = new[] { new Point(0, 0) }; // 無關緊要
        var survivors = new[] { new Point(4, 4) };
        var killers = new[] { new Point(3, 3) }; // 斜向一步即可進入 (4,4)

        var gameState = TestBoardBuilder.BuildFixed(config, exitPos, survivors, killers);
        var exitField = StepRunner.BuildExitField(gameState);

        // Killer Phase
        StepRunner.RunKillerPhase(gameState, exitField);

        // 應在 Killer Phase 就移除生存者，且 KillerScore+1
        Assert.Equal(0, gameState.Survivors.Count(s => s.Alive));
        Assert.Equal(1, gameState.KillerScore);

        // 之後 Survivor Phase 即使執行，也不應有生存者行動
        StepRunner.RunSurvivorPhase(gameState, exitField);
        Assert.Equal(0, gameState.Survivors.Count(s => s.Alive));
        Assert.Equal(1, gameState.KillerScore);
    }

    [Fact(DisplayName = "生存者進入出口（出口上有鬼）時，先逃脫再判被捕")]
    public void SurvivorEscapesBeforeCapture_WhenMovingOntoExitEvenIfKillerOnExit()
    {
        // 佈局：出口 (4,4)；鬼一開始就站在出口；生存者相鄰出口 (3,3)
        var config = new GameConfig
        {
            Width = 9, Height = 9,
            SurvivorCount = 0, KillerCount = 0,
            Seed = 2
        };

        var exitPos = new[] { new Point(4, 4) };
        var survivors = new[] { new Point(3, 3) }; // 下一步可走到 (4,4)
        var killers = new[] { new Point(4, 4) }; // 鬼占在出口

        var gameState = TestBoardBuilder.BuildFixed(config, exitPos, survivors, killers);
        var exitField = StepRunner.BuildExitField(gameState);

        // 直接執行 Survivor Phase（假設 Killer Phase 此 turn 不移動或無關）
        StepRunner.RunSurvivorPhase(gameState, exitField);

        // 期望：生存者因踏上出口→立刻逃脫 +1，被移除；鬼分數不變
        Assert.Equal(0, gameState.Survivors.Count(s => s.Alive));
        Assert.Equal(1, gameState.SurvivorScore);
        Assert.Equal(0, gameState.KillerScore);
    }

    [Fact(DisplayName = "鬼 Phase 的捕獲：鬼移動到生存者所在格時立即捕獲 +1")]
    public void KillerPhaseCapture_GrantsKillerPoint_AndRemovesSurvivor()
    {
        var config = new GameConfig
        {
            Width = 9, Height = 9,
            SurvivorCount = 0, KillerCount = 0,
            Seed = 3
        };

        var exitPos = new[] { new Point(0, 0) };
        var survivors = new[] { new Point(5, 5) };
        var killers = new[] { new Point(4, 4) }; // 一步可至 (5,5)

        var gameState = TestBoardBuilder.BuildFixed(config, exitPos, survivors, killers);
        var exitField = StepRunner.BuildExitField(gameState);

        StepRunner.RunKillerPhase(gameState, exitField);

        Assert.Equal(0, gameState.Survivors.Count(s => s.Alive));
        Assert.Equal(1, gameState.KillerScore);
    }

    [Fact(DisplayName = "時間到且仍有生存者未逃脫 => 鬼勝")]
    public void KillerWinsOnTimeout_WhenAnySurvivorRemains()
    {
        // 佈局：彼此很遠，確保此回合內不會有捕獲也不會逃脫
        var config = new GameConfig
        {
            Width = 9, Height = 9,
            SurvivorCount = 0, KillerCount = 0,
            MaxRounds = 1,
            Seed = 4
        };

        var exitPos = new[] { new Point(8, 8) };
        var survivors = new[] { new Point(1, 1) }; // 距出口遠
        var killers = new[] { new Point(0, 8) }; // 距生存者遠

        var gameState = TestBoardBuilder.BuildFixed(config, exitPos, survivors, killers);
        var exitField = StepRunner.BuildExitField(gameState);

        // 跑一個完整回合（鬼 → 生存者）
        StepRunner.RunKillerPhase(gameState, exitField);
        StepRunner.RunSurvivorPhase(gameState, exitField);

        // 模擬時間到（MaxRounds = 1）
        gameState.Round = config.MaxRounds;

        // 仍有生存者存活 → timeout 規則下判定鬼勝
        Assert.True(gameState.Survivors.Any(s => s.Alive), "There should still be a survivor alive for timeout test.");
        var winner = StepRunner.DecideWinnerOnTimeout(gameState);
        Assert.Equal("Killer", winner);
    }
}