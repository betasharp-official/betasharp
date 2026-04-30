using BetaSharp.NBT;
using BetaSharp.Util.Hit;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Entities;

public sealed class EntityFireball : Entity
{
    private int _blockId;
    private BlockPos _tile = new(-1, -1, -1);
    private int _inAirTime;
    private bool _inGround;
    public readonly EntityLiving? Owner;
    public double PowerX;
    public double PowerY;
    public double PowerZ;
    private int _removalTimer;
    private int _shake;

    public EntityFireball(IWorldContext world) : base(world) => SetBoundingBoxSpacing(1.0F, 1.0F);

    public EntityFireball(IWorldContext world, double x, double y, double z, double directionX, double directionY, double directionZ) : base(world)
    {
        SetBoundingBoxSpacing(1.0F, 1.0F);
        SetPositionAndAnglesKeepPrevAngles(x, y, z, Yaw, Pitch);
        SetPosition(x, y, z);
        double directionLength = (double)MathHelper.Sqrt(directionX * directionX + directionY * directionY + directionZ * directionZ);
        PowerX = directionX / directionLength * 0.1D;
        PowerY = directionY / directionLength * 0.1D;
        PowerZ = directionZ / directionLength * 0.1D;
    }

    public EntityFireball(IWorldContext world, EntityLiving owner, double accelerationX, double accelerationY, double accelerationZ) : base(world)
    {
        Owner = owner;
        SetBoundingBoxSpacing(1.0F, 1.0F);
        SetPositionAndAnglesKeepPrevAngles(owner.X, owner.Y, owner.Z, owner.Yaw, owner.Pitch);
        SetPosition(X, Y, Z);
        StandingEyeHeight = 0.0F;
        VelocityX = VelocityY = VelocityZ = 0.0D;
        accelerationX += Random.NextGaussian() * 0.4D;
        accelerationY += Random.NextGaussian() * 0.4D;
        accelerationZ += Random.NextGaussian() * 0.4D;
        double directionLength = (double)MathHelper.Sqrt(accelerationX * accelerationX + accelerationY * accelerationY + accelerationZ * accelerationZ);
        PowerX = accelerationX / directionLength * 0.1D;
        PowerY = accelerationY / directionLength * 0.1D;
        PowerZ = accelerationZ / directionLength * 0.1D;
    }

    public override EntityType Type => EntityRegistry.Fireball;

    public override float TargetingMargin => 1.0F;


    protected override bool ShouldRender(double squaredDistanceToCamera)
    {
        double renderDistance = BoundingBox.AverageEdgeLength * 4.0D;
        renderDistance *= 64.0D;
        return squaredDistanceToCamera < renderDistance * renderDistance;
    }

