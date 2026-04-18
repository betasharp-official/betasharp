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
        SetBoundingBoxSpacing(1.0F, 1.0F);
    }


    public override bool ShouldRender(double squaredDistanceToCamera)
    {
        double renderDistance = BoundingBox.AverageEdgeLength * 4.0D;
        renderDistance *= 64.0D;
        return squaredDistanceToCamera < renderDistance * renderDistance;
    }

    public EntityFireball(IWorldContext world, double x, double y, double z, double directionX, double directionY, double directionZ) : base(world)
    {
        SetBoundingBoxSpacing(1.0F, 1.0F);
        SetPositionAndAnglesKeepPrevAngles(x, y, z, Yaw, Pitch);
        SetPosition(x, y, z);
        double directionLength = (double)MathHelper.Sqrt(directionX * directionX + directionY * directionY + directionZ * directionZ);
        powerX = directionX / directionLength * 0.1D;
        powerY = directionY / directionLength * 0.1D;
        powerZ = directionZ / directionLength * 0.1D;
    }

    public EntityFireball(IWorldContext world, EntityLiving owner, double accelerationX, double accelerationY, double accelerationZ) : base(world)
    {
        owner = owner;
        SetBoundingBoxSpacing(1.0F, 1.0F);
        SetPositionAndAnglesKeepPrevAngles(owner.X, owner.Y, owner.Z, owner.Yaw, owner.Pitch);
        SetPosition(X, Y, Z);
        StandingEyeHeight = 0.0F;
        VelocityX = VelocityY = VelocityZ = 0.0D;
        accelerationX += Random.NextGaussian() * 0.4D;
        accelerationY += Random.NextGaussian() * 0.4D;
        accelerationZ += Random.NextGaussian() * 0.4D;
        double directionLength = (double)MathHelper.Sqrt(accelerationX * accelerationX + accelerationY * accelerationY + accelerationZ * accelerationZ);
        powerX = accelerationX / directionLength * 0.1D;
        powerY = accelerationY / directionLength * 0.1D;
        powerZ = accelerationZ / directionLength * 0.1D;
    }

    public override void Tick()
    {
        base.Tick();
        FireTicks = 10;
        if (shake > 0)
        {
            --shake;
        }

        if (inGround)
        {
            int inGroundBlockId = World.Reader.GetBlockId(blockX, blockY, blockZ);
            if (inGroundBlockId == blockId)
            {
                ++removalTimer;
                if (removalTimer == 1200)
                {
                    MarkDead();
                }

                return;
            }

            inGround = false;
            VelocityX *= (double)(Random.NextFloat() * 0.2F);
            VelocityY *= (double)(Random.NextFloat() * 0.2F);
            VelocityZ *= (double)(Random.NextFloat() * 0.2F);
            removalTimer = 0;
            inAirTime = 0;
        }
        else
        {
            ++inAirTime;
        }

        Vec3D startPos = new Vec3D(X, Y, Z);
        Vec3D endPos = new Vec3D(X + VelocityX, Y + VelocityY, Z + VelocityZ);
        HitResult hitResult = World.Reader.Raycast(startPos, endPos);
        startPos = new Vec3D(X, Y, Z);
        endPos = new Vec3D(X + VelocityX, Y + VelocityY, Z + VelocityZ);
        if (hitResult.Type != HitResultType.MISS)
        {
            endPos = new Vec3D(hitResult.Pos.x, hitResult.Pos.y, hitResult.Pos.z);
        }

        Entity hitEntity = null;
        var candidateEntities = World.Entities.GetEntities(this, BoundingBox.Stretch(VelocityX, VelocityY, VelocityZ).Expand(1.0D, 1.0D, 1.0D));
        double nearestHitDistance = 0.0D;

        for (int candidateIndex = 0; candidateIndex < candidateEntities.Count; ++candidateIndex)
        {
            Entity candidateEntity = candidateEntities[candidateIndex];
            if (candidateEntity.IsCollidable() && (candidateEntity != owner || inAirTime >= 25))
            {
                float collisionMargin = 0.3F;
                Box candidateBox = candidateEntity.BoundingBox.Expand((double)collisionMargin, (double)collisionMargin, (double)collisionMargin);
                HitResult candidateHit = candidateBox.Raycast(startPos, endPos);
                if (candidateHit.Type != HitResultType.MISS)
                {
                    double hitDistance = startPos.distanceTo(candidateHit.Pos);
                    if (hitDistance < nearestHitDistance || nearestHitDistance == 0.0D)
                    {
                        hitEntity = candidateEntity;
                        nearestHitDistance = hitDistance;
                    }
                }
            }
        }

        if (hitEntity != null)
        {
            hitResult = new HitResult(hitEntity);
        }

        if (hitResult.Type != HitResultType.MISS)
        {
            if (!World.IsRemote)
            {
                if (hitResult.Entity != null && hitResult.Entity.Damage(owner, 0))
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
        Yaw = (float)(System.Math.Atan2(VelocityX, VelocityZ) * 180.0D / (double)((float)Math.PI));

        for (Pitch = (float)(System.Math.Atan2(VelocityY, (double)horizontalSpeed) * 180.0D / (double)((float)Math.PI)); Pitch - PrevPitch < -180.0F; PrevPitch -= 360.0F)
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
        float drag = 0.95F;
        if (IsInWater())
        {
            for (int bubbleIndex = 0; bubbleIndex < 4; ++bubbleIndex)
            {
                float bubbleOffset = 0.25F;
                World.Broadcaster.AddParticle("bubble", X - VelocityX * (double)bubbleOffset, Y - VelocityY * (double)bubbleOffset, Z - VelocityZ * (double)bubbleOffset, VelocityX, VelocityY, VelocityZ);
            }

            drag = 0.8F;
        }

        VelocityX += powerX;
        VelocityY += powerY;
        VelocityZ += powerZ;
        VelocityX *= (double)drag;
        VelocityY *= (double)drag;
        VelocityZ *= (double)drag;
        World.Broadcaster.AddParticle("smoke", X, Y + 0.5D, Z, 0.0D, 0.0D, 0.0D);
        SetPosition(X, Y, Z);
    }

    public override void WriteNbt(NBTTagCompound nbt)
    {
        nbt.SetShort("xTile", (short)blockX);
        nbt.SetShort("yTile", (short)blockY);
        nbt.SetShort("zTile", (short)blockZ);
        nbt.SetByte("inTile", (sbyte)blockId);
        nbt.SetByte("shake", (sbyte)shake);
        nbt.SetByte("inGround", (sbyte)(inGround ? 1 : 0));
    }

    public override void ReadNbt(NBTTagCompound nbt)
    {
        blockX = nbt.GetShort("xTile");
        blockY = nbt.GetShort("yTile");
        blockZ = nbt.GetShort("zTile");
        blockId = nbt.GetByte("inTile") & 255;
        shake = nbt.GetByte("shake") & 255;
        inGround = nbt.GetByte("inGround") == 1;
    }

    public override bool IsCollidable()
    {
        return true;
    }

    public override float GetTargetingMargin()
    {
        return 1.0F;
    }

    public override bool Damage(Entity entity, int amount)
    {
        ScheduleVelocityUpdate();
        if (entity != null)
        {
            Vec3D? lookVector = entity.GetLookVector();
            if (lookVector != null)
            {
                VelocityX = lookVector.Value.x;
                VelocityY = lookVector.Value.y;
                VelocityZ = lookVector.Value.z;
                powerX = VelocityX * 0.1D;
                powerY = VelocityY * 0.1D;
                powerZ = VelocityZ * 0.1D;
            }

            return true;
        }
        else
        {
            return false;
        }
    }

    public override float GetShadowRadius()
    {
        return 0.0F;
    }
}
