using BetaSharp.Blocks;
using BetaSharp.Inventorys;
using BetaSharp.Items;
using BetaSharp.NBT;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core.Systems;
using Microsoft.Extensions.Logging;

namespace BetaSharp.Entities;

public class EntityMinecart : Entity, IInventory
{
    const double maxSpeed = 0.4D;
    const double slopeAcceleration = 1.0D / 128.0D;
    const double poweredRailBoost = 0.06D;

    private static readonly int[][][] s_railShapeVectors =
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

    // Client-side interpolation state.
    private int _clientInterpolationSteps;
    private double _clientTargetPitch;
    private double _clientTargetX;
    private double _clientTargetY;
    private double _clientTargetYaw;
    private double _clientTargetZ;
    private double _clientVelocityX;
    private double _clientVelocityY;
    private double _clientVelocityZ;
    private int _fuel;
    private double _pushX;
    private double _pushZ;

    private bool _yawFlipped;

    // Kept with original names for external compatibility.
    public int MinecartCurrentDamage;
    public int MinecartRockDirection;
    public int MinecartTimeSinceHit;
    public int type;

    public EntityMinecart(IWorldContext world) : base(world)
    {
        _cargoItems = new ItemStack[36];
        MinecartCurrentDamage = 0;
        MinecartTimeSinceHit = 0;
        MinecartRockDirection = 1;
        _yawFlipped = false;
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

    public override EntityType Type => EntityRegistry.Minecart;

    protected override double PassengerRidingHeight => Height * 0.0D - 0.3D;

    public override bool IsPushable => true;

    public override bool HasCollision => !Dead;

    public int Size => 27;

    public ItemStack? GetStack(int slotIndex) => _cargoItems[slotIndex];

    public ItemStack? RemoveStack(int slotIndex, int amount)
    {
        if (_cargoItems[slotIndex] == null) return null;

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

    public bool CanPlayerUse(EntityPlayer player) => !Dead && player.GetSquaredDistance(this) <= 64.0D;

    protected sealed override void SetBoundingBoxSpacing(float width, float height) => base.SetBoundingBoxSpacing(width, height);

    protected override bool BypassesSteppingEffects() => false;

    public override Box? GetCollisionAgainstShape(Entity entity) => entity.BoundingBox;

    public override Box? GetBoundingBox() => BoundingBox;

    public override bool Damage(Entity? entity, int amount)
    {
        if (World.IsRemote || Dead) return true;

        MinecartRockDirection = -MinecartRockDirection;
        MinecartTimeSinceHit = 10;
        ScheduleVelocityUpdate();
        MinecartCurrentDamage += amount * 10;

        if (MinecartCurrentDamage <= 40) return true;

        Passenger?.SetVehicle(this);

        MarkDead();
        DropItem(Item.Minecart.id, 1, 0.0F);

        if (type == 1)
        {
            EntityMinecart minecart = this;

            for (int slotIndex = 0; slotIndex < minecart.Size; ++slotIndex)
            {
                ItemStack? itemStack = minecart.GetStack(slotIndex);
                if (itemStack == null) continue;

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

                    const float scatterSpeed = 0.05F;
                    droppedItem.VelocityX = (float)Random.NextGaussian() * scatterSpeed;
                    droppedItem.VelocityY = (float)Random.NextGaussian() * scatterSpeed + 0.2F;
                    droppedItem.VelocityZ = (float)Random.NextGaussian() * scatterSpeed;
                    World.SpawnEntity(droppedItem);
                }
            }

            DropItem(Block.Chest.id, 1, 0.0F);
        }
        else if (type == 2)
        {
            DropItem(Block.Furnace.id, 1, 0.0F);
        }

        return true;
    }

    public override void AnimateHurt()
    {
        _logger.LogInformation("Animating hurt");
        MinecartRockDirection = -MinecartRockDirection;
        MinecartTimeSinceHit = 10;
        MinecartCurrentDamage += MinecartCurrentDamage * 10;
    }

    public override void MarkDead()
    {
        for (int slotIndex = 0; slotIndex < Size; ++slotIndex)
        {
            ItemStack? itemStack = GetStack(slotIndex);
            if (itemStack == null) continue;

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

        base.MarkDead();
    }

    public override void Tick()
    {
        if (MinecartTimeSinceHit > 0)
        {
            --MinecartTimeSinceHit;
        }

        if (MinecartCurrentDamage > 0)
        {
            --MinecartCurrentDamage;
        }

        if (World.IsRemote && _clientInterpolationSteps > 0)
        {
            double interpolatedX = X + (_clientTargetX - X) / _clientInterpolationSteps;
            double interpolatedY = Y + (_clientTargetY - Y) / _clientInterpolationSteps;
            double interpolatedZ = Z + (_clientTargetZ - Z) / _clientInterpolationSteps;

            double yawDelta = _clientTargetYaw - Yaw;
            while (yawDelta < -180.0D)
            {
                yawDelta += 360.0D;
            }

            while (yawDelta >= 180.0D)
            {
                yawDelta -= 360.0D;
            }

            Yaw = (float)(Yaw + yawDelta / _clientInterpolationSteps);
            Pitch = (float)(Pitch + (_clientTargetPitch - Pitch) / _clientInterpolationSteps);
            --_clientInterpolationSteps;

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

        if (BlockRail.isRail(World, blockX, blockY - 1, blockZ))
        {
            --blockY;
        }


        bool shouldEmitSmoke = false;

        int railBlockId = World.Reader.GetBlockId(blockX, blockY, blockZ);
        if (BlockRail.isRail(railBlockId))
        {
            Vec3D? previousTrackPosition = GetTrackPosition(X, Y, Z);
            int railMeta = World.Reader.GetBlockMeta(blockX, blockY, blockZ);

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

            if (railMeta is >= 2 and <= 5)
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

            int[][] railEnds = s_railShapeVectors[railMeta];
            double railDirX = railEnds[1][0] - railEnds[0][0];
            double railDirZ = railEnds[1][2] - railEnds[0][2];
            double railDirLength = Math.Sqrt(railDirX * railDirX + railDirZ * railDirZ);
            double velocityDotRail = VelocityX * railDirX + VelocityZ * railDirZ;

            if (velocityDotRail < 0.0D)
            {
                railDirX = -railDirX;
                railDirZ = -railDirZ;
            }

            double horizontalSpeed = Math.Sqrt(VelocityX * VelocityX + VelocityZ * VelocityZ);
            VelocityX = horizontalSpeed * railDirX / railDirLength;
            VelocityZ = horizontalSpeed * railDirZ / railDirLength;

            if (poweredRailBraking)
            {
                double brakingSpeed = Math.Sqrt(VelocityX * VelocityX + VelocityZ * VelocityZ);
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

            double positionAlongRail;
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
                    double furnacePushMagnitude = MathHelper.Sqrt(_pushX * _pushX + _pushZ * _pushZ);
                    if (furnacePushMagnitude > 0.01D)
                    {
                        shouldEmitSmoke = true;
                        _pushX /= furnacePushMagnitude;
                        _pushZ /= furnacePushMagnitude;

                        double furnaceAcceleration = 0.04D;
                        VelocityX *= 0.8F;
                        VelocityY = 0.0D;
                        VelocityZ *= 0.8F;
                        VelocityX += _pushX * furnaceAcceleration;
                        VelocityZ += _pushZ * furnaceAcceleration;
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

            Vec3D? currentTrackPosition = GetTrackPosition(X, Y, Z);
            if (currentTrackPosition != null && previousTrackPosition != null)
            {
                double railHeightDeltaForce = (previousTrackPosition.Value.y - currentTrackPosition.Value.y) * 0.05D;
                horizontalSpeed = Math.Sqrt(VelocityX * VelocityX + VelocityZ * VelocityZ);
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
                horizontalSpeed = Math.Sqrt(VelocityX * VelocityX + VelocityZ * VelocityZ);
                VelocityX = horizontalSpeed * (currentBlockX - blockX);
                VelocityZ = horizontalSpeed * (currentBlockZ - blockZ);
            }

            if (type == 2)
            {
                double pushMagnitude = MathHelper.Sqrt(_pushX * _pushX + _pushZ * _pushZ);
                if (pushMagnitude > 0.01D && VelocityX * VelocityX + VelocityZ * VelocityZ > 0.001D)
                {
                    _pushX /= pushMagnitude;
                    _pushZ /= pushMagnitude;

                    if (_pushX * VelocityX + _pushZ * VelocityZ < 0.0D)
                    {
                        _pushX = 0.0D;
                        _pushZ = 0.0D;
                    }
                    else
                    {
                        _pushX = VelocityX;
                        _pushZ = VelocityZ;
                    }
                }
            }

            if (poweredRailActive)
            {
                double speedMagnitude = Math.Sqrt(VelocityX * VelocityX + VelocityZ * VelocityZ);
                if (speedMagnitude > 0.01D)
                {
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
            Yaw = (float)(Math.Atan2(deltaZ, deltaX) * 180.0D / Math.PI);
            if (_yawFlipped)
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
            _yawFlipped = !_yawFlipped;
        }

        SetRotation(Yaw, Pitch);

        List<Entity> nearbyEntities = World.Entities.GetEntities(this, BoundingBox.Expand(0.2D, 0.0D, 0.2D));
        if (nearbyEntities is { Count: > 0 })
        {
            foreach (Entity otherEntity in nearbyEntities)
            {
                if (!Equals(otherEntity, Passenger) && otherEntity.IsPushable && otherEntity is EntityMinecart)
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
            --_fuel;
            if (_fuel < 0)
            {
                _pushX = 0.0D;
                _pushZ = 0.0D;
            }

            World.Broadcaster.AddParticle("largesmoke", X, Y + 0.8D, Z, 0.0D, 0.0D, 0.0D);
        }
    }

    private void setTrackAlignedPosition(double x, double trackY, double z) => SetPosition(x, trackY + StandingEyeHeight, z);

    public Vec3D? GetTrackPositionOffset(double x, double y, double z, double distanceAlongTrack)
    {
        int blockX = MathHelper.Floor(x);
        int blockY = MathHelper.Floor(y);
        int blockZ = MathHelper.Floor(z);

        if (BlockRail.isRail(World, blockX, blockY - 1, blockZ))
        {
            --blockY;
        }

        int blockId = World.Reader.GetBlockId(blockX, blockY, blockZ);
        if (!BlockRail.isRail(blockId))
        {
            return null;
        }

        int railMeta = World.Reader.GetBlockMeta(blockX, blockY, blockZ);
        if (((BlockRail)Block.Blocks[blockId]).isAlwaysStraight())
        {
            railMeta &= 7;
        }

        y = blockY;
        if (railMeta >= 2 && railMeta <= 5)
        {
            y = blockY + 1;
        }

        int[][] railEnds = s_railShapeVectors[railMeta];
        double railDirX = railEnds[1][0] - railEnds[0][0];
        double railDirZ = railEnds[1][2] - railEnds[0][2];
        double railDirLength = Math.Sqrt(railDirX * railDirX + railDirZ * railDirZ);

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

        return GetTrackPosition(x, y, z);
    }

    public Vec3D? GetTrackPosition(double x, double y, double z)
    {
        int blockX = MathHelper.Floor(x);
        int blockY = MathHelper.Floor(y);
        int blockZ = MathHelper.Floor(z);

        if (BlockRail.isRail(World, blockX, blockY - 1, blockZ))
        {
            --blockY;
        }

        int blockId = World.Reader.GetBlockId(blockX, blockY, blockZ);
        if (!BlockRail.isRail(blockId))
        {
            return null;
        }

        int railMeta = World.Reader.GetBlockMeta(blockX, blockY, blockZ);
        y = blockY;

        if (((BlockRail)Block.Blocks[blockId]).isAlwaysStraight())
        {
            railMeta &= 7;
        }

        if (railMeta is >= 2 and <= 5)
        {
            y = blockY + 1;
        }

        int[][] railEnds = s_railShapeVectors[railMeta];

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

    protected override void WriteNbt(NBTTagCompound nbt)
    {
        nbt.SetInteger("Type", type);

        if (type == 2)
        {
            nbt.SetDouble("PushX", _pushX);
            nbt.SetDouble("PushZ", _pushZ);
            nbt.SetShort("Fuel", (short)_fuel);
        }
        else if (type == 1)
        {
            NBTTagList items = new();

            for (int slotIndex = 0; slotIndex < _cargoItems.Length; ++slotIndex)
            {
                ItemStack? stack = _cargoItems[slotIndex];
                if (stack == null)
                {
                    continue;
                }

                NBTTagCompound itemTag = new();
                itemTag.SetByte("Slot", (sbyte)slotIndex);
                stack.writeToNBT(itemTag);
                items.SetTag(itemTag);
            }

            nbt.SetTag("Items", items);
        }
    }

    protected override void ReadNbt(NBTTagCompound nbt)
    {
        type = nbt.GetInteger("Type");

        if (type == 2)
        {
            _pushX = nbt.GetDouble("PushX");
            _pushZ = nbt.GetDouble("PushZ");
            _fuel = nbt.GetShort("Fuel");
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

    public override float GetShadowRadius() => 0.0F;

    public override void OnCollision(Entity entity)
    {
        if (World.IsRemote) return;

        if (Equals(entity, Passenger)) return;

        if (entity is EntityLiving and not EntityPlayer &&
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

        if (distanceSq < 1.0E-4D) return;

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

            if (collisionAlignment > 5.0D) return;

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

    public override bool Interact(EntityPlayer player)
    {
        if (type == 0)
        {
            if (Passenger is EntityPlayer && !Equals(Passenger, player)) return true;
            if (!World.IsRemote) player.SetVehicle(this);
        }
        else if (type == 1)
        {
            if (!World.IsRemote) player.openChestScreen(this);
        }
        else if (type == 2)
        {
            ItemStack? heldItem = player.Inventory.ItemInHand;
            if (heldItem != null && heldItem.ItemId == Item.Coal.id)
            {
                if (--heldItem.Count == 0)
                {
                    player.Inventory.SetStack(player.Inventory.SelectedSlot, null);
                }

                _fuel += 1200;
            }

            _pushX = X - player.X;
            _pushZ = Z - player.Z;
        }

        return true;
    }

    public override void SetPositionAndAnglesAvoidEntities(double x, double y, double z, float yaw, float pitch, int interpolationSteps)
    {
        _clientTargetX = x;
        _clientTargetY = y;
        _clientTargetZ = z;
        _clientTargetYaw = yaw;
        _clientTargetPitch = pitch;
        _clientInterpolationSteps = interpolationSteps + 2;

        VelocityX = _clientVelocityX;
        VelocityY = _clientVelocityY;
        VelocityZ = _clientVelocityZ;
    }

    public override void SetVelocityClient(double velocityX, double velocityY, double velocityZ)
    {
        _clientVelocityX = VelocityX = velocityX;
        _clientVelocityY = VelocityY = velocityY;
        _clientVelocityZ = VelocityZ = velocityZ;
    }
}
