using BetaSharp.Blocks;
using BetaSharp.Items;
using BetaSharp.NBT;
using BetaSharp.Util.Hit;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Entities;

public class EntityArrow : Entity
{
    private const float BubbleOffset = 0.25F;
    private int _inData;
    private bool _inGround;
    private int _inTile;
    private int _ticksInAir;
    private int _ticksInGround;
    private BlockPos _tile = new(-1, -1, -1);
    public int ArrowShake;
    public bool DoesArrowBelongToPlayer;
    public EntityLiving? Owner;

    public EntityArrow(IWorldContext world) : base(world) => SetBoundingBoxSpacing(0.5F, 0.5F);

    public EntityArrow(IWorldContext world, double x, double y, double z) : base(world)
    {
        SetBoundingBoxSpacing(0.5F, 0.5F);
        SetPosition(x, y, z);
        StandingEyeHeight = 0.0F;
    }

    public EntityArrow(IWorldContext world, EntityLiving owner) : base(world)
    {
        Owner = owner;
        DoesArrowBelongToPlayer = owner is EntityPlayer;
        SetBoundingBoxSpacing(0.5F, 0.5F);
        SetPositionAndAnglesKeepPrevAngles(owner.X, owner.Y + owner.EyeHeight, owner.Z, owner.Yaw, owner.Pitch);
        X -= MathHelper.Cos(Yaw / 180.0F * (float)Math.PI) * 0.16F;
        Y -= 0.1F;
        Z -= MathHelper.Sin(Yaw / 180.0F * (float)Math.PI) * 0.16F;
        SetPosition(X, Y, Z);
        StandingEyeHeight = 0.0F;
        VelocityX = -MathHelper.Sin(Yaw / 180.0F * (float)Math.PI) * MathHelper.Cos(Pitch / 180.0F * (float)Math.PI);
        VelocityZ = MathHelper.Cos(Yaw / 180.0F * (float)Math.PI) * MathHelper.Cos(Pitch / 180.0F * (float)Math.PI);
        VelocityY = -MathHelper.Sin(Pitch / 180.0F * (float)Math.PI);
        SetArrowHeading(VelocityX, VelocityY, VelocityZ, 1.5F, 1.0F);
    }

    public override EntityType Type => EntityRegistry.Arrow;
    protected sealed override void SetBoundingBoxSpacing(float widthOffset, float heightOffset) => base.SetBoundingBoxSpacing(widthOffset, heightOffset);

    public void SetArrowHeading(double x, double y, double z, float speed, float spread)
    {
        float length = MathHelper.Sqrt(x * x + y * y + z * z);
        x /= length;
        y /= length;
        z /= length;
        x += Random.NextGaussian() * 0.0075F * spread;
        y += Random.NextGaussian() * 0.0075F * spread;
        z += Random.NextGaussian() * 0.0075F * spread;
        x *= speed;
        y *= speed;
        z *= speed;
        VelocityX = x;
        VelocityY = y;
        VelocityZ = z;
        float horizontalSpeed = MathHelper.Sqrt(x * x + z * z);
        PrevYaw = Yaw = (float)(Math.Atan2(x, z) * 180.0D / (float)Math.PI);
        PrevPitch = Pitch = (float)(Math.Atan2(y, horizontalSpeed) * 180.0D / (float)Math.PI);
        _ticksInGround = 0;
    }

    public override void SetVelocityClient(double velocityX, double velocityY, double velocityZ)
    {
        VelocityX = velocityX;
        VelocityY = velocityY;
        VelocityZ = velocityZ;
        if (PrevPitch != 0.0F || PrevYaw != 0.0F) return;

        float length = MathHelper.Sqrt(velocityX * velocityX + velocityZ * velocityZ);
        PrevYaw = Yaw = (float)(Math.Atan2(velocityX, velocityZ) * 180.0D / (float)Math.PI);
        PrevPitch = Pitch = (float)(Math.Atan2(velocityY, length) * 180.0D / (float)Math.PI);
        PrevPitch = Pitch;
        PrevYaw = Yaw;
        SetPositionAndAnglesKeepPrevAngles(X, Y, Z, Yaw, Pitch);
        _ticksInGround = 0;
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
            PrevYaw = Yaw = (float)(Math.Atan2(VelocityX, VelocityZ) * 180.0D / (float)Math.PI);
            PrevPitch = Pitch = (float)(Math.Atan2(VelocityY, length) * 180.0D / (float)Math.PI);
        }

