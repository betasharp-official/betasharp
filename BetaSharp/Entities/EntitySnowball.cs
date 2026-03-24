using BetaSharp.Items;
using BetaSharp.NBT;
using BetaSharp.Util.Hit;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Entities;

public class EntitySnowball : Entity
{
    public override EntityType Type => EntityRegistry.Snowball;
    private int xTileSnowball = -1;
    private int yTileSnowball = -1;
    private int zTileSnowball = -1;
    private int inTileSnowball;
    private bool inGroundSnowball;
    public int shakeSnowball;
    private EntityLiving thrower;
    private int ticksInGroundSnowball;
    private int ticksInAirSnowball;

    public EntitySnowball(IWorldContext world) : base(world)
    {
        setBoundingBoxSpacing(0.25F, 0.25F);
    }


    public override bool shouldRender(double var1)
    {
        double var3 = boundingBox.AverageEdgeLength * 4.0D;
        var3 *= 64.0D;
        return var1 < var3 * var3;
    }

    public EntitySnowball(IWorldContext world, EntityLiving var2) : base(world)
    {
        thrower = var2;
        setBoundingBoxSpacing(0.25F, 0.25F);
        setPositionAndAnglesKeepPrevAngles(var2.x, var2.y + (double)var2.getEyeHeight(), var2.z, var2.yaw, var2.pitch);
        x -= (double)(MathHelper.Cos(yaw / 180.0F * (float)System.Math.PI) * 0.16F);
        y -= (double)0.1F;
        z -= (double)(MathHelper.Sin(yaw / 180.0F * (float)System.Math.PI) * 0.16F);
        setPosition(x, y, z);
        standingEyeHeight = 0.0F;
        float var3 = 0.4F;
        velocityX = (double)(-MathHelper.Sin(yaw / 180.0F * (float)System.Math.PI) * MathHelper.Cos(pitch / 180.0F * (float)System.Math.PI) * var3);
        velocityZ = (double)(MathHelper.Cos(yaw / 180.0F * (float)System.Math.PI) * MathHelper.Cos(pitch / 180.0F * (float)System.Math.PI) * var3);
        velocityY = (double)(-MathHelper.Sin(pitch / 180.0F * (float)System.Math.PI) * var3);
        setSnowballHeading(velocityX, velocityY, velocityZ, 1.5F, 1.0F);
    }

    public EntitySnowball(IWorldContext world, double x, double y, double z) : base(world)
    {
        ticksInGroundSnowball = 0;
        setBoundingBoxSpacing(0.25F, 0.25F);
        setPosition(x, y, z);
        standingEyeHeight = 0.0F;
    }

    public void setSnowballHeading(double var1, double var3, double var5, float var7, float var8)
    {
        float var9 = MathHelper.Sqrt(var1 * var1 + var3 * var3 + var5 * var5);
        var1 /= (double)var9;
        var3 /= (double)var9;
        var5 /= (double)var9;
        var1 += random.NextGaussian() * (double)0.0075F * (double)var8;
        var3 += random.NextGaussian() * (double)0.0075F * (double)var8;
        var5 += random.NextGaussian() * (double)0.0075F * (double)var8;
        var1 *= (double)var7;
        var3 *= (double)var7;
        var5 *= (double)var7;
        velocityX = var1;
        velocityY = var3;
        velocityZ = var5;
        float var10 = MathHelper.Sqrt(var1 * var1 + var5 * var5);
        prevYaw = yaw = (float)(System.Math.Atan2(var1, var5) * 180.0D / (double)((float)System.Math.PI));
        prevPitch = pitch = (float)(System.Math.Atan2(var3, (double)var10) * 180.0D / (double)((float)System.Math.PI));
        ticksInGroundSnowball = 0;
    }

    public override void setVelocityClient(double var1, double var3, double var5)
    {
        velocityX = var1;
        velocityY = var3;
        velocityZ = var5;
        if (prevPitch == 0.0F && prevYaw == 0.0F)
        {
            float var7 = MathHelper.Sqrt(var1 * var1 + var5 * var5);
            prevYaw = yaw = (float)(System.Math.Atan2(var1, var5) * 180.0D / (double)((float)System.Math.PI));
            prevPitch = pitch = (float)(System.Math.Atan2(var3, (double)var7) * 180.0D / (double)((float)System.Math.PI));
        }

    }

