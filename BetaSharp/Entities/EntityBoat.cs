using BetaSharp.Blocks;
using BetaSharp.Blocks.Materials;
using BetaSharp.Items;
using BetaSharp.NBT;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Entities;

public sealed class EntityBoat : Entity
{
    private const double MaxHorizontalSpeed = 0.4D;
    private const double RiderInputAcceleration = 0.18D;
    private const double RiderTurnVelocityBlend = 0.25D;
    private const double YawSmoothing = 0.35D;

    public int BoatCurrentDamage;
    public int BoatRockDirection;
    public int BoatTimeSinceHit;
    private double _boatVelocityX;
    private double _boatVelocityY;
    private double _boatVelocityZ;

    private int _lerpSteps;
    private double _targetPitch;
    private double _targetX;
    private double _targetY;
    private double _targetYaw;
    private double _targetZ;

    public EntityBoat(IWorldContext world) : base(world)
    {
        BoatCurrentDamage = 0;
        BoatTimeSinceHit = 0;
        BoatRockDirection = 1;
        PreventEntitySpawning = true;
        SetBoundingBoxSpacing(1.5F, 0.6F);
        StandingEyeHeight = Height / 2.0F;
    }

    public EntityBoat(IWorldContext world, double x, double y, double z) : this(world)
    {
        SetPosition(x, y + StandingEyeHeight, z);
        VelocityX = 0.0D;
        VelocityY = 0.0D;
        VelocityZ = 0.0D;
        PrevX = x;
        PrevY = y;
        PrevZ = z;
    }

    public override EntityType Type => EntityRegistry.Boat;

    protected override double PassengerRidingHeight => Height * 0.0D - 0.3D;

    protected override bool BypassesSteppingEffects() => false;

    public override Box? GetCollisionAgainstShape(Entity entity) => entity.BoundingBox;

    public override Box? GetBoundingBox() => BoundingBox;

    public override bool IsPushable => true;

    public override bool Damage(Entity? entity, int amount)
    {
        if (World.IsRemote || Dead) return true;

        BoatRockDirection = -BoatRockDirection;
        BoatTimeSinceHit = 10;
        BoatCurrentDamage += amount * 10;
        ScheduleVelocityUpdate();

        if (BoatCurrentDamage <= 40) return true;

        Passenger?.SetVehicle(this);

        for (int i = 0; i < 3; ++i)
        {
            DropItem(Block.Planks.id, 1, 0.0F);
        }

        for (int i = 0; i < 2; ++i)
        {
            DropItem(Item.Stick.id, 1, 0.0F);
        }

        MarkDead();

        return true;
    }

    public override void AnimateHurt()
    {
        BoatRockDirection = -BoatRockDirection;
        BoatTimeSinceHit = 10;
        BoatCurrentDamage += BoatCurrentDamage * 10;
    }

    public override bool HasCollision => !Dead;

    public override void SetPositionAndAnglesAvoidEntities(double targetX, double targetY, double targetZ, float targetYaw, float targetPitch, int lerpSteps)
    {
        _targetX = targetX;
        _targetY = targetY;
        _targetZ = targetZ;
        _targetYaw = targetYaw;
        _targetPitch = targetPitch;
        _lerpSteps = lerpSteps + 2;
        VelocityX = _boatVelocityX;
        VelocityY = _boatVelocityY;
        VelocityZ = _boatVelocityZ;
    }

    public override void SetVelocityClient(double velocityX, double velocityY, double velocityZ)
    {
        _boatVelocityX = VelocityX = velocityX;
        _boatVelocityY = VelocityY = velocityY;
        _boatVelocityZ = VelocityZ = velocityZ;
    }

    public override void Tick()
    {
        base.Tick();

        if (BoatTimeSinceHit > 0) --BoatTimeSinceHit;
        if (BoatCurrentDamage > 0) --BoatCurrentDamage;

        PrevX = X;
        PrevY = Y;
        PrevZ = Z;

        const int waterSliceCount = 5;
        double waterSubmersion = 0.0D;

        for (int i = 0; i < waterSliceCount; ++i)
        {
            double sliceMinY = BoundingBox.MinY + (BoundingBox.MaxY - BoundingBox.MinY) * i / waterSliceCount - 0.125D;
            double sliceMaxY = BoundingBox.MinY + (BoundingBox.MaxY - BoundingBox.MinY) * (i + 1) / waterSliceCount - 0.125D;
            Box sliceBox = new(BoundingBox.MinX, sliceMinY, BoundingBox.MinZ, BoundingBox.MaxX, sliceMaxY, BoundingBox.MaxZ);

            if (World.Reader.IsMaterialInBox(sliceBox, m => m == Material.Water))
            {
                waterSubmersion += 1.0D / waterSliceCount;
            }
        }

        if (World.IsRemote) tickClient();
        else tickServer(waterSubmersion);
    }

