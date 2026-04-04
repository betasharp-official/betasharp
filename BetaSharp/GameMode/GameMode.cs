using BetaSharp.DataAsset;

namespace BetaSharp.GameMode;

public class GameMode : BaseDataAsset
{
    public float BrakeSpeed { get; set; } = 1f;
    public bool BlockDrops { get; set; } = true;
    public bool CanBeTargeted { get; set; } = true;
    public bool CanBreak { get; set; } = true;
    public bool CanDrop { get; set; } = true;
    public bool CanExhaustFire { get; set; } = true;
    public bool CanInflictDamage { get; set; } = true;
    public bool CanInteract { get; set; } = true;
    public bool CanPickup { get; set; } = true;
    public bool CanPlace { get; set; } = true;
    public bool CanReceiveDamage { get; set; } = true;
    public bool CanWalk { get; set; } = true;
    public bool DisallowFlying { get; set; } = true;
    public bool FiniteResources { get; set; } = true;
    public bool NeedsAir { get; set; } = true;
    public bool VisibleToWorld { get; set; } = true;
}
