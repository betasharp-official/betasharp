using BetaSharp.Items;
using BetaSharp.NBT;
using BetaSharp.Util.Hit;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Entities;

public sealed class EntitySnowball : Entity
{
    private const float Gravity = 0.03F;
    private readonly EntityLiving? _thrower;
    private bool _inGround;
    private int _inTile;
    private int _shake;
    private int _ticksInAir;
    private int _ticksInGround;
    private BlockPos _tile = new(-1, -1, -1);

    public EntitySnowball(IWorldContext world) : base(world) => SetBoundingBoxSpacing(0.25F, 0.25F);

    public EntitySnowball(IWorldContext world, EntityLiving owner) : base(world)
    {
        _thrower = owner;
        SetBoundingBoxSpacing(0.25F, 0.25F);
        SetPositionAndAnglesKeepPrevAngles(owner.X, owner.Y + owner.EyeHeight, owner.Z, owner.Yaw, owner.Pitch);
        X -= MathHelper.Cos(Yaw / 180.0F * (float)Math.PI) * 0.16F;
        Y -= 0.1F;
        Z -= MathHelper.Sin(Yaw / 180.0F * (float)Math.PI) * 0.16F;
        SetPosition(X, Y, Z);
        StandingEyeHeight = 0.0F;
        float speed = 0.4F;
        VelocityX = -MathHelper.Sin(Yaw / 180.0F * (float)Math.PI) * MathHelper.Cos(Pitch / 180.0F * (float)Math.PI) * speed;
        VelocityZ = MathHelper.Cos(Yaw / 180.0F * (float)Math.PI) * MathHelper.Cos(Pitch / 180.0F * (float)Math.PI) * speed;
        VelocityY = -MathHelper.Sin(Pitch / 180.0F * (float)Math.PI) * speed;
        SetHeading(VelocityX, VelocityY, VelocityZ, 1.5F, 1.0F);
    }

    public EntitySnowball(IWorldContext world, double x, double y, double z) : base(world)
    {
        _ticksInGround = 0;
        SetBoundingBoxSpacing(0.25F, 0.25F);
        SetPosition(x, y, z);
        StandingEyeHeight = 0.0F;
    }

    public override EntityType Type => EntityRegistry.Snowball;


    protected override bool ShouldRender(double distanceSquared)
    {
        double renderDistance = BoundingBox.AverageEdgeLength * 4.0D;
        renderDistance *= 64.0D;
        return distanceSquared < renderDistance * renderDistance;
    }

    public void SetHeading(double dirX, double dirY, double dirZ, float speed, float spread)
    {
        float length = MathHelper.Sqrt(dirX * dirX + dirY * dirY + dirZ * dirZ);
        dirX /= length;
        dirY /= length;
        dirZ /= length;
        dirX += Random.NextGaussian() * 0.0075F * spread;
        dirY += Random.NextGaussian() * 0.0075F * spread;
        dirZ += Random.NextGaussian() * 0.0075F * spread;
        dirX *= speed;
        dirY *= speed;
        dirZ *= speed;
        VelocityX = dirX;
        VelocityY = dirY;
        VelocityZ = dirZ;
        float horizontalLength = MathHelper.Sqrt(dirX * dirX + dirZ * dirZ);
        PrevYaw = Yaw = (float)(Math.Atan2(dirX, dirZ) * 180.0D / (float)Math.PI);
        PrevPitch = Pitch = (float)(Math.Atan2(dirY, horizontalLength) * 180.0D / (float)Math.PI);
        _ticksInGround = 0;
    }

    public override void SetVelocityClient(double motionX, double motionY, double motionZ)
    {
        VelocityX = motionX;
        VelocityY = motionY;
        VelocityZ = motionZ;
        if (PrevPitch != 0.0F || PrevYaw != 0.0F) return;

        float horizontalLength = MathHelper.Sqrt(motionX * motionX + motionZ * motionZ);
        PrevYaw = Yaw = (float)(Math.Atan2(motionX, motionZ) * 180.0D / (float)Math.PI);
        PrevPitch = Pitch = (float)(Math.Atan2(motionY, horizontalLength) * 180.0D / (float)Math.PI);
    }

