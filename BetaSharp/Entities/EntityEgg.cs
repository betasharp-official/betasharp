using BetaSharp.Items;
using BetaSharp.NBT;
using BetaSharp.Util.Hit;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Entities;

public class EntityEgg : Entity
{
    public override EntityType Type => EntityRegistry.Egg;
    private int xTile = -1;
    private int yTile = -1;
    private int zTile = -1;
    private int inTile;
    private bool inGround;
    public int shake;
    private EntityLiving thrower;
    private int ticksInGround;
    private int ticksInAir;

    public EntityEgg(IWorldContext world) : base(world)
    {
        setBoundingBoxSpacing(0.25F, 0.25F);
    }


    public override bool shouldRender(double distanceSquared)
    {
        double renderDistance = boundingBox.AverageEdgeLength * 4.0D;
        renderDistance *= 64.0D;
        return distanceSquared < renderDistance * renderDistance;
    }

    public EntityEgg(IWorldContext world, EntityLiving owner) : base(world)
    {
        thrower = owner;
        setBoundingBoxSpacing(0.25F, 0.25F);
        setPositionAndAnglesKeepPrevAngles(owner.x, owner.y + (double)owner.getEyeHeight(), owner.z, owner.yaw, owner.pitch);
        x -= (double)(MathHelper.Cos(yaw / 180.0F * (float)Math.PI) * 0.16F);
        y -= (double)0.1F;
        z -= (double)(MathHelper.Sin(yaw / 180.0F * (float)Math.PI) * 0.16F);
        setPosition(x, y, z);
        standingEyeHeight = 0.0F;
        float speed = 0.4F;
        velocityX = (double)(-MathHelper.Sin(yaw / 180.0F * (float)Math.PI) * MathHelper.Cos(pitch / 180.0F * (float)Math.PI) * speed);
        velocityZ = (double)(MathHelper.Cos(yaw / 180.0F * (float)Math.PI) * MathHelper.Cos(pitch / 180.0F * (float)Math.PI) * speed);
        velocityY = (double)(-MathHelper.Sin(pitch / 180.0F * (float)Math.PI) * speed);
        setHeading(velocityX, velocityY, velocityZ, 1.5F, 1.0F);
    }

    public EntityEgg(IWorldContext world, double x, double y, double z) : base(world)
    {
        ticksInGround = 0;
        setBoundingBoxSpacing(0.25F, 0.25F);
        setPosition(x, y, z);
        standingEyeHeight = 0.0F;
    }

    public void setHeading(double dirX, double dirY, double dirZ, float speed, float spread)
    {
        float length = MathHelper.Sqrt(dirX * dirX + dirY * dirY + dirZ * dirZ);
        dirX /= (double)length;
        dirY /= (double)length;
        dirZ /= (double)length;
        dirX += random.NextGaussian() * (double)0.0075F * (double)spread;
        dirY += random.NextGaussian() * (double)0.0075F * (double)spread;
        dirZ += random.NextGaussian() * (double)0.0075F * (double)spread;
        dirX *= (double)speed;
        dirY *= (double)speed;
        dirZ *= (double)speed;
        velocityX = dirX;
        velocityY = dirY;
        velocityZ = dirZ;
        float horizontalLength = MathHelper.Sqrt(dirX * dirX + dirZ * dirZ);
        prevYaw = yaw = (float)(System.Math.Atan2(dirX, dirZ) * 180.0D / (double)((float)Math.PI));
        prevPitch = pitch = (float)(System.Math.Atan2(dirY, (double)horizontalLength) * 180.0D / (double)((float)Math.PI));
        ticksInGround = 0;
    }

    public override void setVelocityClient(double motionX, double motionY, double motionZ)
    {
        velocityX = motionX;
        velocityY = motionY;
        velocityZ = motionZ;
        if (prevPitch == 0.0F && prevYaw == 0.0F)
        {
            float horizontalLength = MathHelper.Sqrt(motionX * motionX + motionZ * motionZ);
            prevYaw = yaw = (float)(System.Math.Atan2(motionX, motionZ) * 180.0D / (double)((float)Math.PI));
            prevPitch = pitch = (float)(System.Math.Atan2(motionY, (double)horizontalLength) * 180.0D / (double)((float)Math.PI));
        }

    }

