using BetaSharp.NBT;
using BetaSharp.Rules;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Entities;

public class EntityTNTPrimed : Entity
{
    public override EntityType Type => EntityRegistry.PrimedTnt;
    public int fuse;

    public EntityTNTPrimed(IWorldContext world) : base(world)
    {
        fuse = 0;
        preventEntitySpawning = true;
        setBoundingBoxSpacing(0.98F, 0.98F);
        standingEyeHeight = height / 2.0F;
    }

    public EntityTNTPrimed(IWorldContext world, double x, double y, double z) : base(world)
    {
        setPosition(x, y, z);
        float randomAngle = (float)(Random.Shared.NextSingle() * (Math.PI) * 2.0D);
        velocityX = (double)(-MathHelper.Sin(randomAngle * (float)Math.PI / 180.0F) * 0.02F);
        velocityY = (double)0.2F;
        velocityZ = (double)(-MathHelper.Cos(randomAngle * (float)Math.PI / 180.0F) * 0.02F);
        fuse = 80;
        prevX = x;
        prevY = y;
        prevZ = z;
    }


    protected override bool bypassesSteppingEffects()
    {
        return false;
    }

    public override bool isCollidable()
    {
        return !dead;
    }

    public override void tick()
    {
        prevX = x;
        prevY = y;
        prevZ = z;
        velocityY -= (double)0.04F;
        move(velocityX, velocityY, velocityZ);
        velocityX *= (double)0.98F;
        velocityY *= (double)0.98F;
        velocityZ *= (double)0.98F;
        if (onGround)
        {
            velocityX *= (double)0.7F;
            velocityZ *= (double)0.7F;
            velocityY *= -0.5D;
        }

        if (fuse-- <= 0)
        {
            if (!world.IsRemote)
            {
                markDead();
                explode();
            }
            else
            {
                markDead();
            }
        }
        else
        {
            world.Broadcaster.AddParticle("smoke", x, y + 0.5D, z, 0.0D, 0.0D, 0.0D);
        }

    }

    private void explode()
    {
        if (!world.Rules.GetBool(DefaultRules.TntExplodes))
        {
            return;
        }

        const float power = 4.0F;
        world.CreateExplosion((Entity)null, x, y, z, power);
    }

    public override void writeNbt(NBTTagCompound nbt)
    {
        nbt.SetByte("Fuse", (sbyte)fuse);
    }

    public override void readNbt(NBTTagCompound nbt)
    {
        fuse = nbt.GetByte("Fuse");
    }

    public override float getShadowRadius()
    {
        return 0.0F;
    }
}
