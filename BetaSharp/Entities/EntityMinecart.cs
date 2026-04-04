using BetaSharp.Blocks;
using BetaSharp.Inventorys;
using BetaSharp.Items;
using BetaSharp.NBT;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core.Systems;
using Microsoft.Extensions.Logging;

namespace BetaSharp.Entities;

// TODO: BREAKING MINECART CRASHES THE GAME!!
public class EntityMinecart : Entity, IInventory
{
    public override EntityType Type => EntityRegistry.Minecart;

    private static readonly int[][][] RailShapeVectors =
    [
        [[0, 0, -1], [0, 0, 1]],
        [[-1, 0, 0], [1, 0, 0]],
        [[-1, -1, 0], [1, 0, 0]],
        [[-1, 0, 0], [1, -1, 0]],
        [[0, 0, -1], [0, -1, 1]],
        [[0, -1, -1], [0, 0, 1]],
        [[0, 0, 1], [1, 0, 0]],
        [[0, 0, 1], [-1, 0, 0]],
        [[0, 0, -1], [-1, 0, 0]],
        [[0, 0, -1], [1, 0, 0]]
    ];

    private readonly ILogger<EntityMinecart> _logger = Log.Instance.For<EntityMinecart>();

    private ItemStack[] cargoItems;

    // Kept with original names for external compatibility.
    public int minecartCurrentDamage;
    public int minecartTimeSinceHit;
    public int minecartRockDirection;
    public int type;
    public int fuel;
    public double pushX;
    public double pushZ;

    private bool yawFlipped;

    // Client-side interpolation state.
    private int clientInterpolationSteps;
    private double clientTargetX;
    private double clientTargetY;
    private double clientTargetZ;
    private double clientTargetYaw;
    private double clientTargetPitch;
    private double clientVelocityX;
    private double clientVelocityY;
    private double clientVelocityZ;

    public EntityMinecart(IWorldContext world) : base(world)
    {
        cargoItems = new ItemStack[36];
        minecartCurrentDamage = 0;
        minecartTimeSinceHit = 0;
        minecartRockDirection = 1;
        yawFlipped = false;
        preventEntitySpawning = true;
        setBoundingBoxSpacing(0.98F, 0.7F);
        standingEyeHeight = height / 2.0F;
    }

    public EntityMinecart(IWorldContext world, double x, double y, double z, int type) : this(world)
    {
        setTrackAlignedPosition(x, y, z);
        velocityX = 0.0D;
        velocityY = 0.0D;
        velocityZ = 0.0D;
        prevX = x;
        prevY = y;
        prevZ = z;
        this.type = type;
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
            minecartRockDirection = -minecartRockDirection;
            minecartTimeSinceHit = 10;
            scheduleVelocityUpdate();
            minecartCurrentDamage += amount * 10;

            if (minecartCurrentDamage > 40)
            {
                if (passenger != null)
                {
                    passenger.setVehicle(this);
                }

                markDead();
                dropItem(Item.Minecart.id, 1, 0.0F);

                if (type == 1)
                {
                    EntityMinecart minecart = this;

                    for (int slotIndex = 0; slotIndex < minecart.size(); ++slotIndex)
                    {
                        ItemStack itemStack = minecart.getStack(slotIndex);
                        if (itemStack != null)
                        {
                            float offsetX = random.NextFloat() * 0.8F + 0.1F;
                            float offsetY = random.NextFloat() * 0.8F + 0.1F;
                            float offsetZ = random.NextFloat() * 0.8F + 0.1F;

                            while (itemStack.count > 0)
                            {
                                int dropCount = random.NextInt(21) + 10;
                                if (dropCount > itemStack.count)
                                {
                                    dropCount = itemStack.count;
                                }

                                itemStack.count -= dropCount;

                                EntityItem droppedItem = new EntityItem(
                                    world,
                                    x + offsetX,
                                    y + offsetY,
                                    z + offsetZ,
                                    new ItemStack(itemStack.itemId, dropCount, itemStack.getDamage())
                                );

                                float scatterSpeed = 0.05F;
                                droppedItem.velocityX = (float)random.NextGaussian() * scatterSpeed;
                                droppedItem.velocityY = (float)random.NextGaussian() * scatterSpeed + 0.2F;
                                droppedItem.velocityZ = (float)random.NextGaussian() * scatterSpeed;
                                world.SpawnEntity(droppedItem);
                            }
                        }
                    }

                    dropItem(Block.Chest.id, 1, 0.0F);
                }
                else if (type == 2)
                {
                    dropItem(Block.Furnace.id, 1, 0.0F);
                }
            }

            return true;
        }

