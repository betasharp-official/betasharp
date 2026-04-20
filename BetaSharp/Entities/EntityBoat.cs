using BetaSharp.Blocks;
using BetaSharp.Blocks.Materials;
using BetaSharp.Items;
using BetaSharp.NBT;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Entities;

public class EntityBoat : Entity
{
    public override EntityType Type => EntityRegistry.Boat;

    public int boatCurrentDamage;
    public int boatTimeSinceHit;
    public int boatRockDirection;

    private int lerpSteps;
    private double targetX;
    private double targetY;
    private double targetZ;
    private double targetYaw;
    private double targetPitch;
    private double boatVelocityX;
    private double boatVelocityY;
    private double boatVelocityZ;

    private const double MaxHorizontalSpeed = 0.4D;
    private const double RiderInputAcceleration = 0.18D;
    private const double RiderTurnVelocityBlend = 0.25D;
    private const double YawSmoothing = 0.35D;

    public EntityBoat(IWorldContext world) : base(world)
    {
        boatCurrentDamage = 0;
        boatTimeSinceHit = 0;
        boatRockDirection = 1;
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

    protected override bool BypassesSteppingEffects()
    {
        return false;
    }

    public override Box? GetCollisionAgainstShape(Entity entity)
    {
        return entity.BoundingBox;
    }

    public override Box? GetBoundingBox()
    {
        return BoundingBox;
    }

    public override bool IsPushable()
    {
        return true;
    }

    public override double GetPassengerRidingHeight()
    {
        return (double)Height * 0.0D - 0.3D;
    }

    public override bool Damage(Entity entity, int amount)
    {
        if (!World.IsRemote && !Dead)
        {
            boatRockDirection = -boatRockDirection;
            boatTimeSinceHit = 10;
            boatCurrentDamage += amount * 10;
            ScheduleVelocityUpdate();

            if (boatCurrentDamage > 40)
            {
                if (Passenger != null)
                {
                    Passenger.SetVehicle(this);
                }

                for (int i = 0; i < 3; ++i)
                {
                    DropItem(Block.Planks.ID, 1, 0.0F);
                }

                for (int i = 0; i < 2; ++i)
                {
                    DropItem(Item.Stick.id, 1, 0.0F);
                }

                MarkDead();
            }

            return true;
        }

        return true;
    }

    public override void AnimateHurt()
    {
        boatRockDirection = -boatRockDirection;
        boatTimeSinceHit = 10;
        boatCurrentDamage += boatCurrentDamage * 10;
    }

    public override bool IsCollidable()
    {
        return !Dead;
    }

    public override void SetPositionAndAnglesAvoidEntities(double targetX, double targetY, double targetZ, float targetYaw, float targetPitch, int lerpSteps)
    {
        this.targetX = targetX;
        this.targetY = targetY;
        this.targetZ = targetZ;
        this.targetYaw = targetYaw;
        this.targetPitch = targetPitch;
        this.lerpSteps = lerpSteps + 2;
        VelocityX = boatVelocityX;
        VelocityY = boatVelocityY;
        VelocityZ = boatVelocityZ;
    }

    public override void SetVelocityClient(double velocityX, double velocityY, double velocityZ)
    {
        boatVelocityX = base.VelocityX = velocityX;
        boatVelocityY = base.VelocityY = velocityY;
        boatVelocityZ = base.VelocityZ = velocityZ;
    }

    public override void Tick()
    {
        base.Tick();

        if (boatTimeSinceHit > 0)
        {
            --boatTimeSinceHit;
        }

        if (boatCurrentDamage > 0)
        {
            --boatCurrentDamage;
        }

        PrevX = X;
        PrevY = Y;
        PrevZ = Z;

        const int waterSliceCount = 5;
        double waterSubmersion = 0.0D;

        for (int i = 0; i < waterSliceCount; ++i)
        {
            double sliceMinY = BoundingBox.MinY + (BoundingBox.MaxY - BoundingBox.MinY) * i / waterSliceCount - 0.125D;
            double sliceMaxY = BoundingBox.MinY + (BoundingBox.MaxY - BoundingBox.MinY) * (i + 1) / waterSliceCount - 0.125D;
            Box sliceBox = new Box(BoundingBox.MinX, sliceMinY, BoundingBox.MinZ, BoundingBox.MaxX, sliceMaxY, BoundingBox.MaxZ);

            if (World.Reader.IsMaterialInBox(sliceBox, m => m == Material.Water))
            {
                waterSubmersion += 1.0D / waterSliceCount;
            }
        }

        if (World.IsRemote)
        {
            tickClient();
        }
        else
        {
            tickServer(waterSubmersion);
        }
    }

    private void tickClient()
    {
        if (lerpSteps > 0)
        {
            double nextX = X + (targetX - X) / lerpSteps;
            double nextY = Y + (targetY - Y) / lerpSteps;
            double nextZ = Z + (targetZ - Z) / lerpSteps;

            double yawDelta = WrapDegrees(targetYaw - Yaw);
            Yaw = (float)(Yaw + yawDelta / lerpSteps);
            Pitch = (float)(Pitch + (targetPitch - Pitch) / lerpSteps);

            --lerpSteps;
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

        double horizontalSpeed = System.Math.Sqrt(VelocityX * VelocityX + VelocityZ * VelocityZ);
        if (horizontalSpeed > 0.15D)
        {
            spawnSplashParticles(horizontalSpeed);
        }

        if (HorizontalCollison && horizontalSpeed > 0.15D)
        {
            if (!World.IsRemote)
            {
                MarkDead();

                for (int i = 0; i < 3; ++i)
                {
                    DropItem(Block.Planks.ID, 1, 0.0F);
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

        var nearbyEntities = World.Entities.GetEntities(this, BoundingBox.Expand(0.2D, 0.0D, 0.2D));
        if (nearbyEntities != null && nearbyEntities.Count > 0)
        {
            for (int i = 0; i < nearbyEntities.Count; ++i)
            {
                Entity entity = nearbyEntities[i];
                if (entity != Passenger && entity.IsPushable() && entity is EntityBoat)
                {
                    entity.OnCollision(this);
                }
            }
        }

        for (int i = 0; i < 4; ++i)
        {
            int snowX = MathHelper.Floor(base.X + ((i % 2) - 0.5D) * 0.8D);
            int snowY = MathHelper.Floor(base.Y);
            int snowZ = MathHelper.Floor(base.Z + ((i / 2) - 0.5D) * 0.8D);

            if (World.Reader.GetBlockId(snowX, snowY, snowZ) == Block.Snow.ID)
            {
                World.Writer.SetBlock(snowX, snowY, snowZ, 0);
            }
        }

        if (Passenger != null && Passenger.Dead)
        {
            Passenger = null;
        }
    }

    private void applyRiderInput()
    {
        if (Passenger == null)
        {
            return;
        }

        VelocityX += Passenger.VelocityX * RiderInputAcceleration;
        VelocityZ += Passenger.VelocityZ * RiderInputAcceleration;

        double riderInputSpeedSq = Passenger.VelocityX * Passenger.VelocityX + Passenger.VelocityZ * Passenger.VelocityZ;
        if (riderInputSpeedSq <= 1.0E-4D)
        {
            return;
        }

        double speed = System.Math.Sqrt(VelocityX * VelocityX + VelocityZ * VelocityZ);
        if (speed <= 0.01D)
        {
            return;
        }

        double riderInputSpeed = System.Math.Sqrt(riderInputSpeedSq);
        double targetVelocityX = Passenger.VelocityX / riderInputSpeed * speed;
        double targetVelocityZ = Passenger.VelocityZ / riderInputSpeed * speed;

        VelocityX += (targetVelocityX - VelocityX) * RiderTurnVelocityBlend;
        VelocityZ += (targetVelocityZ - VelocityZ) * RiderTurnVelocityBlend;

        double desiredYaw = System.Math.Atan2(-targetVelocityZ, -targetVelocityX) * 180.0D / System.Math.PI;
        Yaw = (float)(Yaw + WrapDegrees(desiredYaw - Yaw) * YawSmoothing);
    }

    private void updateBoatYawFromMotion()
    {
        double desiredYaw = Yaw;
        double motionX = PrevX - X;
        double motionZ = PrevZ - Z;

        if (motionX * motionX + motionZ * motionZ > 0.001D)
        {
            desiredYaw = System.Math.Atan2(motionZ, motionX) * 180.0D / System.Math.PI;
        }

        Yaw = (float)(Yaw + WrapDegrees(desiredYaw - Yaw) * YawSmoothing);
        SetRotation(Yaw, Pitch);
    }

    private void spawnSplashParticles(double horizontalSpeed)
    {
        double yawCos = System.Math.Cos(Yaw * System.Math.PI / 180.0D);
        double yawSin = System.Math.Sin(Yaw * System.Math.PI / 180.0D);

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
        while (angle >= 180.0D)
        {
            angle -= 360.0D;
        }

        while (angle < -180.0D)
        {
            angle += 360.0D;
        }

        return angle;
    }

    public override void UpdatePassengerPosition()
    {
        if (Passenger != null)
        {
            double xOffset = Math.Cos(Yaw * Math.PI / 180.0D) * 0.4D;
            double zOffset = Math.Sin(Yaw * Math.PI / 180.0D) * 0.4D;
            Passenger.SetPosition(X + xOffset, Y + GetPassengerRidingHeight() + Passenger.GetStandingEyeHeight(), Z + zOffset);
        }
    }

    public override void WriteNbt(NBTTagCompound nbt)
    {
    }

    public override void ReadNbt(NBTTagCompound nbt)
    {
    }

    public override float GetShadowRadius()
    {
        return 0.0F;
    }

    public override bool Interact(EntityPlayer player)
    {
        if (Passenger != null && Passenger is EntityPlayer && Passenger != player)
        {
            return true;
        }

        if (!World.IsRemote)
        {
            player.SetVehicle(this);
        }

        return true;
    }
}
