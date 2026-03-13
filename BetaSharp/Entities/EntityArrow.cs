using BetaSharp.Blocks;
using BetaSharp.Items;
using BetaSharp.NBT;
using BetaSharp.Util.Hit;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Entities;

public class EntityArrow : Entity
{
    public int arrowShake;
    public bool doesArrowBelongToPlayer;
    private int inData;
    private bool inGround;
    private int inTile;
    public EntityLiving owner;
    private int ticksInAir;
    private int ticksInGround;
    private int xTile = -1;
    private int yTile = -1;
    private int zTile = -1;

    public EntityArrow(IWorldContext world) : base(world) => setBoundingBoxSpacing(0.5F, 0.5F);

    public EntityArrow(IWorldContext world, double x, double y, double z) : base(world)
    {
        setBoundingBoxSpacing(0.5F, 0.5F);
        setPosition(x, y, z);
        standingEyeHeight = 0.0F;
    }

    public EntityArrow(IWorldContext world, EntityLiving owner) : base(world)
    {
        this.owner = owner;
        doesArrowBelongToPlayer = owner is EntityPlayer;
        setBoundingBoxSpacing(0.5F, 0.5F);
        setPositionAndAnglesKeepPrevAngles(owner.x, owner.y + owner.getEyeHeight(), owner.z, owner.yaw, owner.pitch);
        x -= MathHelper.Cos(yaw / 180.0F * (float)Math.PI) * 0.16F;
        y -= 0.1F;
        z -= MathHelper.Sin(yaw / 180.0F * (float)Math.PI) * 0.16F;
        setPosition(x, y, z);
        standingEyeHeight = 0.0F;
        velocityX = -MathHelper.Sin(yaw / 180.0F * (float)Math.PI) * MathHelper.Cos(pitch / 180.0F * (float)Math.PI);
        velocityZ = MathHelper.Cos(yaw / 180.0F * (float)Math.PI) * MathHelper.Cos(pitch / 180.0F * (float)Math.PI);
        velocityY = -MathHelper.Sin(pitch / 180.0F * (float)Math.PI);
        setArrowHeading(velocityX, velocityY, velocityZ, 1.5F, 1.0F);
    }

    public void setArrowHeading(double x, double y, double z, float speed, float spread)
    {
        float length = MathHelper.Sqrt(x * x + y * y + z * z);
        x /= length;
        y /= length;
        z /= length;
        x += random.NextGaussian() * 0.0075F * spread;
        y += random.NextGaussian() * 0.0075F * spread;
        z += random.NextGaussian() * 0.0075F * spread;
        x *= speed;
        y *= speed;
        z *= speed;
        velocityX = x;
        velocityY = y;
        velocityZ = z;
        float horizontalSpeed = MathHelper.Sqrt(x * x + z * z);
        prevYaw = yaw = (float)(Math.Atan2(x, z) * 180.0D / (float)Math.PI);
        prevPitch = pitch = (float)(Math.Atan2(y, horizontalSpeed) * 180.0D / (float)Math.PI);
        ticksInGround = 0;
    }

    public override void setVelocityClient(double velocityX, double velocityY, double velocityZ)
    {
        this.velocityX = velocityX;
        this.velocityY = velocityY;
        this.velocityZ = velocityZ;
        if (prevPitch == 0.0F && prevYaw == 0.0F)
        {
            float length = MathHelper.Sqrt(velocityX * velocityX + velocityZ * velocityZ);
            prevYaw = yaw = (float)(Math.Atan2(velocityX, velocityZ) * 180.0D / (float)Math.PI);
            prevPitch = pitch = (float)(Math.Atan2(velocityY, length) * 180.0D / (float)Math.PI);
            prevPitch = pitch;
            prevYaw = yaw;
            setPositionAndAnglesKeepPrevAngles(x, y, z, yaw, pitch);
            ticksInGround = 0;
        }
    }