    public override void Tick()
    {
        base.Tick();
        FireTicks = 10;
        if (_shake > 0)
        {
            --_shake;
        }

        if (_inGround)
        {
            int inGroundBlockId = World.Reader.GetBlockId(_tile.x, _tile.y, _tile.z);
            if (inGroundBlockId == _blockId)
            {
                ++_removalTimer;
                if (_removalTimer == 1200)
                {
                    MarkDead();
                }

                return;
            }

            _inGround = false;
            VelocityX *= Random.NextFloat() * 0.2F;
            VelocityY *= Random.NextFloat() * 0.2F;
            VelocityZ *= Random.NextFloat() * 0.2F;
            _removalTimer = 0;
            _inAirTime = 0;
        }
        else
        {
            ++_inAirTime;
        }

        Vec3D startPos = new(X, Y, Z);
        Vec3D endPos = new(X + VelocityX, Y + VelocityY, Z + VelocityZ);
        HitResult hitResult = World.Reader.Raycast(startPos, endPos);
        startPos = new Vec3D(X, Y, Z);
        endPos = new Vec3D(X + VelocityX, Y + VelocityY, Z + VelocityZ);
        if (hitResult.Type != HitResultType.MISS)
        {
            endPos = new Vec3D(hitResult.Pos.x, hitResult.Pos.y, hitResult.Pos.z);
        }

        Entity? hitEntity = null;
        var candidateEntities = World.Entities.GetEntities(this, BoundingBox.Stretch(VelocityX, VelocityY, VelocityZ).Expand(1.0D, 1.0D, 1.0D));
        double nearestHitDistance = 0.0D;

        foreach (var candidateEntity in candidateEntities)
        {
            if (!candidateEntity.HasCollision || (Equals(candidateEntity, Owner) && _inAirTime < 25)) continue;

            const float collisionMargin = 0.3F;
            Box candidateBox = candidateEntity.BoundingBox.Expand(collisionMargin, collisionMargin, collisionMargin);
            HitResult candidateHit = candidateBox.Raycast(startPos, endPos);
            if (candidateHit.Type == HitResultType.MISS) continue;

            double hitDistance = startPos.distanceTo(candidateHit.Pos);
            if (!(hitDistance < nearestHitDistance) && nearestHitDistance != 0.0D) continue;

            hitEntity = candidateEntity;
            nearestHitDistance = hitDistance;
        }

        if (hitEntity != null)
        {
            hitResult = new HitResult(hitEntity);
        }

        if (hitResult.Type != HitResultType.MISS)
        {
            if (!World.IsRemote)
            {
                if (hitResult.Entity != null && hitResult.Entity.Damage(Owner, 0))
                {
                }

                World.CreateExplosion(null, X, Y, Z, 1.0F, true);
            }

            MarkDead();
        }

        X += VelocityX;
        Y += VelocityY;
        Z += VelocityZ;
        float horizontalSpeed = MathHelper.Sqrt(VelocityX * VelocityX + VelocityZ * VelocityZ);
        Yaw = (float)(Math.Atan2(VelocityX, VelocityZ) * 180.0D / (float)Math.PI);

        Pitch = (float)(Math.Atan2(VelocityY, horizontalSpeed) * 180.0D / Math.PI);
        while (Pitch - PrevPitch < -180.0F) PrevPitch -= 360.0F;
        while (Pitch - PrevPitch >= 180.0F) PrevPitch += 360.0F;
        while (Yaw - PrevYaw < -180.0F) PrevYaw -= 360.0F;
        while (Yaw - PrevYaw >= 180.0F) PrevYaw += 360.0F;

        Pitch = PrevPitch + (Pitch - PrevPitch) * 0.2F;
        Yaw = PrevYaw + (Yaw - PrevYaw) * 0.2F;
        float drag = 0.95F;
        if (IsInWater)
        {
            for (int bubbleIndex = 0; bubbleIndex < 4; ++bubbleIndex)
            {
                const float bubbleOffset = 0.25F;
                World.Broadcaster.AddParticle("bubble", X - VelocityX * bubbleOffset, Y - VelocityY * bubbleOffset, Z - VelocityZ * bubbleOffset, VelocityX, VelocityY, VelocityZ);
            }

            drag = 0.8F;
        }

        VelocityX += PowerX;
        VelocityY += PowerY;
        VelocityZ += PowerZ;
        VelocityX *= drag;
        VelocityY *= drag;
        VelocityZ *= drag;
        World.Broadcaster.AddParticle("smoke", X, Y + 0.5D, Z, 0.0D, 0.0D, 0.0D);
        SetPosition(X, Y, Z);
    }

    protected override void WriteNbt(NBTTagCompound nbt)
    {
        nbt.SetShort("xTile", (short)_tile.x);
        nbt.SetShort("yTile", (short)_tile.y);
        nbt.SetShort("zTile", (short)_tile.z);
        nbt.SetByte("inTile", (sbyte)_blockId);
        nbt.SetByte("shake", (sbyte)_shake);
        nbt.SetByte("inGround", (sbyte)(_inGround ? 1 : 0));
    }

    protected override void ReadNbt(NBTTagCompound nbt)
    {
        _tile = new BlockPos(nbt.GetShort("xTile"), nbt.GetShort("yTile"), nbt.GetShort("zTile"));
        _blockId = nbt.GetByte("inTile") & 255;
        _shake = nbt.GetByte("shake") & 255;
        _inGround = nbt.GetByte("inGround") == 1;
    }

    public override bool HasCollision => true;

    public override bool Damage(Entity? entity, int amount)
    {
        ScheduleVelocityUpdate();
        if (entity == null)            return false;
        Vec3D? lookVector = entity.LookVector;
        if (lookVector == null)            return true;

        VelocityX = lookVector.Value.x;
        VelocityY = lookVector.Value.y;
        VelocityZ = lookVector.Value.z;

        PowerX = VelocityX * 0.1D;
        PowerY = VelocityY * 0.1D;
        PowerZ = VelocityZ * 0.1D;

        return true;
    }

    public override float GetShadowRadius() => 0.0F;
}
