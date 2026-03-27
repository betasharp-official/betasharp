namespace BetaSharp;

[Serializable]
public class GameMode
{
    private static int s_nextId = 4;

    public int Id { get; set; } = -1;
    public string Name { get; init; } = "unnamed";

    public float BrakeSpeed { get; init; } = 1f;
    public bool CanBreak { get; init; } = true;
    public bool CanPlace { get; init; } = true;
    public bool CanInteract { get; init; } = true;
    public bool CanReceiveDamage { get; init; } = true;
    public bool CanInflictDamage { get; init; } = true;
    public bool CanBeTargeted { get; init; } = true;
    public bool CanExhaustFire { get; init; } = true;
    public bool CanPickup { get; init; } = true;
    public bool CanDrop { get; init; } = true;
    public bool FiniteResources { get; init; } = true;
    public bool VisibleToWorld { get; init; } = true;
    public bool BlockDrops { get; init; } = true;

    public GameMode()
    {
        if (Id < 0 || Id > ushort.MaxValue)
        {
            Id = s_nextId++;
        }

        if (Name.Length == 0)
        {
            // ReSharper disable once StringLiteralTypo
            Name = "gamemode " + Id;
        }
    }
}