        int blockId = World.Reader.GetBlockId(_tile.x, _tile.y, _tile.z);
        if (blockId > 0)
        {
            Block.Blocks[blockId].updateBoundingBox(World.Reader, _tile.x, _tile.y, _tile.z);
            Box? box = Block.Blocks[blockId].getCollisionShape(World.Reader, World.Entities, _tile.x, _tile.y, _tile.z);
            if (box != null && box.Value.Contains(new Vec3D(X, Y, Z)))
            {
                _inGround = true;
            }
        }

        if (ArrowShake > 0)
        {
            --ArrowShake;
        }

        if (_inGround)
        {
            blockId = World.Reader.GetBlockId(_tile.x, _tile.y, _tile.z);
            int blockMeta = World.Reader.GetBlockMeta(_tile.x, _tile.y, _tile.z);
            if (blockId == _inTile && blockMeta == _inData)
            {
                ++_ticksInGround;
                if (_ticksInGround == 1200)
                {
                    MarkDead();
                }
            }
            else
            {
                _inGround = false;
                VelocityX *= Random.NextFloat() * 0.2F;
                VelocityY *= Random.NextFloat() * 0.2F;
                VelocityZ *= Random.NextFloat() * 0.2F;
                _ticksInGround = 0;
                _ticksInAir = 0;
            }
        }
        else
        {
            ++_ticksInAir;
            Vec3D rayStart = new(X, Y, Z);
            Vec3D rayEnd = new(X + VelocityX, Y + VelocityY, Z + VelocityZ);
            HitResult hit = World.Reader.Raycast(rayStart, rayEnd, false, true);
            if (hit.Type != HitResultType.MISS)
            {
                rayEnd = new Vec3D(hit.Pos.x, hit.Pos.y, hit.Pos.z);
            }

            Entity? hitEntity = null;
            List<Entity> candidates = World.Entities.GetEntities(this, BoundingBox.Stretch(VelocityX, VelocityY, VelocityZ).Expand(1.0D, 1.0D, 1.0D));
            double minHitDistance = 0.0D;

            float expandAmount;
            foreach (Entity entity in candidates)
            {
                if (!entity.HasCollision || (Equals(entity, Owner) && _ticksInAir < 5)) continue;

                expandAmount = 0.3F;
                Box expandedBox = entity.BoundingBox.Expand(expandAmount, expandAmount, expandAmount);
                HitResult hitResult = expandedBox.Raycast(rayStart, rayEnd);
                if (hitResult.Type == HitResultType.MISS) continue;

                double hitDistance = rayStart.distanceTo(hitResult.Pos);
                if (!(hitDistance < minHitDistance) && minHitDistance != 0.0D) continue;

                hitEntity = entity;
                minHitDistance = hitDistance;
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
                    if (hit.Entity.Damage(Owner, 4))
                    {
                        World.Broadcaster.PlaySoundAtEntity(this, "random.drr", 1.0F, 1.2F / (Random.NextFloat() * 0.2F + 0.9F));
                        MarkDead();
                    }
                    else
                    {
                        VelocityX *= -0.1F;
                        VelocityY *= -0.1F;
                        VelocityZ *= -0.1F;
                        Yaw += 180.0F;
                        PrevYaw += 180.0F;
                        _ticksInAir = 0;
                    }
                }
                else
                {
                    _tile = new BlockPos(hit.BlockX, hit.BlockY, hit.BlockZ);
                    _inTile = World.Reader.GetBlockId(_tile.x, _tile.y, _tile.z);
                    _inData = World.Reader.GetBlockMeta(_tile.x, _tile.y, _tile.z);
                    VelocityX = (float)(hit.Pos.x - X);
                    VelocityY = (float)(hit.Pos.y - Y);
                    VelocityZ = (float)(hit.Pos.z - Z);
                    horizontalSpeed = MathHelper.Sqrt(VelocityX * VelocityX + VelocityY * VelocityY + VelocityZ * VelocityZ);
                    X -= VelocityX / horizontalSpeed * 0.05F;
                    Y -= VelocityY / horizontalSpeed * 0.05F;
                    Z -= VelocityZ / horizontalSpeed * 0.05F;
                    World.Broadcaster.PlaySoundAtEntity(this, "random.drr", 1.0F, 1.2F / (Random.NextFloat() * 0.2F + 0.9F));
                    _inGround = true;
                    ArrowShake = 7;
                }
            }

