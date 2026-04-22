using BetaSharp.Blocks;
using BetaSharp.Items;
using BetaSharp.NBT;
using BetaSharp.Util.Hit;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Entities;

public class EntityArrow : Entity
{
    public override EntityType Type => EntityRegistry.Arrow;
    private int xTile = -1;
    private int yTile = -1;
    private int zTile = -1;
    private int inTile;
    private int inData;
    private bool inGround;
    public bool doesArrowBelongToPlayer;
    public int arrowShake;
    public EntityLiving owner;
    private int ticksInGround;
    private int ticksInAir;

    public EntityArrow(IWorldContext world) : base(world)
    {
        SetBoundingBoxSpacing(0.5F, 0.5F);
    }

    public EntityArrow(IWorldContext world, double x, double y, double z) : base(world)
    {
        SetBoundingBoxSpacing(0.5F, 0.5F);
        SetPosition(x, y, z);
        StandingEyeHeight = 0.0F;
    }

    public EntityArrow(IWorldContext world, EntityLiving owner) : base(world)
    {
        this.owner = owner;
        doesArrowBelongToPlayer = owner is EntityPlayer;
        SetBoundingBoxSpacing(0.5F, 0.5F);
        SetPositionAndAnglesKeepPrevAngles(owner.X, owner.Y + (double)owner.GetEyeHeight(), owner.Z, owner.Yaw, owner.Pitch);
        X -= (double)(MathHelper.Cos(Yaw / 180.0F * (float)System.Math.PI) * 0.16F);
        Y -= (double)0.1F;
        Z -= (double)(MathHelper.Sin(Yaw / 180.0F * (float)System.Math.PI) * 0.16F);
        SetPosition(X, Y, Z);
        StandingEyeHeight = 0.0F;
        VelocityX = (double)(-MathHelper.Sin(Yaw / 180.0F * (float)System.Math.PI) * MathHelper.Cos(Pitch / 180.0F * (float)System.Math.PI));
        VelocityZ = (double)(MathHelper.Cos(Yaw / 180.0F * (float)System.Math.PI) * MathHelper.Cos(Pitch / 180.0F * (float)System.Math.PI));
        VelocityY = (double)(-MathHelper.Sin(Pitch / 180.0F * (float)System.Math.PI));
        setArrowHeading(VelocityX, VelocityY, VelocityZ, 1.5F, 1.0F);
    }

    public void setArrowHeading(double x, double y, double z, float speed, float spread)
    {
        float length = MathHelper.Sqrt(x * x + y * y + z * z);
        x /= (double)length;
        y /= (double)length;
        z /= (double)length;
        x += Random.NextGaussian() * (double)0.0075F * (double)spread;
        y += Random.NextGaussian() * (double)0.0075F * (double)spread;
        z += Random.NextGaussian() * (double)0.0075F * (double)spread;
        x *= (double)speed;
        y *= (double)speed;
        z *= (double)speed;
        VelocityX = x;
        VelocityY = y;
        VelocityZ = z;
        float horizontalSpeed = MathHelper.Sqrt(x * x + z * z);
        PrevYaw = Yaw = (float)(System.Math.Atan2(x, z) * 180.0D / (double)((float)System.Math.PI));
        PrevPitch = Pitch = (float)(System.Math.Atan2(y, (double)horizontalSpeed) * 180.0D / (double)((float)System.Math.PI));
        ticksInGround = 0;
    }

    public override void SetVelocityClient(double velocityX, double velocityY, double velocityZ)
    {
        base.VelocityX = velocityX;
        base.VelocityY = velocityY;
        base.VelocityZ = velocityZ;
        if (PrevPitch == 0.0F && PrevYaw == 0.0F)
        {
            float length = MathHelper.Sqrt(velocityX * velocityX + velocityZ * velocityZ);
            PrevYaw = Yaw = (float)(System.Math.Atan2(velocityX, velocityZ) * 180.0D / (double)((float)System.Math.PI));
            PrevPitch = Pitch = (float)(System.Math.Atan2(velocityY, (double)length) * 180.0D / (double)((float)System.Math.PI));
            PrevPitch = Pitch;
            PrevYaw = Yaw;
            SetPositionAndAnglesKeepPrevAngles(X, Y, Z, Yaw, Pitch);
            ticksInGround = 0;
        }

    }

