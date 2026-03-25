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
        preventEntitySpawning = true;
        setBoundingBoxSpacing(1.5F, 0.6F);
        standingEyeHeight = height / 2.0F;
    }

    public EntityBoat(IWorldContext world, double x, double y, double z) : this(world)
    {
        setPosition(x, y + standingEyeHeight, z);
        velocityX = 0.0D;
        velocityY = 0.0D;
        velocityZ = 0.0D;
        prevX = x;
        prevY = y;
        prevZ = z;
    }

    protected override bool bypassesSteppingEffects()
    {
        return false;
    }

    public override Box? getCollisionAgainstShape(Entity entity)
    {
        return entity.boundingBox;
    }

    public override Box? getBoundingBox()
    {
        return boundingBox;
    }

    public override bool isPushable()
    {
        return true;
    }

    public override double getPassengerRidingHeight()
    {
        return (double)height * 0.0D - 0.3D;
    }

    public override bool damage(Entity entity, int amount)
    {
        if (!world.IsRemote && !dead)
        {
            boatRockDirection = -boatRockDirection;
            boatTimeSinceHit = 10;
            boatCurrentDamage += amount * 10;
            scheduleVelocityUpdate();

            if (boatCurrentDamage > 40)
            {
                if (passenger != null)
                {
                    passenger.setVehicle(this);
                }

                for (int i = 0; i < 3; ++i)
                {
                    dropItem(Block.Planks.id, 1, 0.0F);
                }

                for (int i = 0; i < 2; ++i)
                {
                    dropItem(Item.Stick.id, 1, 0.0F);
                }

                markDead();
            }

            return true;
        }

        return true;
    }

    public override void animateHurt()
    {
        boatRockDirection = -boatRockDirection;
        boatTimeSinceHit = 10;
        boatCurrentDamage += boatCurrentDamage * 10;
    }

    public override bool isCollidable()
    {
        return !dead;
    }

    public override void setPositionAndAnglesAvoidEntities(double targetX, double targetY, double targetZ, float targetYaw, float targetPitch, int lerpSteps)
    {
        this.targetX = targetX;
        this.targetY = targetY;
        this.targetZ = targetZ;
        this.targetYaw = targetYaw;
        this.targetPitch = targetPitch;
        this.lerpSteps = lerpSteps + 2;
        velocityX = boatVelocityX;
        velocityY = boatVelocityY;
        velocityZ = boatVelocityZ;
    }

    public override void setVelocityClient(double velocityX, double velocityY, double velocityZ)
    {
        boatVelocityX = base.velocityX = velocityX;
        boatVelocityY = base.velocityY = velocityY;
        boatVelocityZ = base.velocityZ = velocityZ;
    }

    public override void tick()
    {
        base.tick();

        if (boatTimeSinceHit > 0)
        {
            --boatTimeSinceHit;
        }

        if (boatCurrentDamage > 0)
        {
            --boatCurrentDamage;
        }

        prevX = x;
        prevY = y;
        prevZ = z;

        const int waterSliceCount = 5;
        double waterSubmersion = 0.0D;

        for (int i = 0; i < waterSliceCount; ++i)
        {
            double sliceMinY = boundingBox.MinY + (boundingBox.MaxY - boundingBox.MinY) * i / waterSliceCount - 0.125D;
            double sliceMaxY = boundingBox.MinY + (boundingBox.MaxY - boundingBox.MinY) * (i + 1) / waterSliceCount - 0.125D;
            Box sliceBox = new Box(boundingBox.MinX, sliceMinY, boundingBox.MinZ, boundingBox.MaxX, sliceMaxY, boundingBox.MaxZ);

            if (world.Reader.IsMaterialInBox(sliceBox, m => m == Material.Water))
            {
                waterSubmersion += 1.0D / waterSliceCount;
            }
        }

        if (world.IsRemote)
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
            double nextX = x + (targetX - x) / lerpSteps;
            double nextY = y + (targetY - y) / lerpSteps;
            double nextZ = z + (targetZ - z) / lerpSteps;

            double yawDelta = WrapDegrees(targetYaw - yaw);
            yaw = (float)(yaw + yawDelta / lerpSteps);
            pitch = (float)(pitch + (targetPitch - pitch) / lerpSteps);

            --lerpSteps;
            setPosition(nextX, nextY, nextZ);
            setRotation(yaw, pitch);
            return;
        }

        double movedX = x + velocityX;
        double movedY = y + velocityY;
        double movedZ = z + velocityZ;
        setPosition(movedX, movedY, movedZ);

        if (onGround)
        {
            velocityX *= 0.5D;
            velocityY *= 0.5D;
            velocityZ *= 0.5D;
        }

        velocityX *= 0.99D;
        velocityY *= 0.95D;
        velocityZ *= 0.99D;

        pitch = 0.0F;
        updateBoatYawFromMotion();
    }

    private void tickServer(double waterSubmersion)
    {
        if (waterSubmersion < 1.0D)
        {
            double buoyancyFactor = waterSubmersion * 2.0D - 1.0D;
            velocityY += 0.04D * buoyancyFactor;
        }
        else
        {
            if (velocityY < 0.0D)
            {
                velocityY /= 2.0D;
            }

            velocityY += 0.007D;
        }

        applyRiderInput();

        if (velocityX < -MaxHorizontalSpeed)
        {
            velocityX = -MaxHorizontalSpeed;
        }

        if (velocityX > MaxHorizontalSpeed)
        {
            velocityX = MaxHorizontalSpeed;
        }

        if (velocityZ < -MaxHorizontalSpeed)
        {
            velocityZ = -MaxHorizontalSpeed;
        }

        if (velocityZ > MaxHorizontalSpeed)
        {
            velocityZ = MaxHorizontalSpeed;
        }

        if (onGround)
        {
            velocityX *= 0.5D;
            velocityY *= 0.5D;
            velocityZ *= 0.5D;
        }

        move(velocityX, velocityY, velocityZ);

        double horizontalSpeed = System.Math.Sqrt(velocityX * velocityX + velocityZ * velocityZ);
        if (horizontalSpeed > 0.15D)
        {
            spawnSplashParticles(horizontalSpeed);
        }

        if (horizontalCollison && horizontalSpeed > 0.15D)
        {
            if (!world.IsRemote)
            {
                markDead();

                for (int i = 0; i < 3; ++i)
                {
                    dropItem(Block.Planks.id, 1, 0.0F);
                }

                for (int i = 0; i < 2; ++i)
                {
                    dropItem(Item.Stick.id, 1, 0.0F);
                }
            }
        }
        else
        {
            velocityX *= 0.99D;
            velocityY *= 0.95D;
            velocityZ *= 0.99D;
        }

        pitch = 0.0F;
        updateBoatYawFromMotion();

        var nearbyEntities = world.Entities.GetEntities(this, boundingBox.Expand(0.2D, 0.0D, 0.2D));
        if (nearbyEntities != null && nearbyEntities.Count > 0)
        {
            for (int i = 0; i < nearbyEntities.Count; ++i)
            {
                Entity entity = nearbyEntities[i];
                if (entity != passenger && entity.isPushable() && entity is EntityBoat)
                {
                    entity.onCollision(this);
                }
            }
        }

        for (int i = 0; i < 4; ++i)
        {
            int snowX = MathHelper.Floor(base.x + ((i % 2) - 0.5D) * 0.8D);
            int snowY = MathHelper.Floor(base.y);
            int snowZ = MathHelper.Floor(base.z + ((i / 2) - 0.5D) * 0.8D);

            if (world.Reader.GetBlockId(snowX, snowY, snowZ) == Block.Snow.id)
            {
                world.Writer.SetBlock(snowX, snowY, snowZ, 0);
            }
        }

        if (passenger != null && passenger.dead)
        {
            passenger = null;
        }
    }

    private void applyRiderInput()
    {
        if (passenger == null)
        {
            return;
        }

        velocityX += passenger.velocityX * RiderInputAcceleration;
        velocityZ += passenger.velocityZ * RiderInputAcceleration;

        double riderInputSpeedSq = passenger.velocityX * passenger.velocityX + passenger.velocityZ * passenger.velocityZ;
        if (riderInputSpeedSq <= 1.0E-4D)
        {
            return;
        }

        double speed = System.Math.Sqrt(velocityX * velocityX + velocityZ * velocityZ);
        if (speed <= 0.01D)
        {
            return;
        }

        double riderInputSpeed = System.Math.Sqrt(riderInputSpeedSq);
        double targetVelocityX = passenger.velocityX / riderInputSpeed * speed;
        double targetVelocityZ = passenger.velocityZ / riderInputSpeed * speed;

        velocityX += (targetVelocityX - velocityX) * RiderTurnVelocityBlend;
        velocityZ += (targetVelocityZ - velocityZ) * RiderTurnVelocityBlend;

        double desiredYaw = System.Math.Atan2(-targetVelocityZ, -targetVelocityX) * 180.0D / System.Math.PI;
        yaw = (float)(yaw + WrapDegrees(desiredYaw - yaw) * YawSmoothing);
    }

    private void updateBoatYawFromMotion()
    {
        double desiredYaw = yaw;
        double motionX = prevX - x;
        double motionZ = prevZ - z;

        if (motionX * motionX + motionZ * motionZ > 0.001D)
        {
            desiredYaw = System.Math.Atan2(motionZ, motionX) * 180.0D / System.Math.PI;
        }

        yaw = (float)(yaw + WrapDegrees(desiredYaw - yaw) * YawSmoothing);
        setRotation(yaw, pitch);
    }

    private void spawnSplashParticles(double horizontalSpeed)
    {
        double yawCos = System.Math.Cos(yaw * System.Math.PI / 180.0D);
        double yawSin = System.Math.Sin(yaw * System.Math.PI / 180.0D);

        for (int i = 0; i < 1.0D + horizontalSpeed * 60.0D; ++i)
        {
            double randomOffset = random.NextFloat() * 2.0F - 1.0F;
            double sideOffset = (random.NextInt(2) * 2 - 1) * 0.7D;

            double particleX;
            double particleZ;

            if (random.NextBoolean())
            {
                particleX = x - yawCos * randomOffset * 0.8D + yawSin * sideOffset;
                particleZ = z - yawSin * randomOffset * 0.8D - yawCos * sideOffset;
            }
            else
            {
                particleX = x + yawCos + yawSin * randomOffset * 0.7D;
                particleZ = z + yawSin - yawCos * randomOffset * 0.7D;
            }

            world.Broadcaster.AddParticle("splash", particleX, y - 0.125D, particleZ, velocityX, velocityY, velocityZ);
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

    public override void updatePassengerPosition()
    {
        if (passenger != null)
        {
            double xOffset = Math.Cos(yaw * Math.PI / 180.0D) * 0.4D;
            double zOffset = Math.Sin(yaw * Math.PI / 180.0D) * 0.4D;
            passenger.setPosition(x + xOffset, y + getPassengerRidingHeight() + passenger.getStandingEyeHeight(), z + zOffset);
        }
    }

    public override void writeNbt(NBTTagCompound nbt)
    {
    }

    public override void readNbt(NBTTagCompound nbt)
    {
    }

    public override float getShadowRadius()
    {
        return 0.0F;
    }

    public override bool interact(EntityPlayer player)
    {
        if (passenger != null && passenger is EntityPlayer && passenger != player)
        {
            return true;
        }

        if (!world.IsRemote)
        {
            player.setVehicle(this);
        }

        return true;
    }
}