        return true;
    }

    public override void animateHurt()
    {
        _logger.LogInformation("Animating hurt");
        minecartRockDirection = -minecartRockDirection;
        minecartTimeSinceHit = 10;
        minecartCurrentDamage += minecartCurrentDamage * 10;
    }

    public override bool isCollidable()
    {
        return !dead;
    }

    public override void markDead()
    {
        for (int slotIndex = 0; slotIndex < size(); ++slotIndex)
        {
            ItemStack itemStack = getStack(slotIndex);
            if (itemStack != null)
            {
                float offsetX = random.NextFloat() * 0.8F + 0.1F;
                float offsetY = random.NextFloat() * 0.8F + 0.1F;
                float offsetZ = random.NextFloat() * 0.8F + 0.1F;

                while (itemStack.count > 0)
                {
                    int dropCount = random.NextInt(21) + 10;
                    if (dropCount > itemStack.count)
                    {
                        dropCount = itemStack.count;
                    }

                    itemStack.count -= dropCount;

                    EntityItem droppedItem = new EntityItem(
                        world,
                        x + offsetX,
                        y + offsetY,
                        z + offsetZ,
                        new ItemStack(itemStack.itemId, dropCount, itemStack.getDamage())
                    );

                    float scatterSpeed = 0.05F;
                    droppedItem.velocityX = (float)random.NextGaussian() * scatterSpeed;
                    droppedItem.velocityY = (float)random.NextGaussian() * scatterSpeed + 0.2F;
                    droppedItem.velocityZ = (float)random.NextGaussian() * scatterSpeed;
                    world.SpawnEntity(droppedItem);
                }
            }
        }

        base.markDead();
    }

    public override void tick()
    {
        if (minecartTimeSinceHit > 0)
        {
            --minecartTimeSinceHit;
        }

        if (minecartCurrentDamage > 0)
        {
            --minecartCurrentDamage;
        }

        if (world.IsRemote && clientInterpolationSteps > 0)
        {
            double interpolatedX = x + (clientTargetX - x) / clientInterpolationSteps;
            double interpolatedY = y + (clientTargetY - y) / clientInterpolationSteps;
            double interpolatedZ = z + (clientTargetZ - z) / clientInterpolationSteps;

            double yawDelta = clientTargetYaw - yaw;
            while (yawDelta < -180.0D)
            {
                yawDelta += 360.0D;
            }

            while (yawDelta >= 180.0D)
            {
                yawDelta -= 360.0D;
            }

            yaw = (float)(yaw + yawDelta / clientInterpolationSteps);
            pitch = (float)(pitch + (clientTargetPitch - pitch) / clientInterpolationSteps);
            --clientInterpolationSteps;

            setPosition(interpolatedX, interpolatedY, interpolatedZ);
            setRotation(yaw, pitch);
            return;
        }

        if (world.IsRemote)
        {
            setPosition(x, y, z);
            setRotation(yaw, pitch);
            return;
        }

        prevX = x;
        prevY = y;
        prevZ = z;

        velocityY -= 0.04D;

        int blockX = MathHelper.Floor(x);
        int blockY = MathHelper.Floor(y);
        int blockZ = MathHelper.Floor(z);

        if (BlockRail.isRail(world, blockX, blockY - 1, blockZ))
        {
            --blockY;
        }

        double maxSpeed = 0.4D;
        bool shouldEmitSmoke = false;
        double slopeAcceleration = 1.0D / 128.0D;

        int railBlockId = world.Reader.GetBlockId(blockX, blockY, blockZ);
        if (BlockRail.isRail(railBlockId))
        {
            Vec3D? previousTrackPosition = getTrackPosition(x, y, z);
            int railMeta = world.Reader.GetBlockMeta(blockX, blockY, blockZ);

            double trackY = blockY;
            bool poweredRailActive = false;
            bool poweredRailBraking = false;

            if (railBlockId == Block.PoweredRail.id)
            {
                poweredRailActive = (railMeta & 8) != 0;
                poweredRailBraking = !poweredRailActive;
            }

            if (((BlockRail)Block.Blocks[railBlockId]).isAlwaysStraight())
            {
                railMeta &= 7;
            }

            if (railMeta >= 2 && railMeta <= 5)
            {
                trackY = blockY + 1;
            }

            if (railMeta == 2)
            {
                velocityX -= slopeAcceleration;
            }

            if (railMeta == 3)
            {
                velocityX += slopeAcceleration;
            }

            if (railMeta == 4)
            {
                velocityZ += slopeAcceleration;
            }

            if (railMeta == 5)
            {
                velocityZ -= slopeAcceleration;
            }

            int[][] railEnds = RailShapeVectors[railMeta];
            double railDirX = railEnds[1][0] - railEnds[0][0];
            double railDirZ = railEnds[1][2] - railEnds[0][2];
            double railDirLength = System.Math.Sqrt(railDirX * railDirX + railDirZ * railDirZ);
            double velocityDotRail = velocityX * railDirX + velocityZ * railDirZ;

            if (velocityDotRail < 0.0D)
            {
                railDirX = -railDirX;
                railDirZ = -railDirZ;
            }

            double horizontalSpeed = System.Math.Sqrt(velocityX * velocityX + velocityZ * velocityZ);
            velocityX = horizontalSpeed * railDirX / railDirLength;
            velocityZ = horizontalSpeed * railDirZ / railDirLength;

            if (poweredRailBraking)
            {
                double brakingSpeed = System.Math.Sqrt(velocityX * velocityX + velocityZ * velocityZ);
                if (brakingSpeed < 0.03D)
                {
                    velocityX = 0.0D;
                    velocityY = 0.0D;
                    velocityZ = 0.0D;
                }
                else
                {
                    velocityX *= 0.5D;
                    velocityY = 0.0D;
                    velocityZ *= 0.5D;
                }
            }

            double positionAlongRail = 0.0D;
            double railStartX = blockX + 0.5D + railEnds[0][0] * 0.5D;
            double railStartZ = blockZ + 0.5D + railEnds[0][2] * 0.5D;
            double railEndX = blockX + 0.5D + railEnds[1][0] * 0.5D;
            double railEndZ = blockZ + 0.5D + railEnds[1][2] * 0.5D;

            railDirX = railEndX - railStartX;
            railDirZ = railEndZ - railStartZ;

            if (railDirX == 0.0D)
            {
                x = blockX + 0.5D;
                positionAlongRail = z - blockZ;
            }
            else if (railDirZ == 0.0D)
            {
                z = blockZ + 0.5D;
                positionAlongRail = x - blockX;
            }
            else
            {
                double offsetFromRailStartX = x - railStartX;
                double offsetFromRailStartZ = z - railStartZ;
                positionAlongRail = (offsetFromRailStartX * railDirX + offsetFromRailStartZ * railDirZ) * 2.0D;
            }

            x = railStartX + railDirX * positionAlongRail;
            z = railStartZ + railDirZ * positionAlongRail;

            setTrackAlignedPosition(x, trackY, z);

            double moveX = velocityX;
            double moveZ = velocityZ;

            if (passenger != null)
            {
                moveX *= 0.75D;
                moveZ *= 0.75D;
            }

            if (moveX < -maxSpeed)
            {
                moveX = -maxSpeed;
            }

            if (moveX > maxSpeed)
            {
                moveX = maxSpeed;
            }

            if (moveZ < -maxSpeed)
            {
                moveZ = -maxSpeed;
            }

            if (moveZ > maxSpeed)
            {
                moveZ = maxSpeed;
            }

            move(moveX, 0.0D, moveZ);

            if (railEnds[0][1] != 0 &&
                MathHelper.Floor(x) - blockX == railEnds[0][0] &&
                MathHelper.Floor(z) - blockZ == railEnds[0][2])
            {
                setTrackAlignedPosition(x, trackY + railEnds[0][1], z);
            }
            else if (railEnds[1][1] != 0 &&
                     MathHelper.Floor(x) - blockX == railEnds[1][0] &&
                     MathHelper.Floor(z) - blockZ == railEnds[1][2])
            {
                setTrackAlignedPosition(x, trackY + railEnds[1][1], z);
            }

            if (passenger != null)
            {
                velocityX *= 0.997F;
                velocityY = 0.0D;
                velocityZ *= 0.997F;
            }
            else
            {
                if (type == 2)
                {
                    double furnacePushMagnitude = MathHelper.Sqrt(pushX * pushX + pushZ * pushZ);
                    if (furnacePushMagnitude > 0.01D)
                    {
                        shouldEmitSmoke = true;
                        pushX /= furnacePushMagnitude;
                        pushZ /= furnacePushMagnitude;

                        double furnaceAcceleration = 0.04D;
                        velocityX *= 0.8F;
                        velocityY = 0.0D;
                        velocityZ *= 0.8F;
                        velocityX += pushX * furnaceAcceleration;
                        velocityZ += pushZ * furnaceAcceleration;
                    }
                    else
                    {
                        velocityX *= 0.9F;
                        velocityY = 0.0D;
                        velocityZ *= 0.9F;
                    }
                }

                velocityX *= 0.96F;
                velocityY = 0.0D;
                velocityZ *= 0.96F;
            }

            Vec3D? currentTrackPosition = getTrackPosition(x, y, z);
            if (currentTrackPosition != null && previousTrackPosition != null)
            {
                double railHeightDeltaForce = (previousTrackPosition.Value.y - currentTrackPosition.Value.y) * 0.05D;
                horizontalSpeed = System.Math.Sqrt(velocityX * velocityX + velocityZ * velocityZ);
                if (horizontalSpeed > 0.0D)
                {
                    velocityX = velocityX / horizontalSpeed * (horizontalSpeed + railHeightDeltaForce);
                    velocityZ = velocityZ / horizontalSpeed * (horizontalSpeed + railHeightDeltaForce);
                }

                // Important fix:
                // Always keep rail snapping on the same Y convention used everywhere else.
                setTrackAlignedPosition(x, currentTrackPosition.Value.y, z);
            }

            int currentBlockX = MathHelper.Floor(x);
            int currentBlockZ = MathHelper.Floor(z);
            if (currentBlockX != blockX || currentBlockZ != blockZ)
            {
                horizontalSpeed = System.Math.Sqrt(velocityX * velocityX + velocityZ * velocityZ);
                velocityX = horizontalSpeed * (currentBlockX - blockX);
                velocityZ = horizontalSpeed * (currentBlockZ - blockZ);
            }

            if (type == 2)
            {
                double pushMagnitude = MathHelper.Sqrt(pushX * pushX + pushZ * pushZ);
                if (pushMagnitude > 0.01D && velocityX * velocityX + velocityZ * velocityZ > 0.001D)
                {
                    pushX /= pushMagnitude;
                    pushZ /= pushMagnitude;

                    if (pushX * velocityX + pushZ * velocityZ < 0.0D)
                    {
                        pushX = 0.0D;
                        pushZ = 0.0D;
                    }
                    else
                    {
                        pushX = velocityX;
                        pushZ = velocityZ;
                    }
                }
            }

            if (poweredRailActive)
            {
                double speedMagnitude = System.Math.Sqrt(velocityX * velocityX + velocityZ * velocityZ);
                if (speedMagnitude > 0.01D)
                {
                    double poweredRailBoost = 0.06D;
                    velocityX += velocityX / speedMagnitude * poweredRailBoost;
                    velocityZ += velocityZ / speedMagnitude * poweredRailBoost;
                }
                else if (railMeta == 1)
                {
                    if (world.Reader.ShouldSuffocate(blockX - 1, blockY, blockZ))
                    {
                        velocityX = 0.02D;
                    }
                    else if (world.Reader.ShouldSuffocate(blockX + 1, blockY, blockZ))
                    {
                        velocityX = -0.02D;
                    }
                }
                else if (railMeta == 0)
                {
                    if (world.Reader.ShouldSuffocate(blockX, blockY, blockZ - 1))
                    {
                        velocityZ = 0.02D;
                    }
                    else if (world.Reader.ShouldSuffocate(blockX, blockY, blockZ + 1))
                    {
                        velocityZ = -0.02D;
                    }
                }
            }
        }
        else
        {
            if (velocityX < -maxSpeed)
            {
                velocityX = -maxSpeed;
            }

            if (velocityX > maxSpeed)
            {
                velocityX = maxSpeed;
            }

            if (velocityZ < -maxSpeed)
            {
                velocityZ = -maxSpeed;
            }

            if (velocityZ > maxSpeed)
            {
                velocityZ = maxSpeed;
            }

            if (onGround)
            {
                velocityX *= 0.5D;
                velocityY *= 0.5D;
                velocityZ *= 0.5D;
            }

            move(velocityX, velocityY, velocityZ);

            if (!onGround)
            {
                velocityX *= 0.95F;
                velocityY *= 0.95F;
                velocityZ *= 0.95F;
            }
        }

        pitch = 0.0F;

        double deltaX = prevX - x;
        double deltaZ = prevZ - z;
        if (deltaX * deltaX + deltaZ * deltaZ > 0.001D)
        {
            yaw = (float)(System.Math.Atan2(deltaZ, deltaX) * 180.0D / System.Math.PI);
            if (yawFlipped)
            {
                yaw += 180.0F;
            }
        }

        double yawChange = yaw - prevYaw;
        while (yawChange >= 180.0D)
        {
            yawChange -= 360.0D;
        }

        while (yawChange < -180.0D)
        {
            yawChange += 360.0D;
        }

        if (yawChange < -170.0D || yawChange >= 170.0D)
        {
            yaw += 180.0F;
            yawFlipped = !yawFlipped;
        }

        setRotation(yaw, pitch);

        var nearbyEntities = world.Entities.GetEntities(this, boundingBox.Expand(0.2D, 0.0D, 0.2D));
        if (nearbyEntities != null && nearbyEntities.Count > 0)
        {
            for (int i = 0; i < nearbyEntities.Count; ++i)
            {
                Entity otherEntity = nearbyEntities[i];
                if (otherEntity != passenger && otherEntity.isPushable() && otherEntity is EntityMinecart)
                {
                    otherEntity.onCollision(this);
                }
            }
        }

        if (passenger != null && passenger.dead)
        {
            passenger = null;
        }

        if (shouldEmitSmoke && random.NextInt(4) == 0)
        {
            --fuel;
            if (fuel < 0)
            {
                pushX = 0.0D;
                pushZ = 0.0D;
            }

            world.Broadcaster.AddParticle("largesmoke", x, y + 0.8D, z, 0.0D, 0.0D, 0.0D);
        }
    }

    private void setTrackAlignedPosition(double x, double trackY, double z)
    {
        setPosition(x, trackY + standingEyeHeight, z);
    }

    public Vec3D? getTrackPositionOffset(double x, double y, double z, double distanceAlongTrack)
    {
        int blockX = MathHelper.Floor(x);
        int blockY = MathHelper.Floor(y);
        int blockZ = MathHelper.Floor(z);

        if (BlockRail.isRail(world, blockX, blockY - 1, blockZ))
        {
            --blockY;
        }

        int blockId = world.Reader.GetBlockId(blockX, blockY, blockZ);
        if (!BlockRail.isRail(blockId))
        {
            return null;
        }

        int railMeta = world.Reader.GetBlockMeta(blockX, blockY, blockZ);
        if (((BlockRail)Block.Blocks[blockId]).isAlwaysStraight())
        {
            railMeta &= 7;
        }

        y = blockY;
        if (railMeta >= 2 && railMeta <= 5)
        {
            y = blockY + 1;
        }

        int[][] railEnds = RailShapeVectors[railMeta];
        double railDirX = railEnds[1][0] - railEnds[0][0];
        double railDirZ = railEnds[1][2] - railEnds[0][2];
        double railDirLength = System.Math.Sqrt(railDirX * railDirX + railDirZ * railDirZ);

        railDirX /= railDirLength;
        railDirZ /= railDirLength;

        x += railDirX * distanceAlongTrack;
        z += railDirZ * distanceAlongTrack;

        if (railEnds[0][1] != 0 &&
            MathHelper.Floor(x) - blockX == railEnds[0][0] &&
            MathHelper.Floor(z) - blockZ == railEnds[0][2])
        {
            y += railEnds[0][1];
        }
        else if (railEnds[1][1] != 0 &&
                 MathHelper.Floor(x) - blockX == railEnds[1][0] &&
                 MathHelper.Floor(z) - blockZ == railEnds[1][2])
        {
            y += railEnds[1][1];
        }

        return getTrackPosition(x, y, z);
    }

    public Vec3D? getTrackPosition(double x, double y, double z)
    {
        int blockX = MathHelper.Floor(x);
        int blockY = MathHelper.Floor(y);
        int blockZ = MathHelper.Floor(z);

        if (BlockRail.isRail(world, blockX, blockY - 1, blockZ))
        {
            --blockY;
        }

        int blockId = world.Reader.GetBlockId(blockX, blockY, blockZ);
        if (!BlockRail.isRail(blockId))
        {
            return null;
        }

        int railMeta = world.Reader.GetBlockMeta(blockX, blockY, blockZ);
        y = blockY;

        if (((BlockRail)Block.Blocks[blockId]).isAlwaysStraight())
        {
            railMeta &= 7;
        }

        if (railMeta >= 2 && railMeta <= 5)
        {
            y = blockY + 1;
        }

        int[][] railEnds = RailShapeVectors[railMeta];

        double railStartX = blockX + 0.5D + railEnds[0][0] * 0.5D;
        double railStartY = blockY + 0.5D + railEnds[0][1] * 0.5D;
        double railStartZ = blockZ + 0.5D + railEnds[0][2] * 0.5D;

        double railEndX = blockX + 0.5D + railEnds[1][0] * 0.5D;
        double railEndY = blockY + 0.5D + railEnds[1][1] * 0.5D;
        double railEndZ = blockZ + 0.5D + railEnds[1][2] * 0.5D;

        double railDirX = railEndX - railStartX;
        double railDirY = (railEndY - railStartY) * 2.0D;
        double railDirZ = railEndZ - railStartZ;

        double positionAlongRail;
        if (railDirX == 0.0D)
        {
            x = blockX + 0.5D;
            positionAlongRail = z - blockZ;
        }
        else if (railDirZ == 0.0D)
        {
            z = blockZ + 0.5D;
            positionAlongRail = x - blockX;
        }
        else
        {
            double offsetX = x - railStartX;
            double offsetZ = z - railStartZ;
            positionAlongRail = (offsetX * railDirX + offsetZ * railDirZ) * 2.0D;
        }

        x = railStartX + railDirX * positionAlongRail;
        y = railStartY + railDirY * positionAlongRail;
        z = railStartZ + railDirZ * positionAlongRail;

        if (railDirY < 0.0D)
        {
            ++y;
        }

        if (railDirY > 0.0D)
        {
            y += 0.5D;
        }

        return new Vec3D(x, y, z);
    }

    // Compatibility wrappers for any external code still calling the old names.
    public Vec3D? func_515_a(double x, double y, double z, double var7)
    {
        return getTrackPositionOffset(x, y, z, var7);
    }

    public Vec3D? func_514_g(double x, double y, double z)
    {
        return getTrackPosition(x, y, z);
    }

    public override void writeNbt(NBTTagCompound nbt)
    {
        nbt.SetInteger("Type", type);

        if (type == 2)
        {
            nbt.SetDouble("PushX", pushX);
            nbt.SetDouble("PushZ", pushZ);
            nbt.SetShort("Fuel", (short)fuel);
        }
        else if (type == 1)
        {
            NBTTagList items = new NBTTagList();

            for (int slotIndex = 0; slotIndex < cargoItems.Length; ++slotIndex)
            {
                if (cargoItems[slotIndex] != null)
                {
                    NBTTagCompound itemTag = new NBTTagCompound();
                    itemTag.SetByte("Slot", (sbyte)slotIndex);
                    cargoItems[slotIndex].writeToNBT(itemTag);
                    items.SetTag(itemTag);
                }
            }

            nbt.SetTag("Items", items);
        }
    }

    public override void readNbt(NBTTagCompound nbt)
    {
        type = nbt.GetInteger("Type");

        if (type == 2)
        {
            pushX = nbt.GetDouble("PushX");
            pushZ = nbt.GetDouble("PushZ");
            fuel = nbt.GetShort("Fuel");
        }
        else if (type == 1)
        {
            NBTTagList items = nbt.GetTagList("Items");
            cargoItems = new ItemStack[size()];

            for (int i = 0; i < items.TagCount(); ++i)
            {
                NBTTagCompound itemTag = (NBTTagCompound)items.TagAt(i);
                int slotIndex = itemTag.GetByte("Slot") & 255;
                if (slotIndex >= 0 && slotIndex < cargoItems.Length)
                {
                    cargoItems[slotIndex] = new ItemStack(itemTag);
                }
            }
        }
    }

    public override float getShadowRadius()
    {
        return 0.0F;
    }

    public override void onCollision(Entity entity)
    {
        if (world.IsRemote)
        {
            return;
        }

        if (entity == passenger)
        {
            return;
        }

        if (entity is EntityLiving &&
            entity is not EntityPlayer &&
            type == 0 &&
            velocityX * velocityX + velocityZ * velocityZ > 0.01D &&
            passenger == null &&
            entity.vehicle == null)
        {
            entity.setVehicle(this);
        }

        double deltaX = entity.x - x;
        double deltaZ = entity.z - z;
        double distanceSq = deltaX * deltaX + deltaZ * deltaZ;

        if (distanceSq < 1.0E-4D)
        {
            return;
        }

        double distance = MathHelper.Sqrt(distanceSq);
        deltaX /= distance;
        deltaZ /= distance;

        double forceScale = 1.0D / distance;
        if (forceScale > 1.0D)
        {
            forceScale = 1.0D;
        }

        deltaX *= forceScale;
        deltaZ *= forceScale;

        deltaX *= 0.1F;
        deltaZ *= 0.1F;

        deltaX *= 1.0F - pushSpeedReduction;
        deltaZ *= 1.0F - pushSpeedReduction;

        deltaX *= 0.5D;
        deltaZ *= 0.5D;

        if (entity is EntityMinecart otherCart)
        {
            double otherDeltaX = entity.x - x;
            double otherDeltaZ = entity.z - z;

            // Preserved gameplay logic, but with readable names.
            double collisionAlignment = otherDeltaX * entity.velocityZ + otherDeltaZ * entity.prevX;
            collisionAlignment *= collisionAlignment;

            if (collisionAlignment > 5.0D)
            {
                return;
            }

            double averageVelocityX = entity.velocityX + velocityX;
            double averageVelocityZ = entity.velocityZ + velocityZ;

            if (otherCart.type == 2 && type != 2)
            {
                velocityX *= 0.2F;
                velocityZ *= 0.2F;
                addVelocity(entity.velocityX - deltaX, 0.0D, entity.velocityZ - deltaZ);
                entity.velocityX *= 0.7F;
                entity.velocityZ *= 0.7F;
            }
            else if (otherCart.type != 2 && type == 2)
            {
                entity.velocityX *= 0.2F;
                entity.velocityZ *= 0.2F;
                entity.addVelocity(velocityX + deltaX, 0.0D, velocityZ + deltaZ);
                velocityX *= 0.7F;
                velocityZ *= 0.7F;
            }
            else
            {
                averageVelocityX /= 2.0D;
                averageVelocityZ /= 2.0D;

                velocityX *= 0.2F;
                velocityZ *= 0.2F;
                addVelocity(averageVelocityX - deltaX, 0.0D, averageVelocityZ - deltaZ);

                entity.velocityX *= 0.2F;
                entity.velocityZ *= 0.2F;
                entity.addVelocity(averageVelocityX + deltaX, 0.0D, averageVelocityZ + deltaZ);
            }
        }
        else
        {
            addVelocity(-deltaX, 0.0D, -deltaZ);
            entity.addVelocity(deltaX / 4.0D, 0.0D, deltaZ / 4.0D);
        }
    }

    public int size()
    {
        return 27;
    }

    public ItemStack getStack(int slotIndex)
    {
        return cargoItems[slotIndex];
    }

    public ItemStack? removeStack(int slotIndex, int amount)
    {
        if (cargoItems[slotIndex] == null)
        {
            return null;
        }

        ItemStack itemStack;
        if (cargoItems[slotIndex].count <= amount)
        {
            itemStack = cargoItems[slotIndex];
            cargoItems[slotIndex] = null;
            return itemStack;
        }

        itemStack = cargoItems[slotIndex].split(amount);
        if (cargoItems[slotIndex].count == 0)
        {
            cargoItems[slotIndex] = null;
        }

        return itemStack;
    }

    public void setStack(int slotIndex, ItemStack? itemStack)
    {
        cargoItems[slotIndex] = itemStack;
        if (itemStack != null && itemStack.count > getMaxCountPerStack())
        {
            itemStack.count = getMaxCountPerStack();
        }
    }

    public string getName()
    {
        return "Minecart";
    }

    public int getMaxCountPerStack()
    {
        return 64;
    }

    public void markDirty()
    {
    }

    public override bool interact(EntityPlayer player)
    {
        if (type == 0)
        {
            if (passenger != null && passenger is EntityPlayer && passenger != player)
            {
                return true;
            }

            if (!world.IsRemote)
            {
                player.setVehicle(this);
            }
        }
        else if (type == 1)
        {
            if (!world.IsRemote)
            {
                player.openChestScreen(this);
            }
        }
        else if (type == 2)
        {
            ItemStack heldItem = player.inventory.GetItemInHand();
            if (heldItem != null && heldItem.itemId == Item.Coal.id)
            {
                if (--heldItem.count == 0)
                {
                    player.inventory.setStack(player.inventory.selectedSlot, (ItemStack)null);
                }

                fuel += 1200;
            }

            pushX = x - player.x;
            pushZ = z - player.z;
        }

        return true;
    }

    public override void setPositionAndAnglesAvoidEntities(double x, double y, double z, float yaw, float pitch, int interpolationSteps)
    {
        clientTargetX = x;
        clientTargetY = y;
        clientTargetZ = z;
        clientTargetYaw = yaw;
        clientTargetPitch = pitch;
        clientInterpolationSteps = interpolationSteps + 2;

        velocityX = clientVelocityX;
        velocityY = clientVelocityY;
        velocityZ = clientVelocityZ;
    }

    public override void setVelocityClient(double velocityX, double velocityY, double velocityZ)
    {
        clientVelocityX = base.velocityX = velocityX;
        clientVelocityY = base.velocityY = velocityY;
        clientVelocityZ = base.velocityZ = velocityZ;
    }

    public bool canPlayerUse(EntityPlayer player)
    {
        return !dead && player.getSquaredDistance(this) <= 64.0D;
    }
}
