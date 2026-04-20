using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Entities;

public class EntityGiantZombie : EntityMonster
{
    public override EntityType Type => EntityRegistry.Giant;
    public EntityGiantZombie(IWorldContext world) : base(world)
    {
        Texture = "/mob/zombie.png";
        MovementSpeed = 0.5F;
        attackStrength = 50;
        Health *= 10;
        StandingEyeHeight *= 6.0F;
        SetBoundingBoxSpacing(Width * 6.0F, Height * 6.0F);
    }

    protected override float getBlockPathWeight(int x, int y, int z)
    {
        return World.Lighting.GetLuminance(x, y, z) - 0.5F;
    }
}
