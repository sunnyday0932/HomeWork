namespace SurvivorGame;

public class Actor
{
    public int Id { get; }
    public Point Pos { get; set; }
    public bool Alive { get; set; } = true;

    protected Actor(int id, Point pos) { Id = id; Pos = pos; }
    public override string ToString() => $"{GetType().Name}#{Id}@{Pos}";
}