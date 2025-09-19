using SurvivorGame.Enums;

namespace SurvivorGame;

public class GameEvent
{
    public int Episode { get; set; }
    public int Round { get; set; }
    public GameEventType Type { get; set; }

    // 參與者
    public int? SurvivorId { get; set; }
    public int? KillerId { get; set; }

    // 事件位置（當下格）
    public int X { get; set; }
    public int Y { get; set; }

    public override string ToString()
    {
        return
            $"{{\"episode\":{Episode},\"round\":{Round},\"type\":\"{Type}\",\"survivorId\":{(SurvivorId.HasValue ? SurvivorId.Value : -1)},\"killerId\":{(KillerId.HasValue ? KillerId.Value : -1)},\"x\":{X},\"y\":{Y}}}";
    }
}