    public override void tick()
    {
        base.tick();
        if (prevPitch == 0.0F && prevYaw == 0.0F)
        {
            float length = MathHelper.Sqrt(velocityX * velocityX + velocityZ * velocityZ);
            prevYaw = yaw = (float)(Math.Atan2(velocityX, velocityZ) * 180.0D / (float)Math.PI);
            prevPitch = pitch = (float)(Math.Atan2(velocityY, length) * 180.0D / (float)Math.PI);
        }

        int blockId = world.Reader.GetBlockId(xTile, yTile, zTile);
        if (blockId > 0)
        {
            Block.Blocks[blockId].updateBoundingBox(world.Reader, xTile, yTile, zTile);
            Box? box = Block.Blocks[blockId].getCollisionShape(world.Reader, xTile, yTile, zTile);
            if (box != null && box.Value.Contains(new Vec3D(x, y, z)))
            {
                inGround = true;
            }
        }

        if (arrowShake > 0)
        {
            --arrowShake;
        }

        if (inGround)
        {
            blockId = world.Reader.GetBlockId(xTile, yTile, zTile);
            int blockMeta = world.Reader.GetBlockMeta(xTile, yTile, zTile);
            if (blockId == inTile && blockMeta == inData)
            {
                ++ticksInGround;
                if (ticksInGround == 1200)
                {
                    markDead();
                }
            }
            else
            {
                inGround = false;
                velocityX *= random.NextFloat() * 0.2F;
                velocityY *= random.NextFloat() * 0.2F;
                velocityZ *= random.NextFloat() * 0.2F;
                ticksInGround = 0;
                ticksInAir = 0;
            }
        }
        else
        {
            ++ticksInAir;
            Vec3D rayStart = new(x, y, z);
            Vec3D rayEnd = new(x + velocityX, y + velocityY, z + velocityZ);
            HitResult hit = world.Reader.Raycast(rayStart, rayEnd, false, true);
            if (hit.Type != HitResultType.MISS)
            {
                rayEnd = new Vec3D(hit.Pos.x, hit.Pos.y, hit.Pos.z);
            }

            Entity hitEntity = null;
            List<Entity> candidates = world.Entities.GetEntities(this, boundingBox.Stretch(velocityX, velocityY, velocityZ).Expand(1.0D, 1.0D, 1.0D));
            double minHitDistance = 0.0D;

            float expandAmount;
            for (int i = 0; i < candidates.Count; ++i)
            {
                Entity entity = candidates[i];
                if (entity.isCollidable() && (entity != owner || ticksInAir >= 5))
                {
                    expandAmount = 0.3F;
                    Box expandedBox = entity.boundingBox.Expand(expandAmount, expandAmount, expandAmount);
                    HitResult hitResult = expandedBox.Raycast(rayStart, rayEnd);
                    if (hitResult.Type != HitResultType.MISS)
                    {
                        double hitDistance = rayStart.distanceTo(hitResult.Pos);
                        if (hitDistance < minHitDistance || minHitDistance == 0.0D)
                        {
                            hitEntity = entity;
                            minHitDistance = hitDistance;
                        }
                    }
                }
            }

            if (hitEntity != null)
            {
                hit = new HitResult(hitEntity);
            }

            float horizontalSpeed;
            if (hit.Type != HitResultType.MISS)
            {
                if (hit.Entity != null)
                {
                    if (hit.Entity.damage(owner, 4))
                    {
                        world.Broadcaster.PlaySoundAtPos(x, y, z, "random.drr", 1.0F, 1.2F / (random.NextFloat() * 0.2F + 0.9F));
                        markDead();
                    }
                    else
                    {
                        velocityX *= -0.1F;
                        velocityY *= -0.1F;
                        velocityZ *= -0.1F;
                        yaw += 180.0F;
                        prevYaw += 180.0F;
                        ticksInAir = 0;
                    }
                }
                else
                {
                    xTile = hit.BlockX;
                    yTile = hit.BlockY;
                    zTile = hit.BlockZ;
                    inTile = world.Reader.GetBlockId(xTile, yTile, zTile);
                    inData = world.Reader.GetBlockMeta(xTile, yTile, zTile);
                    velocityX = hit.Pos.x - x;
                    velocityY = hit.Pos.y - y;
                    velocityZ = hit.Pos.z - z;
                    horizontalSpeed = MathHelper.Sqrt(velocityX * velocityX + velocityY * velocityY + velocityZ * velocityZ);
                    x -= velocityX / horizontalSpeed * 0.05F;
                    y -= velocityY / horizontalSpeed * 0.05F;
                    z -= velocityZ / horizontalSpeed * 0.05F;
                    world.Broadcaster.PlaySoundAtPos(x, y, z, "random.drr", 1.0F, 1.2F / (random.NextFloat() * 0.2F + 0.9F));
                    inGround = true;
                    arrowShake = 7;
                }
            }

            x += velocityX;
            y += velocityY;
            z += velocityZ;
            horizontalSpeed = MathHelper.Sqrt(velocityX * velocityX + velocityZ * velocityZ);
            yaw = (float)(Math.Atan2(velocityX, velocityZ) * 180.0D / (float)Math.PI);

            for (pitch = (float)(Math.Atan2(velocityY, horizontalSpeed) * 180.0D / (float)Math.PI); pitch - prevPitch < -180.0F; prevPitch -= 360.0F)
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
            expandAmount = 0.03F;
            if (isInWater())
            {
                for (int _ = 0; _ < 4; ++_)
                {
                    float bubbleOffset = 0.25F;
                    world.Broadcaster.AddParticle("bubble", x - velocityX * bubbleOffset, y - velocityY * bubbleOffset, z - velocityZ * bubbleOffset, velocityX, velocityY, velocityZ);
                }

                drag = 0.8F;
            }

            velocityX *= drag;
            velocityY *= drag;
            velocityZ *= drag;
            velocityY -= expandAmount;
            setPosition(x, y, z);
        }
    }

    public override void writeNbt(NBTTagCompound nbt)
    {
        nbt.SetShort("xTile", (short)xTile);
        nbt.SetShort("yTile", (short)yTile);
        nbt.SetShort("zTile", (short)zTile);
        nbt.SetByte("inTile", (sbyte)inTile);
        nbt.SetByte("inData", (sbyte)inData);
        nbt.SetByte("shake", (sbyte)arrowShake);
        nbt.SetByte("inGround", (sbyte)(inGround ? 1 : 0));
        nbt.SetBoolean("player", doesArrowBelongToPlayer);
    }

    public override void readNbt(NBTTagCompound nbt)
    {
        xTile = nbt.GetShort("xTile");
        yTile = nbt.GetShort("yTile");
        zTile = nbt.GetShort("zTile");
        inTile = nbt.GetByte("inTile") & 255;
        inData = nbt.GetByte("inData") & 255;
        arrowShake = nbt.GetByte("shake") & 255;
        inGround = nbt.GetByte("inGround") == 1;
        doesArrowBelongToPlayer = nbt.GetBoolean("player");
    }

    public override void onPlayerInteraction(EntityPlayer player)
    {
        if (!world.IsRemote)
        {
            if (inGround && doesArrowBelongToPlayer && arrowShake <= 0 && player.inventory.addItemStackToInventory(new ItemStack(Item.ARROW, 1)))
            {
                world.Broadcaster.PlaySoundAtPos(x, y, z, "random.pop", 0.2F, ((random.NextFloat() - random.NextFloat()) * 0.7F + 1.0F) * 2.0F);
                player.sendPickup(this, 1);
                markDead();
            }
        }
    }

    public override float getShadowRadius() => 0.0F;
}
