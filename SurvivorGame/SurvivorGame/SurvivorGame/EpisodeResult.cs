namespace SurvivorGame;

public class EpisodeResult
{
    public int Episode { get; set; }
    public string Winner { get; set; } = "Killer";
    public int SurvivorScore { get; set; }
    public int KillerScore { get; set; }
    public int Rounds { get; set; }
}