            X += VelocityX;
            Y += VelocityY;
            Z += VelocityZ;
            horizontalSpeed = MathHelper.Sqrt(VelocityX * VelocityX + VelocityZ * VelocityZ);
            Yaw = (float)(Math.Atan2(VelocityX, VelocityZ) * 180.0D / (float)Math.PI);

            Pitch = (float)(Math.Atan2(VelocityY, horizontalSpeed) * 180.0D / Math.PI);
            while (Pitch - PrevPitch < -180.0F) PrevPitch -= 360.0F;
            while (Pitch - PrevPitch >= 180.0F) PrevPitch += 360.0F;
            while (Yaw - PrevYaw < -180.0F) PrevYaw -= 360.0F;
            while (Yaw - PrevYaw >= 180.0F) PrevYaw += 360.0F;

            Pitch = PrevPitch + (Pitch - PrevPitch) * 0.2F;
            Yaw = PrevYaw + (Yaw - PrevYaw) * 0.2F;
            float drag = 0.99F;
            expandAmount = 0.03F;

            if (IsInWater)
            {
                for (int _ = 0; _ < 4; ++_)
                {
                    World.Broadcaster.AddParticle("bubble", X - VelocityX * BubbleOffset, Y - VelocityY * BubbleOffset, Z - VelocityZ * BubbleOffset, VelocityX, VelocityY, VelocityZ);
                }

                drag = 0.8F;
            }

            VelocityX *= drag;
            VelocityY *= drag;
            VelocityZ *= drag;
            VelocityY -= expandAmount;
            SetPosition(X, Y, Z);
        }
    }

    protected override void WriteNbt(NBTTagCompound nbt)
    {
        nbt.SetShort("xTile", (short)_tile.x);
        nbt.SetShort("yTile", (short)_tile.y);
        nbt.SetShort("zTile", (short)_tile.z);
        nbt.SetByte("inTile", (sbyte)_inTile);
        nbt.SetByte("inData", (sbyte)_inData);
        nbt.SetByte("shake", (sbyte)ArrowShake);
        nbt.SetByte("inGround", (sbyte)(_inGround ? 1 : 0));
        nbt.SetBoolean("player", DoesArrowBelongToPlayer);
    }

    protected override void ReadNbt(NBTTagCompound nbt)
    {
        _tile = new BlockPos(nbt.GetShort("xTile"), nbt.GetShort("yTile"), nbt.GetShort("zTile"));
        _inTile = nbt.GetByte("inTile") & 255;
        _inData = nbt.GetByte("inData") & 255;
        ArrowShake = nbt.GetByte("shake") & 255;
        _inGround = nbt.GetByte("inGround") == 1;
        DoesArrowBelongToPlayer = nbt.GetBoolean("player");
    }

    public override void OnPlayerInteraction(EntityPlayer player)
    {
        if (World.IsRemote) return;
        if (!_inGround || !DoesArrowBelongToPlayer || ArrowShake > 0 || !player.Inventory.AddItemStackToInventory(new ItemStack(Item.ARROW, 1))) return;

        World.Broadcaster.PlaySoundAtEntity(this, "random.pop", 0.2F, ((Random.NextFloat() - Random.NextFloat()) * 0.7F + 1.0F) * 2.0F);
        player.sendPickup(this, 1);
        MarkDead();
    }

    public override float GetShadowRadius() => 0.0F;
}