    private void tickClient()
    {
        if (_lerpSteps > 0)
        {
            double nextX = X + (_targetX - X) / _lerpSteps;
            double nextY = Y + (_targetY - Y) / _lerpSteps;
            double nextZ = Z + (_targetZ - Z) / _lerpSteps;

            double yawDelta = WrapDegrees(_targetYaw - Yaw);
            Yaw = (float)(Yaw + yawDelta / _lerpSteps);
            Pitch = (float)(Pitch + (_targetPitch - Pitch) / _lerpSteps);

            --_lerpSteps;
            SetPosition(nextX, nextY, nextZ);
            SetRotation(Yaw, Pitch);
            return;
        }

        double movedX = X + VelocityX;
        double movedY = Y + VelocityY;
        double movedZ = Z + VelocityZ;
        SetPosition(movedX, movedY, movedZ);

        if (OnGround)
        {
            VelocityX *= 0.5D;
            VelocityY *= 0.5D;
            VelocityZ *= 0.5D;
        }

        VelocityX *= 0.99D;
        VelocityY *= 0.95D;
        VelocityZ *= 0.99D;

        Pitch = 0.0F;
        updateBoatYawFromMotion();
    }

    private void tickServer(double waterSubmersion)
    {
        if (waterSubmersion < 1.0D)
        {
            double buoyancyFactor = waterSubmersion * 2.0D - 1.0D;
            VelocityY += 0.04D * buoyancyFactor;
        }
        else
        {
            if (VelocityY < 0.0D)
            {
                VelocityY /= 2.0D;
            }

            VelocityY += 0.007D;
        }

        applyRiderInput();

        if (VelocityX < -MaxHorizontalSpeed)
        {
            VelocityX = -MaxHorizontalSpeed;
        }

        if (VelocityX > MaxHorizontalSpeed)
        {
            VelocityX = MaxHorizontalSpeed;
        }

        if (VelocityZ < -MaxHorizontalSpeed)
        {
            VelocityZ = -MaxHorizontalSpeed;
        }

        if (VelocityZ > MaxHorizontalSpeed)
        {
            VelocityZ = MaxHorizontalSpeed;
        }

        if (OnGround)
        {
            VelocityX *= 0.5D;
            VelocityY *= 0.5D;
            VelocityZ *= 0.5D;
        }

        Move(VelocityX, VelocityY, VelocityZ);

        double horizontalSpeed = Math.Sqrt(VelocityX * VelocityX + VelocityZ * VelocityZ);
        if (horizontalSpeed > 0.15D)
        {
            spawnSplashParticles(horizontalSpeed);
        }

        if (HorizontalCollision && horizontalSpeed > 0.15D)
        {
            if (!World.IsRemote)
            {
                MarkDead();

                for (int i = 0; i < 3; ++i)
                {
                    DropItem(Block.Planks.id, 1, 0.0F);
                }

                for (int i = 0; i < 2; ++i)
                {
                    DropItem(Item.Stick.id, 1, 0.0F);
                }
            }
        }
        else
        {
            VelocityX *= 0.99D;
            VelocityY *= 0.95D;
            VelocityZ *= 0.99D;
        }

        Pitch = 0.0F;
        updateBoatYawFromMotion();

        List<Entity> nearbyEntities = World.Entities.GetEntities(this, BoundingBox.Expand(0.2D, 0.0D, 0.2D));
        if (nearbyEntities is { Count: > 0 })
        {
            foreach (var entity in nearbyEntities)
            {
                if (!Equals(entity, Passenger) && entity.IsPushable && entity is EntityBoat)
                {
                    entity.OnCollision(this);
                }
            }
        }

        for (int i = 0; i < 4; ++i)
        {
            int snowX = MathHelper.Floor(X + (i % 2 - 0.5D) * 0.8D);
            int snowY = MathHelper.Floor(Y);
            int snowZ = MathHelper.Floor(Z + (i * 0.5f - 0.5D) * 0.8D);

            if (World.Reader.GetBlockId(snowX, snowY, snowZ) == Block.Snow.id)
            {
                World.Writer.SetBlock(snowX, snowY, snowZ, 0);
            }
        }

        if (Passenger is { Dead: true })
        {
            Passenger = null;
        }
    }

