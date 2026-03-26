using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Entities;

public abstract class EntityWaterMob : EntityCreature, SpawnableEntity
{
    public EntityWaterMob(IWorldContext world) : base(world)
    {
    }

    public override bool canBreatheUnderwater()
    {
        return true;
    }

    public override bool canSpawn()
    {
        return world.Entities.CanSpawnEntity(boundingBox);
    }

    public override int getTalkInterval()
    {
        return 120;
    }
}
