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

    public EntityBoat(IWorldContext world) : base(world)
    {
        boatCurrentDamage = 0;
        boatTimeSinceHit = 0;
        boatRockDirection = 1;
        preventEntitySpawning = true;
        setBoundingBoxSpacing(1.5F, 0.6F);
        standingEyeHeight = height / 2.0F;
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

    public EntityBoat(IWorldContext world, double x, double y, double z) : this(world)
    {
        setPosition(x, y + (double)standingEyeHeight, z);
        velocityX = 0.0D;
        velocityY = 0.0D;
        velocityZ = 0.0D;
        prevX = x;
        prevY = y;
        prevZ = z;
    }

    public override double getPassengerRidingHeight()
    {
        return (double)height * 0.0D - (double)0.3F;
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

                int i;
                for (i = 0; i < 3; ++i)
                {
                    dropItem(Block.Planks.id, 1, 0.0F);
                }

                for (i = 0; i < 2; ++i)
                {
                    dropItem(Item.Stick.id, 1, 0.0F);
                }

                markDead();
            }

            return true;
        }
        else
        {
            return true;
        }
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
        this.targetYaw = (double)targetYaw;
        this.targetPitch = (double)targetPitch;
        this.lerpSteps = lerpSteps + 4;
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
        byte var1 = 5;
        double var2 = 0.0D;

        for (int i = 0; i < var1; ++i)
        {
            double var5 = boundingBox.MinY + (boundingBox.MaxY - boundingBox.MinY) * (double)(i + 0) / (double)var1 - 0.125D;
            double var7 = boundingBox.MinY + (boundingBox.MaxY - boundingBox.MinY) * (double)(i + 1) / (double)var1 - 0.125D;
            Box var9 = new Box(boundingBox.MinX, var5, boundingBox.MinZ, boundingBox.MaxX, var7, boundingBox.MaxZ);
            if (world.Reader.IsMaterialInBox(var9, m => m == Material.Water))
            {
                var2 += 1.0D / (double)var1;
            }
        }

        double var6;
        double var8;
        double var10;
        double var21;
        if (world.IsRemote)
        {
            if (lerpSteps > 0)
            {
                var21 = x + (targetX - x) / (double)lerpSteps;
                var6 = y + (targetY - y) / (double)lerpSteps;
                var8 = z + (targetZ - z) / (double)lerpSteps;

                for (var10 = this.targetYaw - (double)yaw; var10 < -180.0D; var10 += 360.0D)
                {
                }

                while (var10 >= 180.0D)
                {
                    var10 -= 360.0D;
                }

                yaw = (float)((double)yaw + var10 / (double)lerpSteps);
                pitch = (float)((double)pitch + (targetPitch - (double)pitch) / (double)lerpSteps);
                --lerpSteps;
                setPosition(var21, var6, var8);
                setRotation(yaw, pitch);
            }
            else
            {
                var21 = x + velocityX;
                var6 = y + velocityY;
                var8 = z + velocityZ;
                setPosition(var21, var6, var8);
                if (onGround)
                {
                    velocityX *= 0.5D;
                    velocityY *= 0.5D;
                    velocityZ *= 0.5D;
                }

                velocityX *= (double)0.99F;
                velocityY *= (double)0.95F;
                velocityZ *= (double)0.99F;
            }

        }
        else
        {
            if (var2 < 1.0D)
            {
                var21 = var2 * 2.0D - 1.0D;
                velocityY += (double)0.04F * var21;
            }
            else
            {
                if (velocityY < 0.0D)
                {
                    velocityY /= 2.0D;
                }

                velocityY += (double)0.007F;
            }

            if (passenger != null)
            {
                velocityX += passenger.velocityX * 0.2D;
                velocityZ += passenger.velocityZ * 0.2D;
            }

            var21 = 0.4D;
            if (velocityX < -var21)
            {
                velocityX = -var21;
            }

            if (velocityX > var21)
            {
                velocityX = var21;
            }

            if (velocityZ < -var21)
            {
                velocityZ = -var21;
            }

            if (velocityZ > var21)
            {
                velocityZ = var21;
            }

            if (onGround)
            {
                velocityX *= 0.5D;
                velocityY *= 0.5D;
                velocityZ *= 0.5D;
            }

            move(velocityX, velocityY, velocityZ);
            var6 = System.Math.Sqrt(velocityX * velocityX + velocityZ * velocityZ);
            if (var6 > 0.15D)
            {
                var8 = System.Math.Cos((double)yaw * System.Math.PI / 180.0D);
                var10 = System.Math.Sin((double)yaw * System.Math.PI / 180.0D);

                for (int var12 = 0; (double)var12 < 1.0D + var6 * 60.0D; ++var12)
                {
                    double randomOffset = (double)(random.NextFloat() * 2.0F - 1.0F);
                    double sideOffset = (double)(random.NextInt(2) * 2 - 1) * 0.7D;
                    double particleX;
                    double particleZ;
                    if (random.NextBoolean())
                    {
                        particleX = x - var8 * randomOffset * 0.8D + var10 * sideOffset;
                        particleZ = z - var10 * randomOffset * 0.8D - var8 * sideOffset;
                        world.Broadcaster.AddParticle("splash", particleX, y - 0.125D, particleZ, velocityX, velocityY, velocityZ);
                    }
                    else
                    {
                        particleX = x + var8 + var10 * randomOffset * 0.7D;
                        particleZ = z + var10 - var8 * randomOffset * 0.7D;
                        world.Broadcaster.AddParticle("splash", particleX, y - 0.125D, particleZ, velocityX, velocityY, velocityZ);
                    }
                }
            }

            if (horizontalCollison && var6 > 0.15D)
            {
                if (!world.IsRemote)
                {
                    markDead();

                    int j;
                    for (j = 0; j < 3; ++j)
                    {
                        dropItem(Block.Planks.id, 1, 0.0F);
                    }

                    for (j = 0; j < 2; ++j)
                    {
                        dropItem(Item.Stick.id, 1, 0.0F);
                    }
                }
            }
            else
            {
                velocityX *= (double)0.99F;
                velocityY *= (double)0.95F;
                velocityZ *= (double)0.99F;
            }

            pitch = 0.0F;
            var8 = (double)yaw;
            var10 = prevX - x;
            double var23 = prevZ - z;
            if (var10 * var10 + var23 * var23 > 0.001D)
            {
                var8 = (double)((float)(System.Math.Atan2(var23, var10) * 180.0D / System.Math.PI));
            }

            double yawDelta;
            for (yawDelta = var8 - (double)yaw; yawDelta >= 180.0D; yawDelta -= 360.0D)
            {
            }

            while (yawDelta < -180.0D)
            {
                yawDelta += 360.0D;
            }

            if (yawDelta > 20.0D)
            {
                yawDelta = 20.0D;
            }

            if (yawDelta < -20.0D)
            {
                yawDelta = -20.0D;
            }

            yaw = (float)((double)yaw + yawDelta);
            setRotation(yaw, pitch);
            var entitiesInbound = world.Entities.GetEntities(this, boundingBox.Expand((double)0.2F, 0.0D, (double)0.2F));
            int i;
            if (entitiesInbound != null && entitiesInbound.Count > 0)
            {
                for (i = 0; i < entitiesInbound.Count; ++i)
                {
                    Entity entity = entitiesInbound[i];
                    if (entity != passenger && entity.isPushable() && entity is EntityBoat)
                    {
                        entity.onCollision(this);
                    }
                }
            }

            for (i = 0; i < 4; ++i)
            {
                int x = MathHelper.Floor(base.x + ((double)(i % 2) - 0.5D) * 0.8D);
                int y = MathHelper.Floor(base.y);
                int z = MathHelper.Floor(base.z + ((double)(i / 2) - 0.5D) * 0.8D);
                if (world.Reader.GetBlockId(x, y, z) == Block.Snow.id)
                {
                    world.Writer.SetBlock(x, y, z, 0);
                }
            }

            if (passenger != null && passenger.dead)
            {
                passenger = null;
            }

        }
    }

    public override void updatePassengerPosition()
    {
        if (passenger != null)
        {
            double xOffset = System.Math.Cos((double)yaw * System.Math.PI / 180.0D) * 0.4D;
            double zOffset = System.Math.Sin((double)yaw * System.Math.PI / 180.0D) * 0.4D;
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
        else
        {
            if (!world.IsRemote)
            {
                player.setVehicle(this);
            }

            return true;
        }
    }
}
