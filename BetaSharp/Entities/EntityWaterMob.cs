using BetaSharp.Worlds;

namespace BetaSharp.Entities;

public abstract class EntityWaterMob : EntityCreature, SpawnableEntity
{
    public EntityWaterMob(World world) : base(world)
    {
    }

    public override bool canBreatheUnderwater()
    {
        return true;
    }

    public override bool canSpawn()
    {
        return world.canSpawnEntity(boundingBox);
    }

    public override int getTalkInterval()
    {
        return 120;
    }
}
