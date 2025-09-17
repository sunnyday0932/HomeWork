namespace SurvivorGame.Configs;

public class GameConfig
{
    // 地圖與回合
    public int Width { get; init; } = 50;
    public int Height { get; init; } = 50;
    public int MaxRounds { get; init; } = 100;

    // 人數
    public int SurvivorCount { get; init; } = 3;
    public int KillerCount { get; init; } = 3;

    // 視野（Chebyshev 半徑）
    public int SurvivorSight { get; init; } = 2; // 5x5
    public int KillerSight { get; init; } = 3;   // 7x7

    // 起始約束
    public int MinDistToExit { get; init; } = 8;
    public int MinDistEnemy { get; init; } = 4;
    public int MinDistAlly { get; init; } = 2;

    // 出口
    public int ExitCount { get; init; } = 2;

    // Survivor Scoring 參數
    public double Alpha_Exit = 1.0;                 // 越靠近出口越好
    public double Beta_Safety = 1.0;                // 越遠離鬼越好
    public double Gamma_FlowPenalty = 0.0;          // 擁擠懲罰（POC先0）
    public double Omega_Margin = 0.3;               // (D_killer - D_exit)
    public double UnknownAreaPenalty = 0.2;         // 看不到鬼的5x5未知微懲罰
    public double LastSeenShadowPenalty = 0.8;      // last-seen 陰影區懲罰
    public int LastSeenShadowCap = 5;               // 陰影最大半徑
    public double OneStepLookaheadPenalty = 1.5;    // 鬼下一步可能貼臉的懲罰
    public int StrongCloseDanger = 2;               // D_killer ≤ 2 強懲
    public double StrongClosePenalty = 9999;        // 大負分等效禁止

    // Killer Scoring 參數（簡版）
    public double KillerSpacingPenalty = 0.3;       // 鬼之間太近的懲罰
    public double KillerInterceptBonus = 0.5;       // 接近生→出口路徑的加分（POC簡化）
    public int LastSeenTimeout = 8;                 // N 回合未見則轉巡邏出口

    // 模擬
    public int Seed { get; set; } = 42;
    public bool VerboseLog { get; set; } = false;  // 大量逐步 log
    public bool PrintAsciiMapEachRound { get; set; } = false;
}