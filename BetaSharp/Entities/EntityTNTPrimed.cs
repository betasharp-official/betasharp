using BetaSharp.NBT;
using BetaSharp.Rules;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Entities;

public sealed class EntityTntPrimed : Entity
{
    public int Fuse;

    public EntityTntPrimed(IWorldContext world) : base(world)
    {
        Fuse = 0;
        PreventEntitySpawning = true;
        SetBoundingBoxSpacing(0.98F, 0.98F);
        StandingEyeHeight = Height / 2.0F;
    }

    public EntityTntPrimed(IWorldContext world, double x, double y, double z) : base(world)
    {
        SetPosition(x, y, z);
        float randomAngle = (float)(System.Random.Shared.NextSingle() * Math.PI * 2.0D);
        VelocityX = -MathHelper.Sin(randomAngle * (float)Math.PI / 180.0F) * 0.02F;
        VelocityY = 0.2F;
        VelocityZ = -MathHelper.Cos(randomAngle * (float)Math.PI / 180.0F) * 0.02F;
        Fuse = 80;
        PrevX = x;
        PrevY = y;
        PrevZ = z;
    }

    public override EntityType Type => EntityRegistry.PrimedTnt;

    public override bool HasCollision => !Dead;


    protected override bool BypassesSteppingEffects() => false;

    public override void Tick()
    {
        PrevX = X;
        PrevY = Y;
        PrevZ = Z;
        VelocityY -= 0.04F;
        Move(VelocityX, VelocityY, VelocityZ);
        VelocityX *= 0.98F;
        VelocityY *= 0.98F;
        VelocityZ *= 0.98F;
        if (OnGround)
        {
            VelocityX *= 0.7F;
            VelocityZ *= 0.7F;
            VelocityY *= -0.5D;
        }

        if (Fuse-- <= 0)
        {
            if (!World.IsRemote)
            {
                MarkDead();
                explode();
            }
            else
            {
                MarkDead();
            }
        }
        else
        {
            World.Broadcaster.AddParticle("smoke", X, Y + 0.5D, Z, 0.0D, 0.0D, 0.0D);
        }
    }

    private void explode()
    {
        if (!World.Rules.GetBool(DefaultRules.TntExplodes)) return;

        const float power = 4.0F;
        World.CreateExplosion(null, X, Y, Z, power);
    }

    protected override void WriteNbt(NBTTagCompound nbt) => nbt.SetByte("Fuse", (sbyte)Fuse);

    protected override void ReadNbt(NBTTagCompound nbt) => Fuse = nbt.GetByte("Fuse");

    public override float GetShadowRadius() => 0.0F;
}
