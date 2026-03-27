using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Entities;

public class EntityGiantZombie : EntityMonster
{
    public override EntityType Type => EntityRegistry.Giant;
    public EntityGiantZombie(IWorldContext world) : base(world)
    {
        texture = "/mob/zombie.png";
        movementSpeed = 0.5F;
        attackStrength = 50;
        health *= 10;
        standingEyeHeight *= 6.0F;
        setBoundingBoxSpacing(width * 6.0F, height * 6.0F);
    }

    protected override float getBlockPathWeight(int x, int y, int z)
    {
        return world.Lighting.GetLuminance(x, y, z) - 0.5F;
    }
}
