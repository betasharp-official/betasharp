using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Entities;

public class EntityGiantZombie : EntityMonster
{
    public EntityGiantZombie(IWorldContext world) : base(world)
    {
        Texture = "/mob/zombie.png";
        MovementSpeed = 0.5F;
        AttackStrength = 50;
        Health *= 10;
        StandingEyeHeight *= 6.0F;
        SetBoundingBoxSpacing(Width * 6.0F, Height * 6.0F);
    }

    public override EntityType Type => EntityRegistry.Giant;

    protected sealed override void SetBoundingBoxSpacing(float widthOffset, float heightOffset) => base.SetBoundingBoxSpacing(widthOffset, heightOffset);

    protected override float GetBlockPathWeight(int x, int y, int z) => World.Lighting.GetLuminance(x, y, z) - 0.5F;
}
