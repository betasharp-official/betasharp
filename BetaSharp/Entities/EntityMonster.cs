using BetaSharp.NBT;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Entities;

public abstract class EntityMonster : EntityCreature, Monster
{
    protected int attackStrength = 2;

    public EntityMonster(IWorldContext world) : base(world)
    {
        Health = 20;
    }

    public override void tickMovement()
    {
        float brightness = GetBrightnessAtEyes(1.0F);
        if (brightness > 0.5F)
        {
            EntityAge += 2;
        }

        base.tickMovement();
    }

    public override void Tick()
    {
        base.Tick();
        if (!World.IsRemote && World.Difficulty == 0)
        {
            MarkDead();
        }

    }

    protected override Entity? findPlayerToAttack()
    {
        EntityPlayer? player = World.Entities.GetClosestPlayerTarget(this.X, this.Y, this.Z, 16.0D);
        return player != null && canSee(player) ? player : null;
    }

    public override bool Damage(Entity entity, int amount)
    {
        if (base.Damage(entity, amount))
        {
            if (Passenger != entity && Vehicle != entity)
            {
                if (entity != this)
                {
                    if (entity is EntityPlayer { GameMode.CanBeTargeted: true })
                    {
                        playerToAttack = entity;
                    }
                }

                return true;
            }
            else
            {
                return true;
            }
        }
        else
        {
            return false;
        }
    }

    protected override void attackEntity(Entity entity, float distance)
    {
        if (AttackTime <= 0 && distance < 2.0F && entity.BoundingBox.MaxY > BoundingBox.MinY && entity.BoundingBox.MinY < BoundingBox.MaxY)
        {
            AttackTime = 20;
            entity.Damage(this, attackStrength);
        }

    }

    protected override float getBlockPathWeight(int x, int y, int z)
    {
        return 0.5F - World.Lighting.GetLuminance(x, y, z);
    }

    public override void WriteNbt(NBTTagCompound nbt)
    {
        base.WriteNbt(nbt);
    }

    public override void ReadNbt(NBTTagCompound nbt)
    {
        base.ReadNbt(nbt);
    }

    public override bool canSpawn()
    {
        int x = MathHelper.Floor(this.X);
        int y = MathHelper.Floor(BoundingBox.MinY);
        int z = MathHelper.Floor(this.Z);
        if (World.Lighting.GetBrightness(LightType.Sky, x, y, z) > Random.NextInt(32))
        {
            return false;
        }

        int lightLevel = World.Lighting.GetLightLevel(x, y, z);
        if (World.Environment.IsThundering())
        {
            int ambientDarkness = World.Environment.AmbientDarkness;
            World.Environment.AmbientDarkness = 10;
            lightLevel = World.Lighting.GetLightLevel(x, y, z);
            World.Environment.AmbientDarkness = ambientDarkness;
        }

        return lightLevel <= Random.NextInt(8) && base.canSpawn();
    }
}