    public override void Tick()
    {
        LastTickX = X;
        LastTickY = Y;
        LastTickZ = Z;
        base.Tick();
        if (_shake > 0)
        {
            --_shake;
        }

        if (_inGround)
        {
            int blockId = World.Reader.GetBlockId(_tile.x, _tile.y, _tile.z);
            if (blockId == _inTile)
            {
                ++_ticksInGround;
                if (_ticksInGround == 1200)
                {
                    MarkDead();
                }

                return;
            }

            _inGround = false;
            VelocityX *= Random.NextFloat() * 0.2F;
            VelocityY *= Random.NextFloat() * 0.2F;
            VelocityZ *= Random.NextFloat() * 0.2F;
            _ticksInGround = 0;
            _ticksInAir = 0;
        }
        else
        {
            ++_ticksInAir;
        }

        Vec3D rayStart = new(X, Y, Z);
        Vec3D rayEnd = new(X + VelocityX, Y + VelocityY, Z + VelocityZ);
        HitResult hit = World.Reader.Raycast(rayStart, rayEnd);
        rayStart = new Vec3D(X, Y, Z);
        rayEnd = new Vec3D(X + VelocityX, Y + VelocityY, Z + VelocityZ);
        if (hit.Type != HitResultType.MISS)
        {
            rayEnd = new Vec3D(hit.Pos.x, hit.Pos.y, hit.Pos.z);
        }

        if (!World.IsRemote)
        {
            Entity? hitEntity = null;
            List<Entity> entities = World.Entities.GetEntities(this, BoundingBox.Stretch(VelocityX, VelocityY, VelocityZ).Expand(1.0D, 1.0D, 1.0D));
            double minHitDistance = 0.0D;

            foreach (Entity entity in entities)
            {
                if (!entity.HasCollision || (Equals(entity, _thrower) && _ticksInAir < 5)) continue;

                const float expandAmount = 0.3F;
                Box expandedBox = entity.BoundingBox.Expand(expandAmount, expandAmount, expandAmount);
                HitResult entityHit = expandedBox.Raycast(rayStart, rayEnd);
                if (entityHit.Type == HitResultType.MISS) continue;

                double distance = rayStart.distanceTo(entityHit.Pos);
                if (!(distance < minHitDistance) && minHitDistance != 0.0D) continue;

                hitEntity = entity;
                minHitDistance = distance;
            }

            if (hitEntity != null)
            {
                hit = new HitResult(hitEntity);
            }
        }

        if (hit.Type != HitResultType.MISS)
        {
            if (hit.Entity != null && hit.Entity.Damage(_thrower, 0))
            {
            }

            for (int i = 0; i < 8; ++i)
            {
                World.Broadcaster.AddParticle("snowballpoof", X, Y, Z, 0.0D, 0.0D, 0.0D);
            }

            MarkDead();
        }

        X += VelocityX;
        Y += VelocityY;
        Z += VelocityZ;
        float horizontalSpeed = MathHelper.Sqrt(VelocityX * VelocityX + VelocityZ * VelocityZ);
        Yaw = (float)(Math.Atan2(VelocityX, VelocityZ) * 180.0D / Math.PI);
        Pitch = (float)(Math.Atan2(VelocityY, horizontalSpeed) * 180.0D / Math.PI);

        while (Pitch - PrevPitch < -180.0F)
        {
            PrevPitch -= 360.0F;
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
        if (IsInWater)
        {
            for (int i = 0; i < 4; ++i)
            {
                const float trailOffset = 0.25F;
                World.Broadcaster.AddParticle("bubble", X - VelocityX * trailOffset, Y - VelocityY * trailOffset, Z - VelocityZ * trailOffset, VelocityX, VelocityY, VelocityZ);
            }

            drag = 0.8F;
        }

        VelocityX *= drag;
        VelocityY *= drag;
        VelocityZ *= drag;
        VelocityY -= Gravity;
        SetPosition(X, Y, Z);
    }

    protected override void WriteNbt(NBTTagCompound nbt)
    {
        nbt.SetShort("xTile", (short)_tile.x);
        nbt.SetShort("yTile", (short)_tile.y);
        nbt.SetShort("zTile", (short)_tile.z);
        nbt.SetByte("inTile", (sbyte)_inTile);
        nbt.SetByte("shake", (sbyte)_shake);
        nbt.SetByte("inGround", (sbyte)(_inGround ? 1 : 0));
    }

    protected override void ReadNbt(NBTTagCompound nbt)
    {
        _tile = new BlockPos(nbt.GetShort("xTile"), nbt.GetShort("yTile"), nbt.GetShort("zTile"));
        _inTile = nbt.GetByte("inTile") & 255;
        _shake = nbt.GetByte("shake") & 255;
        _inGround = nbt.GetByte("inGround") == 1;
    }

    public override void OnPlayerInteraction(EntityPlayer player)
    {
        if (!_inGround || !Equals(_thrower, player) || _shake > 0 || !player.Inventory.AddItemStackToInventory(new ItemStack(Item.ARROW, 1)))
        {
            return;
        }

        World.Broadcaster.PlaySoundAtEntity(this, "random.pop", 0.2F, ((Random.NextFloat() - Random.NextFloat()) * 0.7F + 1.0F) * 2.0F);
        player.sendPickup(this, 1);
        MarkDead();
    }

    public override float GetShadowRadius() => 0.0F;
}