    private void applyRiderInput()
    {
        if (Passenger == null) return;

        VelocityX += Passenger.VelocityX * RiderInputAcceleration;
        VelocityZ += Passenger.VelocityZ * RiderInputAcceleration;

        double riderInputSpeedSq = Passenger.VelocityX * Passenger.VelocityX + Passenger.VelocityZ * Passenger.VelocityZ;
        if (riderInputSpeedSq <= 1.0E-4D) return;

        double speed = Math.Sqrt(VelocityX * VelocityX + VelocityZ * VelocityZ);
        if (speed <= 0.01D) return;

        double riderInputSpeed = Math.Sqrt(riderInputSpeedSq);
        double targetVelocityX = Passenger.VelocityX / riderInputSpeed * speed;
        double targetVelocityZ = Passenger.VelocityZ / riderInputSpeed * speed;

        VelocityX += (targetVelocityX - VelocityX) * RiderTurnVelocityBlend;
        VelocityZ += (targetVelocityZ - VelocityZ) * RiderTurnVelocityBlend;

        double desiredYaw = Math.Atan2(-targetVelocityZ, -targetVelocityX) * 180.0D / Math.PI;
        Yaw = (float)(Yaw + WrapDegrees(desiredYaw - Yaw) * YawSmoothing);
    }

    private void updateBoatYawFromMotion()
    {
        double desiredYaw = Yaw;
        double motionX = PrevX - X;
        double motionZ = PrevZ - Z;

        if (motionX * motionX + motionZ * motionZ > 0.001D)
        {
            desiredYaw = Math.Atan2(motionZ, motionX) * 180.0D / Math.PI;
        }

        Yaw = (float)(Yaw + WrapDegrees(desiredYaw - Yaw) * YawSmoothing);
        SetRotation(Yaw, Pitch);
    }

    private void spawnSplashParticles(double horizontalSpeed)
    {
        double yawCos = Math.Cos(Yaw * Math.PI / 180.0D);
        double yawSin = Math.Sin(Yaw * Math.PI / 180.0D);

        for (int i = 0; i < 1.0D + horizontalSpeed * 60.0D; ++i)
        {
            double randomOffset = Random.NextFloat() * 2.0F - 1.0F;
            double sideOffset = (Random.NextInt(2) * 2 - 1) * 0.7D;

            double particleX;
            double particleZ;

            if (Random.NextBoolean())
            {
                particleX = X - yawCos * randomOffset * 0.8D + yawSin * sideOffset;
                particleZ = Z - yawSin * randomOffset * 0.8D - yawCos * sideOffset;
            }
            else
            {
                particleX = X + yawCos + yawSin * randomOffset * 0.7D;
                particleZ = Z + yawSin - yawCos * randomOffset * 0.7D;
            }

            World.Broadcaster.AddParticle("splash", particleX, Y - 0.125D, particleZ, VelocityX, VelocityY, VelocityZ);
        }
    }

    private static double WrapDegrees(double angle)
    {
        while (angle >= 180.0D) angle -= 360.0D;
        while (angle < -180.0D) angle += 360.0D;
        return angle;
    }

    public override void UpdatePassengerPosition()
    {
        if (Passenger == null) return;

        double xOffset = Math.Cos(Yaw * Math.PI / 180.0D) * 0.4D;
        double zOffset = Math.Sin(Yaw * Math.PI / 180.0D) * 0.4D;
        Passenger.SetPosition(X + xOffset, Y + PassengerRidingHeight + Passenger.StandingEyeHeight, Z + zOffset);
    }

    protected override void WriteNbt(NBTTagCompound nbt)
    {
    }

    protected override void ReadNbt(NBTTagCompound nbt)
    {
    }

    public override float GetShadowRadius() => 0.0F;

    public override bool Interact(EntityPlayer player)
    {
        if (Passenger is EntityPlayer && !Equals(Passenger, player)) return true;
        if (!World.IsRemote) player.SetVehicle(this);
        return true;
    }
}
