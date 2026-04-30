using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Entities;

public abstract class EntityMonster : EntityCreature, Monster
{
    protected int AttackStrength = 2;

    protected EntityMonster(IWorldContext world) : base(world) => Health = 20;

    protected override void TickMovement()
    {
        float brightness = GetBrightnessAtEyes(1.0F);
        if (brightness > 0.5F)
        {
            EntityAge += 2;
        }

        base.TickMovement();
    }

    public override void Tick()
    {
        base.Tick();
        if (World is { IsRemote: false, Difficulty: 0 })
        {
            MarkDead();
        }
    }

    protected override Entity? FindPlayerToAttack()
    {
        EntityPlayer? player = World.Entities.GetClosestPlayerTarget(X, Y, Z, 16.0D);
        return player != null && CanSee(player) ? player : null;
    }

    public override bool Damage(Entity? entity, int amount)
    {
        if (!base.Damage(entity, amount)) return false;
        if (Equals(Passenger, entity) || Equals(Vehicle, entity)) return true;
        if (Equals(entity, this)) return true;
        if (entity is EntityPlayer { GameMode.CanBeTargeted: true }) Target = entity;
        return true;
    }

    protected override void attackEntity(Entity entity, float distance)
    {
        if (AttackTime > 0 || !(distance < 2.0F) || !(entity.BoundingBox.MaxY > BoundingBox.MinY) || !(entity.BoundingBox.MinY < BoundingBox.MaxY))
        {
            return;
        }

        AttackTime = 20;
        entity.Damage(this, AttackStrength);
    }

    protected override float GetBlockPathWeight(int x, int y, int z) => 0.5F - World.Lighting.GetLuminance(x, y, z);

    public override bool CanSpawn()
    {
        int x = MathHelper.Floor(X);
        int y = MathHelper.Floor(BoundingBox.MinY);
        int z = MathHelper.Floor(Z);
        if (World.Lighting.GetBrightness(LightType.Sky, x, y, z) > Random.NextInt(32)) return false;

        int lightLevel = World.Lighting.GetLightLevel(x, y, z);
        if (!World.Environment.IsThundering()) return lightLevel <= Random.NextInt(8) && base.CanSpawn();

        int ambientDarkness = World.Environment.AmbientDarkness;
        World.Environment.AmbientDarkness = 10;
        lightLevel = World.Lighting.GetLightLevel(x, y, z);
        World.Environment.AmbientDarkness = ambientDarkness;

        return lightLevel <= Random.NextInt(8) && base.CanSpawn();
    }
}
