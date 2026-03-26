using BetaSharp.Items;
using BetaSharp.NBT;
using BetaSharp.Util.Hit;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Entities;

public class EntityEgg : Entity
{
    public override EntityType Type => EntityRegistry.Egg;
    private int field_20056_b = -1;
    private int field_20055_c = -1;
    private int field_20054_d = -1;
    private int field_20053_e;
    private bool field_20052_f;
    public int field_20057_a;
    private EntityLiving field_20051_g;
    private int field_20050_h;
    private int field_20049_i;

    public EntityEgg(IWorldContext world) : base(world)
    {
        setBoundingBoxSpacing(0.25F, 0.25F);
    }


    public override bool shouldRender(double var1)
    {
        double var3 = boundingBox.AverageEdgeLength * 4.0D;
        var3 *= 64.0D;
        return var1 < var3 * var3;
    }

    public EntityEgg(IWorldContext world, EntityLiving var2) : base(world)
    {
        field_20051_g = var2;
        setBoundingBoxSpacing(0.25F, 0.25F);
        setPositionAndAnglesKeepPrevAngles(var2.x, var2.y + (double)var2.getEyeHeight(), var2.z, var2.yaw, var2.pitch);
        x -= (double)(MathHelper.Cos(yaw / 180.0F * (float)Math.PI) * 0.16F);
        y -= (double)0.1F;
        z -= (double)(MathHelper.Sin(yaw / 180.0F * (float)Math.PI) * 0.16F);
        setPosition(x, y, z);
        standingEyeHeight = 0.0F;
        float var3 = 0.4F;
        velocityX = (double)(-MathHelper.Sin(yaw / 180.0F * (float)Math.PI) * MathHelper.Cos(pitch / 180.0F * (float)Math.PI) * var3);
        velocityZ = (double)(MathHelper.Cos(yaw / 180.0F * (float)Math.PI) * MathHelper.Cos(pitch / 180.0F * (float)Math.PI) * var3);
        velocityY = (double)(-MathHelper.Sin(pitch / 180.0F * (float)Math.PI) * var3);
        setEggHeading(velocityX, velocityY, velocityZ, 1.5F, 1.0F);
    }

    public EntityEgg(IWorldContext world, double x, double y, double z) : base(world)
    {
        field_20050_h = 0;
        setBoundingBoxSpacing(0.25F, 0.25F);
        setPosition(x, y, z);
        standingEyeHeight = 0.0F;
    }

    public void setEggHeading(double var1, double var3, double var5, float var7, float var8)
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
        prevYaw = yaw = (float)(System.Math.Atan2(var1, var5) * 180.0D / (double)((float)Math.PI));
        prevPitch = pitch = (float)(System.Math.Atan2(var3, (double)var10) * 180.0D / (double)((float)Math.PI));
        field_20050_h = 0;
    }

    public override void setVelocityClient(double var1, double var3, double var5)
    {
        velocityX = var1;
        velocityY = var3;
        velocityZ = var5;
        if (prevPitch == 0.0F && prevYaw == 0.0F)
        {
            float var7 = MathHelper.Sqrt(var1 * var1 + var5 * var5);
            prevYaw = yaw = (float)(System.Math.Atan2(var1, var5) * 180.0D / (double)((float)Math.PI));
            prevPitch = pitch = (float)(System.Math.Atan2(var3, (double)var7) * 180.0D / (double)((float)Math.PI));
        }

    }

    public override void tick()
    {
        lastTickX = x;
        lastTickY = y;
        lastTickZ = z;
        base.tick();
        if (field_20057_a > 0)
        {
            --field_20057_a;
        }

        if (field_20052_f)
        {
            int var1 = world.Reader.GetBlockId(field_20056_b, field_20055_c, field_20054_d);
            if (var1 == field_20053_e)
            {
                ++field_20050_h;
                if (field_20050_h == 1200)
                {
                    markDead();
                }

                return;
            }

            field_20052_f = false;
            velocityX *= (double)(random.NextFloat() * 0.2F);
            velocityY *= (double)(random.NextFloat() * 0.2F);
            velocityZ *= (double)(random.NextFloat() * 0.2F);
            field_20050_h = 0;
            field_20049_i = 0;
        }
        else
        {
            ++field_20049_i;
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
                if (var9.isCollidable() && (var9 != field_20051_g || field_20049_i >= 5))
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
            if (var3.Entity != null && var3.Entity.damage(field_20051_g, 0))
            {
            }

            if (!world.IsRemote && random.NextInt(8) == 0)
            {
                byte var16 = 1;
                if (random.NextInt(32) == 0)
                {
                    var16 = 4;
                }

                for (int var17 = 0; var17 < var16; ++var17)
                {
                    EntityChicken var21 = new EntityChicken(world);
                    var21.setPositionAndAnglesKeepPrevAngles(x, y, z, yaw, 0.0F);
                    world.SpawnEntity(var21);
                }
            }

            for (int var18 = 0; var18 < 8; ++var18)
            {
                world.Broadcaster.AddParticle("snowballpoof", x, y, z, 0.0D, 0.0D, 0.0D);
            }

            markDead();
        }

        x += velocityX;
        y += velocityY;
        z += velocityZ;
        float var20 = MathHelper.Sqrt(velocityX * velocityX + velocityZ * velocityZ);
        yaw = (float)(System.Math.Atan2(velocityX, velocityZ) * 180.0D / (double)((float)Math.PI));

        for (pitch = (float)(System.Math.Atan2(velocityY, (double)var20) * 180.0D / (double)((float)Math.PI)); pitch - prevPitch < -180.0F; prevPitch -= 360.0F)
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
        float var19 = 0.99F;
        float var22 = 0.03F;
        if (isInWater())
        {
            for (int var7 = 0; var7 < 4; ++var7)
            {
                float var23 = 0.25F;
                world.Broadcaster.AddParticle("bubble", x - velocityX * (double)var23, y - velocityY * (double)var23, z - velocityZ * (double)var23, velocityX, velocityY, velocityZ);
            }

            var19 = 0.8F;
        }

        velocityX *= (double)var19;
        velocityY *= (double)var19;
        velocityZ *= (double)var19;
        velocityY -= (double)var22;
        setPosition(x, y, z);
    }

    public override void writeNbt(NBTTagCompound nbt)
    {
        nbt.SetShort("xTile", (short)field_20056_b);
        nbt.SetShort("yTile", (short)field_20055_c);
        nbt.SetShort("zTile", (short)field_20054_d);
        nbt.SetByte("inTile", (sbyte)field_20053_e);
        nbt.SetByte("shake", (sbyte)field_20057_a);
        nbt.SetByte("inGround", (sbyte)(field_20052_f ? 1 : 0));
    }

    public override void readNbt(NBTTagCompound nbt)
    {
        field_20056_b = nbt.GetShort("xTile");
        field_20055_c = nbt.GetShort("yTile");
        field_20054_d = nbt.GetShort("zTile");
        field_20053_e = nbt.GetByte("inTile") & 255;
        field_20057_a = nbt.GetByte("shake") & 255;
        field_20052_f = nbt.GetByte("inGround") == 1;
    }

    public override void onPlayerInteraction(EntityPlayer player)
    {
        if (field_20052_f && field_20051_g == player && field_20057_a <= 0 && player.inventory.addItemStackToInventory(new ItemStack(Item.ARROW, 1)))
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
