using BetaSharp.Items;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Entities;

public class EntityZombie : EntityMonster
{
    public override EntityType Type => EntityRegistry.Zombie;
    public EntityZombie(IWorldContext world) : base(world)
    {
        Texture = "/mob/zombie.png";
        MovementSpeed = 0.5F;
        attackStrength = 5;
    }

    public override void tickMovement()
    {
        if (World.Environment.CanMonsterSpawn())
        {
            float brightness = GetBrightnessAtEyes(1.0F);
            if (brightness > 0.5F && World.Lighting.HasSkyLight(MathHelper.Floor(X), MathHelper.Floor(Y), MathHelper.Floor(Z)) && Random.NextFloat() * 30.0F < (brightness - 0.4F) * 2.0F)
            {
                FireTicks = 300;
            }
        }

        base.tickMovement();
    }

    protected override String getLivingSound()
    {
        return "mob.zombie";
    }

    protected override String getHurtSound()
    {
        return "mob.zombiehurt";
    }

    protected override String getDeathSound()
    {
        return "mob.zombiedeath";
    }

    protected override int getDropItemId()
    {
        return Item.Feather.id;
    }
}