    // Arrow inherits from base entity so it needs to set its position properly or it will default to the collide and move up on hit
    public override void SetPositionAndAnglesAvoidEntities(double x, double y, double z, float yaw, float pitch, int steps)
    {
        SetPosition(x, y, z);
        SetRotation(yaw, pitch);
    }

    public override void Tick()
    {
        base.Tick();
        if (PrevPitch == 0.0F && PrevYaw == 0.0F)
        {
            float length = MathHelper.Sqrt(VelocityX * VelocityX + VelocityZ * VelocityZ);
            PrevYaw = Yaw = (float)(System.Math.Atan2(VelocityX, VelocityZ) * 180.0D / (double)((float)System.Math.PI));
            PrevPitch = Pitch = (float)(System.Math.Atan2(VelocityY, (double)length) * 180.0D / (double)((float)System.Math.PI));
        }

        int blockId = World.Reader.GetBlockId(xTile, yTile, zTile);
        if (blockId > 0)
        {
            Block.Blocks[blockId].updateBoundingBox(World.Reader, xTile, yTile, zTile);
            Box? box = Block.Blocks[blockId].getCollisionShape(World.Reader, World.Entities, xTile, yTile, zTile);
            if (box != null && box.Value.Contains(new Vec3D(X, Y, Z)))
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
            blockId = World.Reader.GetBlockId(xTile, yTile, zTile);
            int blockMeta = World.Reader.GetBlockMeta(xTile, yTile, zTile);
            if (blockId == inTile && blockMeta == inData)
            {
                ++ticksInGround;
                if (ticksInGround == 1200)
                {
                    MarkDead();
                }

            }
            else
            {
                inGround = false;
                VelocityX *= (double)(Random.NextFloat() * 0.2F);
                VelocityY *= (double)(Random.NextFloat() * 0.2F);
                VelocityZ *= (double)(Random.NextFloat() * 0.2F);
                ticksInGround = 0;
                ticksInAir = 0;
            }
        }
        else
        {
            ++ticksInAir;
            Vec3D rayStart = new Vec3D(X, Y, Z);
            Vec3D rayEnd = new Vec3D(X + VelocityX, Y + VelocityY, Z + VelocityZ);
            HitResult hit = World.Reader.Raycast(rayStart, rayEnd, false, true);
            if (hit.Type != HitResultType.MISS)
            {
                rayEnd = new Vec3D(hit.Pos.x, hit.Pos.y, hit.Pos.z);
            }

            Entity hitEntity = null;
            List<Entity> candidates = World.Entities.GetEntities(this, BoundingBox.Stretch(VelocityX, VelocityY, VelocityZ).Expand(1.0D, 1.0D, 1.0D));
            double minHitDistance = 0.0D;

            float expandAmount;
            for (int i = 0; i < candidates.Count; ++i)
            {
                Entity entity = candidates[i];
                if (entity.IsCollidable() && (entity != owner || ticksInAir >= 5))
                {
                    expandAmount = 0.3F;
                    Box expandedBox = entity.BoundingBox.Expand((double)expandAmount, (double)expandAmount, (double)expandAmount);
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
                    if (hit.Entity.Damage(owner, 4))
                    {
                        World.Broadcaster.PlaySoundAtEntity(this, "random.drr", 1.0F, 1.2F / (Random.NextFloat() * 0.2F + 0.9F));
                        MarkDead();
                    }
                    else
                    {
                        VelocityX *= (double)-0.1F;
                        VelocityY *= (double)-0.1F;
                        VelocityZ *= (double)-0.1F;
                        Yaw += 180.0F;
                        PrevYaw += 180.0F;
                        ticksInAir = 0;
                    }
                }
                else
                {
                    xTile = hit.BlockX;
                    yTile = hit.BlockY;
                    zTile = hit.BlockZ;
                    inTile = World.Reader.GetBlockId(xTile, yTile, zTile);
                    inData = World.Reader.GetBlockMeta(xTile, yTile, zTile);
                    VelocityX = (double)((float)(hit.Pos.x - X));
                    VelocityY = (double)((float)(hit.Pos.y - Y));
                    VelocityZ = (double)((float)(hit.Pos.z - Z));
                    horizontalSpeed = MathHelper.Sqrt(VelocityX * VelocityX + VelocityY * VelocityY + VelocityZ * VelocityZ);
                    X -= VelocityX / (double)horizontalSpeed * (double)0.05F;
                    Y -= VelocityY / (double)horizontalSpeed * (double)0.05F;
                    Z -= VelocityZ / (double)horizontalSpeed * (double)0.05F;
                    World.Broadcaster.PlaySoundAtEntity(this, "random.drr", 1.0F, 1.2F / (Random.NextFloat() * 0.2F + 0.9F));
                    inGround = true;
                    arrowShake = 7;
                }
            }

            X += VelocityX;
            Y += VelocityY;
            Z += VelocityZ;
            horizontalSpeed = MathHelper.Sqrt(VelocityX * VelocityX + VelocityZ * VelocityZ);
            Yaw = (float)(System.Math.Atan2(VelocityX, VelocityZ) * 180.0D / (double)((float)System.Math.PI));

            for (Pitch = (float)(System.Math.Atan2(VelocityY, (double)horizontalSpeed) * 180.0D / (double)((float)System.Math.PI)); Pitch - PrevPitch < -180.0F; PrevPitch -= 360.0F)
            {
            }

            while (Pitch - PrevPitch >= 180.0F)
            {
                PrevPitch += 360.0F;
            }

            while (Yaw - PrevYaw < -180.0F)
            {
                PrevYaw -= 360.0F;
            }

            while (Yaw - PrevYaw >= 180.0F)
            {
                PrevYaw += 360.0F;
            }

            Pitch = PrevPitch + (Pitch - PrevPitch) * 0.2F;
            Yaw = PrevYaw + (Yaw - PrevYaw) * 0.2F;
            float drag = 0.99F;
            expandAmount = 0.03F;
            if (IsInWater())
            {
                for (int _ = 0; _ < 4; ++_)
                {
                    float bubbleOffset = 0.25F;
                    World.Broadcaster.AddParticle("bubble", X - VelocityX * (double)bubbleOffset, Y - VelocityY * (double)bubbleOffset, Z - VelocityZ * (double)bubbleOffset, VelocityX, VelocityY, VelocityZ);
                }

                drag = 0.8F;
            }

            VelocityX *= (double)drag;
            VelocityY *= (double)drag;
            VelocityZ *= (double)drag;
            VelocityY -= (double)expandAmount;
            SetPosition(X, Y, Z);
        }
    }

    public override void WriteNbt(NBTTagCompound nbt)
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

    public override void ReadNbt(NBTTagCompound nbt)
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

    public override void OnPlayerInteraction(EntityPlayer player)
    {
        if (!World.IsRemote)
        {
            if (inGround && doesArrowBelongToPlayer && arrowShake <= 0 && player.inventory.AddItemStackToInventory(new ItemStack(Item.ARROW, 1)))
            {
                World.Broadcaster.PlaySoundAtEntity(this, "random.pop", 0.2F, ((Random.NextFloat() - Random.NextFloat()) * 0.7F + 1.0F) * 2.0F);
                player.sendPickup(this, 1);
                MarkDead();
            }

        }
    }

    public override float GetShadowRadius()
    {
        return 0.0F;
    }
}