    public override void tick()
    {
        lastTickX = x;
        lastTickY = y;
        lastTickZ = z;
        base.tick();
        if (shake > 0)
        {
            --shake;
        }

        if (inGround)
        {
            int blockId = world.Reader.GetBlockId(xTile, yTile, zTile);
            if (blockId == inTile)
            {
                ++ticksInGround;
                if (ticksInGround == 1200)
                {
                    markDead();
                }

                return;
            }

            inGround = false;
            velocityX *= (double)(random.NextFloat() * 0.2F);
            velocityY *= (double)(random.NextFloat() * 0.2F);
            velocityZ *= (double)(random.NextFloat() * 0.2F);
            ticksInGround = 0;
            ticksInAir = 0;
        }
        else
        {
            ++ticksInAir;
        }

        Vec3D rayStart = new Vec3D(x, y, z);
        Vec3D rayEnd = new Vec3D(x + velocityX, y + velocityY, z + velocityZ);
        HitResult hit = world.Reader.Raycast(rayStart, rayEnd);
        rayStart = new Vec3D(x, y, z);
        rayEnd = new Vec3D(x + velocityX, y + velocityY, z + velocityZ);
        if (hit.Type != HitResultType.MISS)
        {
            rayEnd = new Vec3D(hit.Pos.x, hit.Pos.y, hit.Pos.z);
        }

        if (!world.IsRemote)
        {
            Entity hitEntity = null;
            var entities = world.Entities.GetEntities(this, boundingBox.Stretch(velocityX, velocityY, velocityZ).Expand(1.0D, 1.0D, 1.0D));
            double minHitDistance = 0.0D;

            for (int i = 0; i < entities.Count; ++i)
            {
                Entity entity = entities[i];
                if (entity.isCollidable() && (entity != thrower || ticksInAir >= 5))
                {
                    float expandAmount = 0.3F;
                    Box expandedBox = entity.boundingBox.Expand((double)expandAmount, (double)expandAmount, (double)expandAmount);
                    HitResult entityHit = expandedBox.Raycast(rayStart, rayEnd);
                    if (entityHit.Type != HitResultType.MISS)
                    {
                        double distance = rayStart.distanceTo(entityHit.Pos);
                        if (distance < minHitDistance || minHitDistance == 0.0D)
                        {
                            hitEntity = entity;
                            minHitDistance = distance;
                        }
                    }
                }
            }

            if (hitEntity != null)
            {
                hit = new HitResult(hitEntity);
            }
        }

        if (hit.Type != HitResultType.MISS)
        {
            if (hit.Entity != null && hit.Entity.damage(thrower, 0))
            {
            }

            if (!world.IsRemote && random.NextInt(8) == 0)
            {
                byte chickenCount = 1;
                if (random.NextInt(32) == 0)
                {
                    chickenCount = 4;
                }

                for (int i = 0; i < chickenCount; ++i)
                {
                    EntityChicken chicken = new EntityChicken(world);
                    chicken.setPositionAndAnglesKeepPrevAngles(x, y, z, yaw, 0.0F);
                    world.SpawnEntity(chicken);
                }
            }

            for (int i = 0; i < 8; ++i)
            {
                world.Broadcaster.AddParticle("snowballpoof", x, y, z, 0.0D, 0.0D, 0.0D);
            }

            markDead();
        }

        x += velocityX;
        y += velocityY;
        z += velocityZ;
        float horizontalSpeed = MathHelper.Sqrt(velocityX * velocityX + velocityZ * velocityZ);
        yaw = (float)(System.Math.Atan2(velocityX, velocityZ) * 180.0D / (double)((float)Math.PI));

        for (pitch = (float)(System.Math.Atan2(velocityY, (double)horizontalSpeed) * 180.0D / (double)((float)Math.PI)); pitch - prevPitch < -180.0F; prevPitch -= 360.0F)
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
        float drag = 0.99F;
        float gravity = 0.03F;
        if (isInWater())
        {
            for (int i = 0; i < 4; ++i)
            {
                float trailOffset = 0.25F;
                world.Broadcaster.AddParticle("bubble", x - velocityX * (double)trailOffset, y - velocityY * (double)trailOffset, z - velocityZ * (double)trailOffset, velocityX, velocityY, velocityZ);
            }

            drag = 0.8F;
        }

        velocityX *= (double)drag;
        velocityY *= (double)drag;
        velocityZ *= (double)drag;
        velocityY -= (double)gravity;
        setPosition(x, y, z);
    }

    public override void writeNbt(NBTTagCompound nbt)
    {
        nbt.SetShort("xTile", (short)xTile);
        nbt.SetShort("yTile", (short)yTile);
        nbt.SetShort("zTile", (short)zTile);
        nbt.SetByte("inTile", (sbyte)inTile);
        nbt.SetByte("shake", (sbyte)shake);
        nbt.SetByte("inGround", (sbyte)(inGround ? 1 : 0));
    }

    public override void readNbt(NBTTagCompound nbt)
    {
        xTile = nbt.GetShort("xTile");
        yTile = nbt.GetShort("yTile");
        zTile = nbt.GetShort("zTile");
        inTile = nbt.GetByte("inTile") & 255;
        shake = nbt.GetByte("shake") & 255;
        inGround = nbt.GetByte("inGround") == 1;
    }

    public override void onPlayerInteraction(EntityPlayer player)
    {
        if (inGround && thrower == player && shake <= 0 && player.inventory.AddItemStackToInventory(new ItemStack(Item.ARROW, 1)))
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
