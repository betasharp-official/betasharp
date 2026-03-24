using BetaSharp.NBT;
using BetaSharp.Util.Hit;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Entities;

public class EntityFireball : Entity
{
    public override EntityType Type => EntityRegistry.Fireball;
    private int blockX = -1;
    private int blockY = -1;
    private int blockZ = -1;
    private int blockId;
    private bool inGround;
    public int shake;
    public EntityLiving owner;
    private int removalTimer;
    private int inAirTime;
    public double powerX;
    public double powerY;
    public double powerZ;

    public EntityFireball(IWorldContext world) : base(world)
    {
        setBoundingBoxSpacing(1.0F, 1.0F);
    }


    public override bool shouldRender(double var1)
    {
        double var3 = boundingBox.AverageEdgeLength * 4.0D;
        var3 *= 64.0D;
        return var1 < var3 * var3;
    }

    public EntityFireball(IWorldContext world, double x, double y, double z, double var8, double var10, double var12) : base(world)
    {
        setBoundingBoxSpacing(1.0F, 1.0F);
        setPositionAndAnglesKeepPrevAngles(x, y, z, yaw, pitch);
        setPosition(x, y, z);
        double var14 = (double)MathHelper.Sqrt(var8 * var8 + var10 * var10 + var12 * var12);
        powerX = var8 / var14 * 0.1D;
        powerY = var10 / var14 * 0.1D;
        powerZ = var12 / var14 * 0.1D;
    }

    public EntityFireball(IWorldContext world, EntityLiving var2, double var3, double var5, double var7) : base(world)
    {
        owner = var2;
        setBoundingBoxSpacing(1.0F, 1.0F);
        setPositionAndAnglesKeepPrevAngles(var2.x, var2.y, var2.z, var2.yaw, var2.pitch);
        setPosition(x, y, z);
        standingEyeHeight = 0.0F;
        velocityX = velocityY = velocityZ = 0.0D;
        var3 += random.NextGaussian() * 0.4D;
        var5 += random.NextGaussian() * 0.4D;
        var7 += random.NextGaussian() * 0.4D;
        double var9 = (double)MathHelper.Sqrt(var3 * var3 + var5 * var5 + var7 * var7);
        powerX = var3 / var9 * 0.1D;
        powerY = var5 / var9 * 0.1D;
        powerZ = var7 / var9 * 0.1D;
    }