    public override void tick()
    {
        lastTickX = x;
        lastTickY = y;
        lastTickZ = z;
        base.tick();
        if (shakeSnowball > 0)
        {
            --shakeSnowball;
        }

        if (inGroundSnowball)
        {
            int var1 = world.Reader.GetBlockId(xTileSnowball, yTileSnowball, zTileSnowball);
            if (var1 == inTileSnowball)
            {
                ++ticksInGroundSnowball;
                if (ticksInGroundSnowball == 1200)
                {
                    markDead();
                }

                return;
            }

            inGroundSnowball = false;
            velocityX *= (double)(random.NextFloat() * 0.2F);
            velocityY *= (double)(random.NextFloat() * 0.2F);
            velocityZ *= (double)(random.NextFloat() * 0.2F);
            ticksInGroundSnowball = 0;
            ticksInAirSnowball = 0;
        }
        else
        {
            ++ticksInAirSnowball;
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

        if (!world.IsRemote)
        {
            Entity var4 = null;
            var var5 = world.Entities.GetEntities(this, boundingBox.Stretch(velocityX, velocityY, velocityZ).Expand(1.0D, 1.0D, 1.0D));
            double var6 = 0.0D;

            for (int var8 = 0; var8 < var5.Count; ++var8)
            {
                Entity var9 = var5[var8];
                if (var9.isCollidable() && (var9 != thrower || ticksInAirSnowball >= 5))
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
        }

        if (var3.Type != HitResultType.MISS)
        {
            if (var3.Entity != null && var3.Entity.damage(thrower, 0))
            {
            }

            for (int var16 = 0; var16 < 8; ++var16)
            {
                world.Broadcaster.AddParticle("snowballpoof", x, y, z, 0.0D, 0.0D, 0.0D);
            }

            markDead();
        }

        x += velocityX;
        y += velocityY;
        z += velocityZ;
        float var17 = MathHelper.Sqrt(velocityX * velocityX + velocityZ * velocityZ);
        yaw = (float)(System.Math.Atan2(velocityX, velocityZ) * 180.0D / (double)((float)System.Math.PI));

        for (pitch = (float)(System.Math.Atan2(velocityY, (double)var17) * 180.0D / (double)((float)System.Math.PI)); pitch - prevPitch < -180.0F; prevPitch -= 360.0F)
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
        float var18 = 0.99F;
        float var19 = 0.03F;
        if (isInWater())
        {
            for (int var7 = 0; var7 < 4; ++var7)
            {
                float var20 = 0.25F;
                world.Broadcaster.AddParticle("bubble", x - velocityX * (double)var20, y - velocityY * (double)var20, z - velocityZ * (double)var20, velocityX, velocityY, velocityZ);
            }

            var18 = 0.8F;
        }

        velocityX *= (double)var18;
        velocityY *= (double)var18;
        velocityZ *= (double)var18;
        velocityY -= (double)var19;
        setPosition(x, y, z);
    }

    public override void writeNbt(NBTTagCompound nbt)
    {
        nbt.SetShort("xTile", (short)xTileSnowball);
        nbt.SetShort("yTile", (short)yTileSnowball);
        nbt.SetShort("zTile", (short)zTileSnowball);
        nbt.SetByte("inTile", (sbyte)inTileSnowball);
        nbt.SetByte("shake", (sbyte)shakeSnowball);
        nbt.SetByte("inGround", (sbyte)(inGroundSnowball ? 1 : 0));
    }

    public override void readNbt(NBTTagCompound nbt)
    {
        xTileSnowball = nbt.GetShort("xTile");
        yTileSnowball = nbt.GetShort("yTile");
        zTileSnowball = nbt.GetShort("zTile");
        inTileSnowball = nbt.GetByte("inTile") & 255;
        shakeSnowball = nbt.GetByte("shake") & 255;
        inGroundSnowball = nbt.GetByte("inGround") == 1;
    }

    public override void onPlayerInteraction(EntityPlayer player)
    {
        if (inGroundSnowball && thrower == player && shakeSnowball <= 0 && player.inventory.addItemStackToInventory(new ItemStack(Item.ARROW, 1)))
        {
            world.Broadcaster.PlaySoundAtEntity(this, "random.pop", 0.2F, ((random.NextFloat() - random.NextFloat()) * 0.7F + 1.0F) * 2.0F);
            player.sendPickup(this, 1);
            markDead();
        }

    }

    public override float getShadowRadius()
    {
        return 0.0F;
    }
}
