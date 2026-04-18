using BetaSharp.Items;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Entities;

public class EntityZombie : EntityMonster
{
    public EntityZombie(IWorldContext world) : base(world)
    {
        Texture = "/mob/zombie.png";
        MovementSpeed = 0.5F;
        AttackStrength = 5;
    }

    public override EntityType Type => EntityRegistry.Zombie;

    protected override void TickMovement()
    {
        if (World.Environment.CanMonsterSpawn())
        {
            float brightness = GetBrightnessAtEyes(1.0F);
            if (brightness > 0.5F && World.Lighting.HasSkyLight(MathHelper.Floor(X), MathHelper.Floor(Y), MathHelper.Floor(Z)) && Random.NextFloat() * 30.0F < (brightness - 0.4F) * 2.0F)
            {
                FireTicks = 300;
            }
        }

        base.TickMovement();
    }

    protected override string? LivingSound => "mob.zombie";

    protected override string? HurtSound => "mob.zombiehurt";

    protected override string? DeathSound => "mob.zombiedeath";

    protected override int DropItemId => Item.Feather.id;
}