    public override void tick()
    {
        base.tick();
        fireTicks = 10;
        if (shake > 0)
        {
            --shake;
        }

        if (inGround)
        {
            int var1 = world.Reader.GetBlockId(blockX, blockY, blockZ);
            if (var1 == blockId)
            {
                ++removalTimer;
                if (removalTimer == 1200)
                {
                    markDead();
                }

                return;
            }

            inGround = false;
            velocityX *= (double)(random.NextFloat() * 0.2F);
            velocityY *= (double)(random.NextFloat() * 0.2F);
            velocityZ *= (double)(random.NextFloat() * 0.2F);
            removalTimer = 0;
            inAirTime = 0;
        }
        else
        {
            ++inAirTime;
        }

        Vec3D var15 = new Vec3D(x, y, z);
        Vec3D var2 = new Vec3D(x + velocityX, y + velocityY, z + velocityZ);
        HitResult var3 = world.Reader.Raycast(var15, var2);
        var15 = new Vec3D(x, y, z);
        var2 = new Vec3D(x + velocityX, y + velocityY, z + velocityZ);
        if (var3.Type != HitResultType.MISS)
        {
            var2 = new Vec3D(var3.Pos.x, var3.Pos.y, var3.Pos.z);
        }

        Entity var4 = null;
        var var5 = world.Entities.GetEntities(this, boundingBox.Stretch(velocityX, velocityY, velocityZ).Expand(1.0D, 1.0D, 1.0D));
        double var6 = 0.0D;

        for (int var8 = 0; var8 < var5.Count; ++var8)
        {
            Entity var9 = var5[var8];
            if (var9.isCollidable() && (var9 != owner || inAirTime >= 25))
            {
                float var10 = 0.3F;
                Box var11 = var9.boundingBox.Expand((double)var10, (double)var10, (double)var10);
                HitResult var12 = var11.Raycast(var15, var2);
                if (var12.Type != HitResultType.MISS)
                {
                    double var13 = var15.distanceTo(var12.Pos);
                    if (var13 < var6 || var6 == 0.0D)
                    {
                        var4 = var9;
                        var6 = var13;
                    }
                }
            }
        }

        if (var4 != null)
        {
            var3 = new HitResult(var4);
        }

        if (var3.Type != HitResultType.MISS)
        {
            if (!world.IsRemote)
            {
                if (var3.Entity != null && var3.Entity.damage(owner, 0))
                {
                }

                world.CreateExplosion(null, x, y, z, 1.0F, true);
            }

            markDead();
        }

        x += velocityX;
        y += velocityY;
        z += velocityZ;
        float var16 = MathHelper.Sqrt(velocityX * velocityX + velocityZ * velocityZ);
        yaw = (float)(System.Math.Atan2(velocityX, velocityZ) * 180.0D / (double)((float)Math.PI));

        for (pitch = (float)(System.Math.Atan2(velocityY, (double)var16) * 180.0D / (double)((float)Math.PI)); pitch - prevPitch < -180.0F; prevPitch -= 360.0F)
        {
        }

        while (pitch - prevPitch >= 180.0F)
        {
            prevPitch += 360.0F;
        }

        while (yaw - prevYaw < -180.0F)
        {
            prevYaw -= 360.0F;
        }

        while (yaw - prevYaw >= 180.0F)
        {
            prevYaw += 360.0F;
        }

        pitch = prevPitch + (pitch - prevPitch) * 0.2F;
        yaw = prevYaw + (yaw - prevYaw) * 0.2F;
        float var17 = 0.95F;
        if (isInWater())
        {
            for (int var18 = 0; var18 < 4; ++var18)
            {
                float var19 = 0.25F;
                world.Broadcaster.AddParticle("bubble", x - velocityX * (double)var19, y - velocityY * (double)var19, z - velocityZ * (double)var19, velocityX, velocityY, velocityZ);
            }

            var17 = 0.8F;
        }

        velocityX += powerX;
        velocityY += powerY;
        velocityZ += powerZ;
        velocityX *= (double)var17;
        velocityY *= (double)var17;
        velocityZ *= (double)var17;
        world.Broadcaster.AddParticle("smoke", x, y + 0.5D, z, 0.0D, 0.0D, 0.0D);
        setPosition(x, y, z);
    }

    public override void writeNbt(NBTTagCompound nbt)
    {
        nbt.SetShort("xTile", (short)blockX);
        nbt.SetShort("yTile", (short)blockY);
        nbt.SetShort("zTile", (short)blockZ);
        nbt.SetByte("inTile", (sbyte)blockId);
        nbt.SetByte("shake", (sbyte)shake);
        nbt.SetByte("inGround", (sbyte)(inGround ? 1 : 0));
    }

    public override void readNbt(NBTTagCompound nbt)
    {
        blockX = nbt.GetShort("xTile");
        blockY = nbt.GetShort("yTile");
        blockZ = nbt.GetShort("zTile");
        blockId = nbt.GetByte("inTile") & 255;
        shake = nbt.GetByte("shake") & 255;
        inGround = nbt.GetByte("inGround") == 1;
    }

    public override bool isCollidable()
    {
        return true;
    }

    public override float getTargetingMargin()
    {
        return 1.0F;
    }

    public override bool damage(Entity entity, int amount)
    {
        scheduleVelocityUpdate();
        if (entity != null)
        {
            Vec3D? var3 = entity.getLookVector();
            if (var3 != null)
            {
                velocityX = var3.Value.x;
                velocityY = var3.Value.y;
                velocityZ = var3.Value.z;
                powerX = velocityX * 0.1D;
                powerY = velocityY * 0.1D;
                powerZ = velocityZ * 0.1D;
            }

            return true;
        }
        else
        {
            return false;
        }
    }

    public override float getShadowRadius()
    {
        return 0.0F;
    }
}
