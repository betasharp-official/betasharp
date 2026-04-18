using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Entities;

public abstract class EntityWaterMob(IWorldContext world) : EntityCreature(world), SpawnableEntity
{
    protected override bool canBreatheUnderwater() => true;

    public override bool CanSpawn() => World.Entities.CanSpawnEntity(BoundingBox);

    protected override int TalkInterval => 120;
}
