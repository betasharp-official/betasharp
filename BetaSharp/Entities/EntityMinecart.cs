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

    private ItemStack?[] _cargoItems;

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
        _cargoItems = new ItemStack[36];
        minecartCurrentDamage = 0;
        minecartTimeSinceHit = 0;
        minecartRockDirection = 1;
        yawFlipped = false;
        PreventEntitySpawning = true;
        SetBoundingBoxSpacing(0.98F, 0.7F);
        StandingEyeHeight = Height / 2.0F;
    }

    public EntityMinecart(IWorldContext world, double x, double y, double z, int type) : this(world)
    {
        setTrackAlignedPosition(x, y, z);
        VelocityX = 0.0D;
        VelocityY = 0.0D;
        VelocityZ = 0.0D;
        PrevX = x;
        PrevY = y;
        PrevZ = z;
        this.type = type;
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
            minecartRockDirection = -minecartRockDirection;
            minecartTimeSinceHit = 10;
            ScheduleVelocityUpdate();
            minecartCurrentDamage += amount * 10;

            if (minecartCurrentDamage > 40)
            {
                Passenger?.SetVehicle(this);

                MarkDead();
                DropItem(Item.Minecart.id, 1, 0.0F);

                if (type == 1)
                {
                    EntityMinecart minecart = this;

                    for (int slotIndex = 0; slotIndex < minecart.Size; ++slotIndex)
                    {
                        ItemStack? itemStack = minecart.GetStack(slotIndex);
                        if (itemStack != null)
                        {
                            float offsetX = Random.NextFloat() * 0.8F + 0.1F;
                            float offsetY = Random.NextFloat() * 0.8F + 0.1F;
                            float offsetZ = Random.NextFloat() * 0.8F + 0.1F;

                            while (itemStack.Count > 0)
                            {
                                int dropCount = Random.NextInt(21) + 10;
                                if (dropCount > itemStack.Count)
                                {
                                    dropCount = itemStack.Count;
                                }

                                itemStack.Count -= dropCount;

                                EntityItem droppedItem = new(
                                    World,
                                    X + offsetX,
                                    Y + offsetY,
                                    Z + offsetZ,
                                    new ItemStack(itemStack.ItemId, dropCount, itemStack.getDamage())
                                );

                                float scatterSpeed = 0.05F;
                                droppedItem.VelocityX = (float)Random.NextGaussian() * scatterSpeed;
                                droppedItem.VelocityY = (float)Random.NextGaussian() * scatterSpeed + 0.2F;
                                droppedItem.VelocityZ = (float)Random.NextGaussian() * scatterSpeed;
                                World.SpawnEntity(droppedItem);
                            }
                        }
                    }

                    DropItem(Block.Chest.ID, 1, 0.0F);
                }
                else if (type == 2)
                {
                    DropItem(Block.Furnace.ID, 1, 0.0F);
                }
            }

            return true;
        }

        return true;
    }

    public override void AnimateHurt()
    {
        _logger.LogInformation("Animating hurt");
        minecartRockDirection = -minecartRockDirection;
        minecartTimeSinceHit = 10;
        minecartCurrentDamage += minecartCurrentDamage * 10;
    }

    public override bool IsCollidable()
    {
        return !Dead;
    }

    public override void MarkDead()
    {
        for (int slotIndex = 0; slotIndex < Size; ++slotIndex)
        {
            ItemStack? itemStack = GetStack(slotIndex);
            if (itemStack != null)
            {
                float offsetX = Random.NextFloat() * 0.8F + 0.1F;
                float offsetY = Random.NextFloat() * 0.8F + 0.1F;
                float offsetZ = Random.NextFloat() * 0.8F + 0.1F;

                while (itemStack.Count > 0)
                {
                    int dropCount = Random.NextInt(21) + 10;
                    if (dropCount > itemStack.Count)
                    {
                        dropCount = itemStack.Count;
                    }

                    itemStack.Count -= dropCount;

                    EntityItem droppedItem = new(
                        World,
                        X + offsetX,
                        Y + offsetY,
                        Z + offsetZ,
                        new ItemStack(itemStack.ItemId, dropCount, itemStack.getDamage())
                    );

                    float scatterSpeed = 0.05F;
                    droppedItem.VelocityX = (float)Random.NextGaussian() * scatterSpeed;
                    droppedItem.VelocityY = (float)Random.NextGaussian() * scatterSpeed + 0.2F;
                    droppedItem.VelocityZ = (float)Random.NextGaussian() * scatterSpeed;
                    World.SpawnEntity(droppedItem);
                }
            }
        }

        base.MarkDead();
    }

    public override void Tick()
    {
        if (minecartTimeSinceHit > 0)
        {
            --minecartTimeSinceHit;
        }

        if (minecartCurrentDamage > 0)
        {
            --minecartCurrentDamage;
        }

        if (World.IsRemote && clientInterpolationSteps > 0)
        {
            double interpolatedX = X + (clientTargetX - X) / clientInterpolationSteps;
            double interpolatedY = Y + (clientTargetY - Y) / clientInterpolationSteps;
            double interpolatedZ = Z + (clientTargetZ - Z) / clientInterpolationSteps;

            double yawDelta = clientTargetYaw - Yaw;
            while (yawDelta < -180.0D)
            {
                yawDelta += 360.0D;
            }

            while (yawDelta >= 180.0D)
            {
                yawDelta -= 360.0D;
            }

            Yaw = (float)(Yaw + yawDelta / clientInterpolationSteps);
            Pitch = (float)(Pitch + (clientTargetPitch - Pitch) / clientInterpolationSteps);
            --clientInterpolationSteps;

            SetPosition(interpolatedX, interpolatedY, interpolatedZ);
            SetRotation(Yaw, Pitch);
            return;
        }

        if (World.IsRemote)
        {
            SetPosition(X, Y, Z);
            SetRotation(Yaw, Pitch);
            return;
        }

        PrevX = X;
        PrevY = Y;
        PrevZ = Z;

        VelocityY -= 0.04D;

        int blockX = MathHelper.Floor(X);
        int blockY = MathHelper.Floor(Y);
        int blockZ = MathHelper.Floor(Z);

        if (BlockRail.IsRail(World, blockX, blockY - 1, blockZ))
        {
            --blockY;
        }

        double maxSpeed = 0.4D;
        bool shouldEmitSmoke = false;
        double slopeAcceleration = 1.0D / 128.0D;

        int railBlockId = World.Reader.GetBlockId(blockX, blockY, blockZ);
        if (BlockRail.IsRail(railBlockId))
        {
            Vec3D? previousTrackPosition = getTrackPosition(X, Y, Z);
            int railMeta = World.Reader.GetBlockMeta(blockX, blockY, blockZ);

            double trackY = blockY;
            bool poweredRailActive = false;
            bool poweredRailBraking = false;

            if (railBlockId == Block.PoweredRail.ID)
            {
                poweredRailActive = (railMeta & 8) != 0;
                poweredRailBraking = !poweredRailActive;
            }

            if (((BlockRail)Block.Blocks[railBlockId]).IsAlwaysStraight())
            {
                railMeta &= 7;
            }

            if (railMeta >= 2 && railMeta <= 5)
            {
                trackY = blockY + 1;
            }

            if (railMeta == 2)
            {
                VelocityX -= slopeAcceleration;
            }

            if (railMeta == 3)
            {
                VelocityX += slopeAcceleration;
            }

            if (railMeta == 4)
            {
                VelocityZ += slopeAcceleration;
            }

            if (railMeta == 5)
            {
                VelocityZ -= slopeAcceleration;
            }

            int[][] railEnds = RailShapeVectors[railMeta];
            double railDirX = railEnds[1][0] - railEnds[0][0];
            double railDirZ = railEnds[1][2] - railEnds[0][2];
            double railDirLength = System.Math.Sqrt(railDirX * railDirX + railDirZ * railDirZ);
            double velocityDotRail = VelocityX * railDirX + VelocityZ * railDirZ;

            if (velocityDotRail < 0.0D)
            {
                railDirX = -railDirX;
                railDirZ = -railDirZ;
            }

            double horizontalSpeed = System.Math.Sqrt(VelocityX * VelocityX + VelocityZ * VelocityZ);
            VelocityX = horizontalSpeed * railDirX / railDirLength;
            VelocityZ = horizontalSpeed * railDirZ / railDirLength;

            if (poweredRailBraking)
            {
                double brakingSpeed = System.Math.Sqrt(VelocityX * VelocityX + VelocityZ * VelocityZ);
                if (brakingSpeed < 0.03D)
                {
                    VelocityX = 0.0D;
                    VelocityY = 0.0D;
                    VelocityZ = 0.0D;
                }
                else
                {
                    VelocityX *= 0.5D;
                    VelocityY = 0.0D;
                    VelocityZ *= 0.5D;
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
                X = blockX + 0.5D;
                positionAlongRail = Z - blockZ;
            }
            else if (railDirZ == 0.0D)
            {
                Z = blockZ + 0.5D;
                positionAlongRail = X - blockX;
            }
            else
            {
                double offsetFromRailStartX = X - railStartX;
                double offsetFromRailStartZ = Z - railStartZ;
                positionAlongRail = (offsetFromRailStartX * railDirX + offsetFromRailStartZ * railDirZ) * 2.0D;
            }

            X = railStartX + railDirX * positionAlongRail;
            Z = railStartZ + railDirZ * positionAlongRail;

            setTrackAlignedPosition(X, trackY, Z);

            double moveX = VelocityX;
            double moveZ = VelocityZ;

            if (Passenger != null)
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

            Move(moveX, 0.0D, moveZ);

            if (railEnds[0][1] != 0 &&
                MathHelper.Floor(X) - blockX == railEnds[0][0] &&
                MathHelper.Floor(Z) - blockZ == railEnds[0][2])
            {
                setTrackAlignedPosition(X, trackY + railEnds[0][1], Z);
            }
            else if (railEnds[1][1] != 0 &&
                     MathHelper.Floor(X) - blockX == railEnds[1][0] &&
                     MathHelper.Floor(Z) - blockZ == railEnds[1][2])
            {
                setTrackAlignedPosition(X, trackY + railEnds[1][1], Z);
            }

            if (Passenger != null)
            {
                VelocityX *= 0.997F;
                VelocityY = 0.0D;
                VelocityZ *= 0.997F;
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
                        VelocityX *= 0.8F;
                        VelocityY = 0.0D;
                        VelocityZ *= 0.8F;
                        VelocityX += pushX * furnaceAcceleration;
                        VelocityZ += pushZ * furnaceAcceleration;
                    }
                    else
                    {
                        VelocityX *= 0.9F;
                        VelocityY = 0.0D;
                        VelocityZ *= 0.9F;
                    }
                }

                VelocityX *= 0.96F;
                VelocityY = 0.0D;
                VelocityZ *= 0.96F;
            }

            Vec3D? currentTrackPosition = getTrackPosition(X, Y, Z);
            if (currentTrackPosition != null && previousTrackPosition != null)
            {
                double railHeightDeltaForce = (previousTrackPosition.Value.y - currentTrackPosition.Value.y) * 0.05D;
                horizontalSpeed = System.Math.Sqrt(VelocityX * VelocityX + VelocityZ * VelocityZ);
                if (horizontalSpeed > 0.0D)
                {
                    VelocityX = VelocityX / horizontalSpeed * (horizontalSpeed + railHeightDeltaForce);
                    VelocityZ = VelocityZ / horizontalSpeed * (horizontalSpeed + railHeightDeltaForce);
                }

                // Important fix:
                // Always keep rail snapping on the same Y convention used everywhere else.
                setTrackAlignedPosition(X, currentTrackPosition.Value.y, Z);
            }

            int currentBlockX = MathHelper.Floor(X);
            int currentBlockZ = MathHelper.Floor(Z);
            if (currentBlockX != blockX || currentBlockZ != blockZ)
            {
                horizontalSpeed = System.Math.Sqrt(VelocityX * VelocityX + VelocityZ * VelocityZ);
                VelocityX = horizontalSpeed * (currentBlockX - blockX);
                VelocityZ = horizontalSpeed * (currentBlockZ - blockZ);
            }

            if (type == 2)
            {
                double pushMagnitude = MathHelper.Sqrt(pushX * pushX + pushZ * pushZ);
                if (pushMagnitude > 0.01D && VelocityX * VelocityX + VelocityZ * VelocityZ > 0.001D)
                {
                    pushX /= pushMagnitude;
                    pushZ /= pushMagnitude;

                    if (pushX * VelocityX + pushZ * VelocityZ < 0.0D)
                    {
                        pushX = 0.0D;
                        pushZ = 0.0D;
                    }
                    else
                    {
                        pushX = VelocityX;
                        pushZ = VelocityZ;
                    }
                }
            }

            if (poweredRailActive)
            {
                double speedMagnitude = System.Math.Sqrt(VelocityX * VelocityX + VelocityZ * VelocityZ);
                if (speedMagnitude > 0.01D)
                {
                    double poweredRailBoost = 0.06D;
                    VelocityX += VelocityX / speedMagnitude * poweredRailBoost;
                    VelocityZ += VelocityZ / speedMagnitude * poweredRailBoost;
                }
                else if (railMeta == 1)
                {
                    if (World.Reader.ShouldSuffocate(blockX - 1, blockY, blockZ))
                    {
                        VelocityX = 0.02D;
                    }
                    else if (World.Reader.ShouldSuffocate(blockX + 1, blockY, blockZ))
                    {
                        VelocityX = -0.02D;
                    }
                }
                else if (railMeta == 0)
                {
                    if (World.Reader.ShouldSuffocate(blockX, blockY, blockZ - 1))
                    {
                        VelocityZ = 0.02D;
                    }
                    else if (World.Reader.ShouldSuffocate(blockX, blockY, blockZ + 1))
                    {
                        VelocityZ = -0.02D;
                    }
                }
            }
        }
        else
        {
            if (VelocityX < -maxSpeed)
            {
                VelocityX = -maxSpeed;
            }

            if (VelocityX > maxSpeed)
            {
                VelocityX = maxSpeed;
            }

            if (VelocityZ < -maxSpeed)
            {
                VelocityZ = -maxSpeed;
            }

            if (VelocityZ > maxSpeed)
            {
                VelocityZ = maxSpeed;
            }

            if (OnGround)
            {
                VelocityX *= 0.5D;
                VelocityY *= 0.5D;
                VelocityZ *= 0.5D;
            }

            Move(VelocityX, VelocityY, VelocityZ);

            if (!OnGround)
            {
                VelocityX *= 0.95F;
                VelocityY *= 0.95F;
                VelocityZ *= 0.95F;
            }
        }

        Pitch = 0.0F;

        double deltaX = PrevX - X;
        double deltaZ = PrevZ - Z;
        if (deltaX * deltaX + deltaZ * deltaZ > 0.001D)
        {
            Yaw = (float)(System.Math.Atan2(deltaZ, deltaX) * 180.0D / System.Math.PI);
            if (yawFlipped)
            {
                Yaw += 180.0F;
            }
        }

        double yawChange = Yaw - PrevYaw;
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
            Yaw += 180.0F;
            yawFlipped = !yawFlipped;
        }

        SetRotation(Yaw, Pitch);

        var nearbyEntities = World.Entities.GetEntities(this, BoundingBox.Expand(0.2D, 0.0D, 0.2D));
        if (nearbyEntities != null && nearbyEntities.Count > 0)
        {
            for (int i = 0; i < nearbyEntities.Count; ++i)
            {
                Entity otherEntity = nearbyEntities[i];
                if (otherEntity != Passenger && otherEntity.IsPushable() && otherEntity is EntityMinecart)
                {
                    otherEntity.OnCollision(this);
                }
            }
        }

        if (Passenger != null && Passenger.Dead)
        {
            Passenger = null;
        }

        if (shouldEmitSmoke && Random.NextInt(4) == 0)
        {
            --fuel;
            if (fuel < 0)
            {
                pushX = 0.0D;
                pushZ = 0.0D;
            }

            World.Broadcaster.AddParticle("largesmoke", X, Y + 0.8D, Z, 0.0D, 0.0D, 0.0D);
        }
    }

    private void setTrackAlignedPosition(double x, double trackY, double z)
    {
        SetPosition(x, trackY + StandingEyeHeight, z);
    }

    public Vec3D? getTrackPositionOffset(double x, double y, double z, double distanceAlongTrack)
    {
        int blockX = MathHelper.Floor(x);
        int blockY = MathHelper.Floor(y);
        int blockZ = MathHelper.Floor(z);

        if (BlockRail.IsRail(World, blockX, blockY - 1, blockZ))
        {
            --blockY;
        }

        int blockId = World.Reader.GetBlockId(blockX, blockY, blockZ);
        if (!BlockRail.IsRail(blockId))
        {
            return null;
        }

        int railMeta = World.Reader.GetBlockMeta(blockX, blockY, blockZ);
        if (((BlockRail)Block.Blocks[blockId]).IsAlwaysStraight())
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

        if (BlockRail.IsRail(World, blockX, blockY - 1, blockZ))
        {
            --blockY;
        }

        int blockId = World.Reader.GetBlockId(blockX, blockY, blockZ);
        if (!BlockRail.IsRail(blockId))
        {
            return null;
        }

        int railMeta = World.Reader.GetBlockMeta(blockX, blockY, blockZ);
        y = blockY;

        if (((BlockRail)Block.Blocks[blockId]).IsAlwaysStraight())
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
    public Vec3D? func_515_a(double x, double y, double z, double trackOffset)
    {
        return getTrackPositionOffset(x, y, z, trackOffset);
    }

    public Vec3D? func_514_g(double x, double y, double z)
    {
        return getTrackPosition(x, y, z);
    }

    public override void WriteNbt(NBTTagCompound nbt)
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
            NBTTagList items = new();

            for (int slotIndex = 0; slotIndex < _cargoItems.Length; ++slotIndex)
            {
                ItemStack? stack = _cargoItems[slotIndex];
                if (stack != null)
                {
                    NBTTagCompound itemTag = new();
                    itemTag.SetByte("Slot", (sbyte)slotIndex);
                    stack.writeToNBT(itemTag);
                    items.SetTag(itemTag);
                }
            }

            nbt.SetTag("Items", items);
        }
    }

    public override void ReadNbt(NBTTagCompound nbt)
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
            _cargoItems = new ItemStack[Size];

            for (int i = 0; i < items.TagCount(); ++i)
            {
                NBTTagCompound itemTag = (NBTTagCompound)items.TagAt(i);
                int slotIndex = itemTag.GetByte("Slot") & 255;
                if (slotIndex >= 0 && slotIndex < _cargoItems.Length)
                {
                    _cargoItems[slotIndex] = new ItemStack(itemTag);
                }
            }
        }
    }

    public override float GetShadowRadius()
    {
        return 0.0F;
    }

    public override void OnCollision(Entity entity)
    {
        if (World.IsRemote)
        {
            return;
        }

        if (entity == Passenger)
        {
            return;
        }

        if (entity is EntityLiving &&
            entity is not EntityPlayer &&
            type == 0 &&
            VelocityX * VelocityX + VelocityZ * VelocityZ > 0.01D &&
            Passenger == null &&
            entity.Vehicle == null)
        {
            entity.SetVehicle(this);
        }

        double deltaX = entity.X - X;
        double deltaZ = entity.Z - Z;
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

        deltaX *= 1.0F - PushSpeedReduction;
        deltaZ *= 1.0F - PushSpeedReduction;

        deltaX *= 0.5D;
        deltaZ *= 0.5D;

        if (entity is EntityMinecart otherCart)
        {
            double otherDeltaX = entity.X - X;
            double otherDeltaZ = entity.Z - Z;

            // Preserved gameplay logic, but with readable names.
            double collisionAlignment = otherDeltaX * entity.VelocityZ + otherDeltaZ * entity.PrevX;
            collisionAlignment *= collisionAlignment;

            if (collisionAlignment > 5.0D)
            {
                return;
            }

            double averageVelocityX = entity.VelocityX + VelocityX;
            double averageVelocityZ = entity.VelocityZ + VelocityZ;

            if (otherCart.type == 2 && type != 2)
            {
                VelocityX *= 0.2F;
                VelocityZ *= 0.2F;
                AddVelocity(entity.VelocityX - deltaX, 0.0D, entity.VelocityZ - deltaZ);
                entity.VelocityX *= 0.7F;
                entity.VelocityZ *= 0.7F;
            }
            else if (otherCart.type != 2 && type == 2)
            {
                entity.VelocityX *= 0.2F;
                entity.VelocityZ *= 0.2F;
                entity.AddVelocity(VelocityX + deltaX, 0.0D, VelocityZ + deltaZ);
                VelocityX *= 0.7F;
                VelocityZ *= 0.7F;
            }
            else
            {
                averageVelocityX /= 2.0D;
                averageVelocityZ /= 2.0D;

                VelocityX *= 0.2F;
                VelocityZ *= 0.2F;
                AddVelocity(averageVelocityX - deltaX, 0.0D, averageVelocityZ - deltaZ);

                entity.VelocityX *= 0.2F;
                entity.VelocityZ *= 0.2F;
                entity.AddVelocity(averageVelocityX + deltaX, 0.0D, averageVelocityZ + deltaZ);
            }
        }
        else
        {
            AddVelocity(-deltaX, 0.0D, -deltaZ);
            entity.AddVelocity(deltaX / 4.0D, 0.0D, deltaZ / 4.0D);
        }
    }

    public int Size => 27;

    public ItemStack? GetStack(int slotIndex)
    {
        return _cargoItems[slotIndex];
    }

    public ItemStack? RemoveStack(int slotIndex, int amount)
    {
        if (_cargoItems[slotIndex] == null)
        {
            return null;
        }

        ItemStack itemStack;
        ItemStack? stack = _cargoItems[slotIndex];

        if (stack is null) return null;

        if (stack.Count <= amount)
        {
            itemStack = stack;
            _cargoItems[slotIndex] = null;
            return itemStack;
        }

        itemStack = stack.Split(amount);
        if (stack.Count == 0)
        {
            _cargoItems[slotIndex] = null;
        }

        return itemStack;
    }

    public void SetStack(int slotIndex, ItemStack? itemStack)
    {
        _cargoItems[slotIndex] = itemStack;
        if (itemStack != null && itemStack.Count > MaxCountPerStack)
        {
            itemStack.Count = MaxCountPerStack;
        }
    }

    public string Name => "Minecart";

    public int MaxCountPerStack => 64;

    public void MarkDirty()
    {
    }

    public override bool Interact(EntityPlayer player)
    {
        if (type == 0)
        {
            if (Passenger != null && Passenger is EntityPlayer && Passenger != player)
            {
                return true;
            }

            if (!World.IsRemote)
            {
                player.SetVehicle(this);
            }
        }
        else if (type == 1)
        {
            if (!World.IsRemote)
            {
                player.openChestScreen(this);
            }
        }
        else if (type == 2)
        {
            ItemStack? heldItem = player.inventory.GetItemInHand();
            if (heldItem != null && heldItem.ItemId == Item.Coal.id)
            {
                if (--heldItem.Count == 0)
                {
                    player.inventory.SetStack(player.inventory.SelectedSlot, null);
                }

                fuel += 1200;
            }

            pushX = X - player.X;
            pushZ = Z - player.Z;
        }

        return true;
    }

    public override void SetPositionAndAnglesAvoidEntities(double x, double y, double z, float yaw, float pitch, int interpolationSteps)
    {
        clientTargetX = x;
        clientTargetY = y;
        clientTargetZ = z;
        clientTargetYaw = yaw;
        clientTargetPitch = pitch;
        clientInterpolationSteps = interpolationSteps + 2;

        VelocityX = clientVelocityX;
        VelocityY = clientVelocityY;
        VelocityZ = clientVelocityZ;
    }

    public override void SetVelocityClient(double velocityX, double velocityY, double velocityZ)
    {
        clientVelocityX = base.VelocityX = velocityX;
        clientVelocityY = base.VelocityY = velocityY;
        clientVelocityZ = base.VelocityZ = velocityZ;
    }

    public bool CanPlayerUse(EntityPlayer player)
    {
        return !Dead && player.GetSquaredDistance(this) <= 64.0D;
    }
}
