using BetaSharp.Blocks;
using BetaSharp.Blocks.Materials;
using BetaSharp.Items;
using BetaSharp.NBT;
using BetaSharp.Util;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Chunks;
using BetaSharp.Worlds.Core.Systems;
using Math = System.Math;

namespace BetaSharp.Entities;

public abstract class Entity
{
    private static int s_nextEntityId;
    private readonly SyncedProperty<byte> _flags;
    private bool _firstTick = true;
    private int _nextStepSoundDistance = 1;
    private double _vehiclePitchDelta;
    private double _vehicleYawDelta;
    public Box BoundingBox = new(0.0D, 0.0D, 0.0D, 0.0D, 0.0D, 0.0D);

    protected Entity(IWorldContext world)
    {
        World = world;
        SetPosition(0.0D, 0.0D, 0.0D);
        _flags = DataSynchronizer.MakeProperty<byte>(0, 0);
    }

    public abstract EntityType? Type { get; }
    public int ID { get; set; } = s_nextEntityId++;

    /// <summary>
    ///     Multiplayer for rendering, based of the render distance,
    /// </summary>
    protected double RenderDistanceWeight { get; init; } = 1.0D;

    /// <summary>
    ///     Prevents another entity spawning near/in this entity.
    /// </summary>
    public bool PreventEntitySpawning { get; protected init; }

    public Entity? Passenger { get; set; }
    public Entity? Vehicle { get; set; }
    public IWorldContext World { get; private set; }
    public double PrevX { get; set; }
    public double PrevY { get; set; }
    public double PrevZ { get; set; }
    public double X { get; set; }
    public double Y { get; set; }
    public double Z { get; set; }
    public double VelocityX { get; set; }
    public double VelocityY { get; set; }
    public double VelocityZ { get; set; }
    public float Yaw { get; set; }
    public float Pitch { get; set; }
    public float PrevYaw { get; set; }
    public float PrevPitch { get; set; }
    public bool OnGround { get; set; }

    /// <summary>
    ///     If a collision occured in the X or Z directions.
    /// </summary>
    protected bool HorizontalCollision { get; private set; }

    /// <summary>
    ///     If a collision occured in the Y direction.
    /// </summary>
    private bool VerticalCollision { get; set; }

    /// <summary>
    ///     If a collision occured in either the X, Y, OR Z directions.
    /// </summary>
    public bool HasCollided { get; set; }

    public bool VelocityModified { get; set; }
    public bool Slowed { get; set; }
    private static bool KeepVelocityOnCollision => true;
    public bool Dead { get; set; }
    public float Width { get; private set; } = 0.6F;
    public float Height { get; private set; } = 1.8F;
    public float PrevHorizontalSpeed { get; private set; }
    public float HorizontalSpeed { get; private set; }
    protected float FallDistance { get; set; }
    public double LastTickX { get; set; }
    public double LastTickY { get; set; }
    public double LastTickZ { get; set; }
    public float CameraOffset { get; set; }
    protected float StepHeight { get; init; }
    protected bool NoClip { get; init; }
    protected static float PushSpeedReduction => 0.0F;
    protected JavaRandom Random { get; } = new();
    public int Age { get; private set; }
    protected int FireImmunityTicks { get; init; } = 1;
    protected int FireTicks { get; set; }
    public static int MaxAir => 300;
    protected bool InWater { get; private set; }
    public int Hearts { get; protected set; }
    public int Air { get; protected set; } = 300;
    public string? CloakUrl { get; set; }
    protected bool IsImmuneToFire { get; init; }
    public DataSynchronizer DataSynchronizer { get; } = new();
    public float MinBrightness { get; set; }
    public bool IsPersistent { get; set; }
    public int ChunkX { get; set; }
    public int ChunkSlice { get; set; }
    public int ChunkZ { get; set; }
    public int TrackedPosX { get; set; }
    public int TrackedPosY { get; set; }
    public int TrackedPosZ { get; set; }

    /// <summary>
    ///     If an entity should render even IF it's outside the viewing angle.
    /// </summary>
    public bool IgnoreFrustumCheck { get; init; }

    public Vec3D Position => new(X, Y, Z);

    public float StandingEyeHeight { get; protected set; }

    protected virtual double PassengerRidingHeight => Height * 0.75D;

    public virtual float TargetingMargin => 0.1F;

    public virtual Vec3D? LookVector => null;

    public bool IsOnFire => FireTicks > 0 || GetFlag(0);

    public bool HasVehicle => Vehicle != null || GetFlag(2);

    public virtual ItemStack?[] Equipment => null;

    protected bool IsWet => InWater || World.Environment.IsRainingAt(MathHelper.Floor(X), MathHelper.Floor(Y), MathHelper.Floor(Z));

    protected virtual bool IsInWater => InWater;

    protected bool IsTouchingLava => World.Reader.IsMaterialInBox(BoundingBox.Expand(-0.1F, -0.4F, -0.1F), m => m == Material.Lava);

    public virtual bool IsAlive => !Dead;

    public virtual float EyeHeight => 0.0F;

    public virtual bool HasCollision => false;

    public virtual bool IsPushable => false;

    /// <summary>
    ///     Keep moving up until there's no collision.
    /// </summary>
    /// <remarks>
    ///     Note that the Pitch will be reset to 0, and the Motion
    ///     will be fully zeroed, so the entity might fall for a bit
    ///     if the position was off at the start.
    /// </remarks>
    public virtual void TeleportToTop()
    {
        while (Y > 0.0D)
        {
            SetPosition(X, Y, Z);
            if (World.Entities.GetEntityCollisionsScratch(this, BoundingBox).Count == 0) break;
            ++Y;
        }

        VelocityX = VelocityY = VelocityZ = 0.0D;
        Pitch = 0.0F;
    }

    public virtual void MarkDead() => Dead = true;

    protected virtual void SetBoundingBoxSpacing(float width, float height)
    {
        Width = width;
        Height = height;
    }

    protected void SetRotation(float yaw, float pitch)
    {
        Yaw = yaw % 360.0F;
        Pitch = pitch % 360.0F;
    }

    public void SetPosition(double x, double y, double z)
    {
        X = x;
        Y = y;
        Z = z;
        float halfWidth = Width / 2.0F;
        float height = Height;
        BoundingBox = new Box(x - halfWidth, y - StandingEyeHeight + CameraOffset, z - halfWidth, x + halfWidth, y - StandingEyeHeight + CameraOffset + height, z + halfWidth);
    }

    /// <summary>
    ///     Change the current look direction, with capping the pitch.
    /// </summary>
    public void ChangeLookDirection(float yaw, float pitch)
    {
        float oldPitch = Pitch;
        float oldYaw = Yaw;
        Yaw = (float)(Yaw + yaw * 0.15D);
        Pitch = (float)(Pitch - pitch * 0.15D);
        if (Pitch < -90.0F) Pitch = -90.0F;
        if (Pitch > 90.0F) Pitch = 90.0F;
        PrevPitch += Pitch - oldPitch;
        PrevYaw += Yaw - oldYaw;
    }

    public virtual void Tick() => BaseTick();

    public virtual void BaseTick()
    {
        if (Vehicle is { Dead: true })
        {
            Vehicle = null;
        }

        ++Age;
        PrevHorizontalSpeed = HorizontalSpeed;
        PrevX = X;
        PrevY = Y;
        PrevZ = Z;
        PrevPitch = Pitch;
        PrevYaw = Yaw;
        if (CheckWaterCollisions())
        {
            if (!InWater && !_firstTick)
            {
                float volume = MathHelper.Sqrt(VelocityX * VelocityX * 0.2F + VelocityY * VelocityY + VelocityZ * VelocityZ * 0.2F) * 0.2F;
                if (volume > 1.0F)
                {
                    volume = 1.0F;
                }

                World.Broadcaster.PlaySoundAtEntity(this, "random.splash", volume, 1.0F + (Random.NextFloat() - Random.NextFloat()) * 0.4F);
                float floorMinY = MathHelper.Floor(BoundingBox.MinY);

                for (int i = 0; i < 1.0F + Width * 20.0F; ++i)
                {
                    double xOffset = (Random.NextFloat() * 2.0F - 1.0F) * Width;
                    double zOffset = (Random.NextFloat() * 2.0F - 1.0F) * Width;
                    World.Broadcaster.AddParticle("bubble", X + xOffset, floorMinY + 1.0D, Z + zOffset, VelocityX, VelocityY - Random.NextFloat() * 0.2D, VelocityZ);

                    xOffset = (Random.NextFloat() * 2.0F - 1.0F) * Width;
                    zOffset = (Random.NextFloat() * 2.0F - 1.0F) * Width;
                    World.Broadcaster.AddParticle("splash", X + xOffset, floorMinY + 1.0D, Z + zOffset, VelocityX, VelocityY, VelocityZ);
                }
            }

            FallDistance = 0.0F;
            InWater = true;
            FireTicks = 0;
        }
        else
        {
            InWater = false;
        }

        if (World.IsRemote)
        {
            FireTicks = 0;
        }
        else if (FireTicks > 0)
        {
            if (IsImmuneToFire)
            {
                FireTicks -= 4;
                if (FireTicks < 0)
                {
                    FireTicks = 0;
                }
            }
            else
            {
                if (FireTicks % 20 == 0)
                {
                    Damage(null, 1);
                }

                --FireTicks;
            }
        }

        if (IsTouchingLava)
        {
            SetOnFire();
        }

        if (Y < -64.0D)
        {
            TickInVoid();
        }

        if (!World.IsRemote)
        {
            SetFlag(0, FireTicks > 0);
            SetFlag(2, Vehicle != null);
        }

        _firstTick = false;
    }

    private void SetOnFire()
    {
        if (IsImmuneToFire) return;
        Damage(null, 4);
        FireTicks = 600;
    }

    protected virtual void TickInVoid() => MarkDead();

    protected bool GetEntitiesInside(double x, double y, double z)
    {
        Box box = BoundingBox.Offset(x, y, z);
        List<Box> entitiesInbound = World.Entities.GetEntityCollisionsScratch(this, box);
        return entitiesInbound.Count <= 0 && !World.Reader.IsMaterialInBox(box, m => m.IsFluid);
    }

    /// <summary>
    ///     Move by a certain amount, making sure to handle collisions and the such.
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="z"></param>
    public virtual void Move(double x, double y, double z)
    {
        if (World.IsRemote && this is not EntityPlayer)
        {
            int minChunkX = MathHelper.Floor(BoundingBox.MinX) >> 4;
            int maxChunkX = MathHelper.Floor(BoundingBox.MaxX) >> 4;
            int minChunkZ = MathHelper.Floor(BoundingBox.MinZ) >> 4;
            int maxChunkZ = MathHelper.Floor(BoundingBox.MaxZ) >> 4;

            for (int chunkX = minChunkX; chunkX <= maxChunkX; ++chunkX)
            {
                for (int chunkZ = minChunkZ; chunkZ <= maxChunkZ; ++chunkZ)
                {
                    var chunk = World.ChunkHost.GetChunk(chunkX, chunkZ);
                    if (!chunk.Loaded)
                    {
                        VelocityX = VelocityY = VelocityZ = 0.0D;
                        return;
                    }
                }
            }
        }

        if (NoClip)
        {
            BoundingBox.Translate(x, y, z);
            X = (BoundingBox.MinX + BoundingBox.MaxX) / 2.0D;
            Y = BoundingBox.MinY + (double)StandingEyeHeight - (double)CameraOffset;
            Z = (BoundingBox.MinZ + BoundingBox.MaxZ) / 2.0D;
        }
        else
        {
            CameraOffset *= 0.4F;
            double mx = X;
            double my = Z;
            if (Slowed)
            {
                Slowed = false;
                x *= 0.25D;
                y *= (double)0.05F;
                z *= 0.25D;
                VelocityX = 0.0D;
                VelocityY = 0.0D;
                VelocityZ = 0.0D;
            }

            double originalX = x;
            double originalY = y;
            double originalZ = z;
            Box bound = BoundingBox;
            bool sneakingOnGround = OnGround && IsSneaking();
            if (sneakingOnGround)
            {
                double edgeStep;
                for (edgeStep = 0.05D; x != 0.0D && World.Entities.GetEntityCollisionsScratch(this, BoundingBox.Offset(x, -1.0D, 0.0D)).Count == 0; originalX = x)
                {
                    if (x < edgeStep && x >= -edgeStep)
                    {
                        x = 0.0D;
                    }
                    else if (x > 0.0D)
                    {
                        x -= edgeStep;
                    }
                    else
                    {
                        x += edgeStep;
                    }
                }

                for (; z != 0.0D && World.Entities.GetEntityCollisionsScratch(this, BoundingBox.Offset(0.0D, -1.0D, z)).Count == 0; originalZ = z)
                {
                    if (z < edgeStep && z >= -edgeStep)
                    {
                        z = 0.0D;
                    }
                    else if (z > 0.0D)
                    {
                        z -= edgeStep;
                    }
                    else
                    {
                        z += edgeStep;
                    }
                }
            }

            List<Box> entitiesInbound = World.Entities.GetEntityCollisionsScratch(this, BoundingBox.Stretch(x, y, z));

            for (int i = 0; i < entitiesInbound.Count; ++i)
            {
                y = entitiesInbound[i].GetYOffset(BoundingBox, y);
            }

            BoundingBox.Translate(0.0D, y, 0.0D);
            if (!KeepVelocityOnCollision && originalY != y)
            {
                z = 0.0D;
                y = z;
                x = z;
            }

            bool canStepUp = OnGround || originalY != y && originalY < 0.0D;

            for (int i = 0; i < entitiesInbound.Count; ++i)
            {
                x = entitiesInbound[i].GetXOffset(BoundingBox, x);
            }

            BoundingBox.Translate(x, 0.0D, 0.0D);
            if (!KeepVelocityOnCollision && originalX != x)
            {
                z = 0.0D;
                y = z;
                x = z;
            }

            for (int i = 0; i < entitiesInbound.Count; ++i)
            {
                z = entitiesInbound[i].GetZOffset(BoundingBox, z);
            }

            BoundingBox.Translate(0.0D, 0.0D, z);
            if (!KeepVelocityOnCollision && originalZ != z)
            {
                z = 0.0D;
                y = z;
                x = z;
            }

            double originalStepZ;
            int blockId;
            double originalStepX;
            if (StepHeight > 0.0F && canStepUp && (sneakingOnGround || CameraOffset < 0.05F) && (originalX != x || originalZ != z))
            {
                originalStepX = x;
                originalStepZ = y;
                double originalZStep = z;
                x = originalX;
                y = (double)StepHeight;
                z = originalZ;
                Box originalBoundingBox = BoundingBox;
                BoundingBox = bound;
                entitiesInbound = World.Entities.GetEntityCollisionsScratch(this, BoundingBox.Stretch(originalX, y, originalZ));

                for (blockId = 0; blockId < entitiesInbound.Count; ++blockId)
                {
                    y = entitiesInbound[blockId].GetYOffset(BoundingBox, y);
                }

                BoundingBox.Translate(0.0D, y, 0.0D);
                if (!KeepVelocityOnCollision && originalY != y)
                {
                    z = 0.0D;
                    y = z;
                    x = z;
                }

                for (blockId = 0; blockId < entitiesInbound.Count; ++blockId)
                {
                    x = entitiesInbound[blockId].GetXOffset(BoundingBox, x);
                }

                BoundingBox.Translate(x, 0.0D, 0.0D);
                if (!KeepVelocityOnCollision && originalX != x)
                {
                    z = 0.0D;
                    y = z;
                    x = z;
                }

                for (blockId = 0; blockId < entitiesInbound.Count; ++blockId)
                {
                    z = entitiesInbound[blockId].GetZOffset(BoundingBox, z);
                }

                BoundingBox.Translate(0.0D, 0.0D, z);
                if (!KeepVelocityOnCollision && originalZ != z)
                {
                    z = 0.0D;
                    y = z;
                    x = z;
                }

                if (!KeepVelocityOnCollision && originalY != y)
                {
                    z = 0.0D;
                    y = z;
                    x = z;
                }
                else
                {
                    y = (double)(-StepHeight);

                    for (blockId = 0; blockId < entitiesInbound.Count; ++blockId)
                    {
                        y = entitiesInbound[blockId].GetYOffset(BoundingBox, y);
                    }

                    BoundingBox.Translate(0.0D, y, 0.0D);
                }

                if (originalStepX * originalStepX + originalZStep * originalZStep >= x * x + z * z)
                {
                    x = originalStepX;
                    y = originalStepZ;
                    z = originalZStep;
                    BoundingBox = originalBoundingBox;
                }
                else
                {
                    double stepHeightOffset = BoundingBox.MinY - (double)((int)BoundingBox.MinY);
                    if (stepHeightOffset > 0.0D)
                    {
                        CameraOffset = (float)((double)CameraOffset + stepHeightOffset + 0.01D);
                    }
                }
            }

            X = (BoundingBox.MinX + BoundingBox.MaxX) / 2.0D;
            Y = BoundingBox.MinY + (double)StandingEyeHeight - (double)CameraOffset;
            Z = (BoundingBox.MinZ + BoundingBox.MaxZ) / 2.0D;
            HorizontalCollision = originalX != x || originalZ != z;
            VerticalCollision = originalY != y;
            OnGround = originalY != y && originalY < 0.0D;
            HasCollided = HorizontalCollision || VerticalCollision;
            Fall(y, OnGround);
            if (originalX != x)
            {
                VelocityX = 0.0D;
            }

            if (originalY != y)
            {
                VelocityY = 0.0D;
            }

            if (originalZ != z)
            {
                VelocityZ = 0.0D;
            }

            originalStepX = X - mx;
            originalStepZ = Z - my;
            int blockY;
            int blockX;
            int blockZ;
            if (BypassesSteppingEffects() && !sneakingOnGround && Vehicle == null)
            {
                HorizontalSpeed = (float)((double)HorizontalSpeed + (double)MathHelper.Sqrt(originalStepX * originalStepX + originalStepZ * originalStepZ) * 0.6D);

                if (OnGround)
                {
                    blockX = MathHelper.Floor(X);
                    blockY = MathHelper.Floor(Y - (double)0.2F - (double)StandingEyeHeight);
                    blockZ = MathHelper.Floor(Z);
                    blockId = World.Reader.GetBlockId(blockX, blockY, blockZ);
                    if (World.Reader.GetBlockId(blockX, blockY - 1, blockZ) == Block.Fence.id)
                    {
                        blockId = World.Reader.GetBlockId(blockX, blockY - 1, blockZ);
                    }

                    if (HorizontalSpeed > (float)_nextStepSoundDistance && blockId > 0)
                    {
                        _nextStepSoundDistance = (int)HorizontalSpeed + 1;
                        BlockSoundGroup soundGroup = Block.Blocks[blockId].SoundGroup;
                        if (World.Reader.GetBlockId(blockX, blockY + 1, blockZ) == Block.Snow.id)
                        {
                            soundGroup = Block.Snow.SoundGroup;
                            World.Broadcaster.PlaySoundAtEntity(this, soundGroup.StepSound, soundGroup.Volume * 0.15F, soundGroup.Pitch);
                        }
                        else if (!Block.Blocks[blockId].material.IsFluid)
                        {
                            World.Broadcaster.PlaySoundAtEntity(this, soundGroup.StepSound, soundGroup.Volume * 0.15F, soundGroup.Pitch);
                        }

                        Block.Blocks[blockId].onSteppedOn(new OnEntityStepEvent(World, this, blockX, blockY, blockZ));
                    }
                }
            }

            blockX = MathHelper.Floor(BoundingBox.MinX + 0.001D);
            blockY = MathHelper.Floor(BoundingBox.MinY + 0.001D);
            blockZ = MathHelper.Floor(BoundingBox.MinZ + 0.001D);
            blockId = MathHelper.Floor(BoundingBox.MaxX - 0.001D);
            int maxBlockY = MathHelper.Floor(BoundingBox.MaxY - 0.001D);
            int maxBlockZ = MathHelper.Floor(BoundingBox.MaxZ - 0.001D);
            if (World.ChunkHost.IsRegionLoaded(blockX, blockY, blockZ, blockId, maxBlockY, maxBlockZ))
            {
                for (int collisionX = blockX; collisionX <= blockId; ++collisionX)
                {
                    for (int collisionY = blockY; collisionY <= maxBlockY; ++collisionY)
                    {
                        for (int collisionZ = blockZ; collisionZ <= maxBlockZ; ++collisionZ)
                        {
                            int collisionBlockId = World.Reader.GetBlockId(collisionX, collisionY, collisionZ);
                            if (collisionBlockId > 0)
                            {
                                Block.Blocks[collisionBlockId].onEntityCollision(new OnEntityCollisionEvent(World, this, collisionX, collisionY, collisionZ));
                            }
                        }
                    }
                }
            }

            bool wet = IsWet;
            if (World.Reader.IsMaterialInBox(BoundingBox.Contract(0.001D, 0.001D, 0.001D), m => m == Material.Fire || m == Material.Lava))
            {
                Damage(1);
                if (!wet)
                {
                    ++FireTicks;
                    if (FireTicks == 0)
                    {
                        FireTicks = 300;
                    }
                }
            }
            else if (FireTicks <= 0)
            {
                FireTicks = -FireImmunityTicks;
            }

            if (wet && FireTicks > 0)
            {
                World.Broadcaster.PlaySoundAtEntity(this, "random.fizz", 0.7F, 1.6F + (Random.NextFloat() - Random.NextFloat()) * 0.4F);
                FireTicks = -FireImmunityTicks;
            }

        }
    }

    protected virtual bool BypassesSteppingEffects() => true;

    protected virtual void Fall(double fallDistance, bool onGround)
    {
        if (onGround)
        {
            if (!(FallDistance > 0.0F))
            {
                return;
            }

            OnLanding(FallDistance);
            FallDistance = 0.0F;
        }
        else if (fallDistance < 0.0D)
        {
            FallDistance = (float)(FallDistance - fallDistance);
        }
    }

    public virtual Box? GetBoundingBox() => null;

    protected virtual void Damage(int amt)
    {
        if (!IsImmuneToFire)
        {
            Damage(null, amt);
        }
    }

    protected virtual void OnLanding(float fallDistance) => Passenger?.OnLanding(fallDistance);

    public virtual bool CheckWaterCollisions() => World.Reader.UpdateMovementInFluid(BoundingBox.Expand(0.0D, -0.4F, 0.0D).Contract(0.001D, 0.001D, 0.001D), Material.Water, this);

    public bool IsInFluid(Material mat)
    {
        double eyeY = Y + EyeHeight;
        int floorX = MathHelper.Floor(X);
        int floorEyeY = MathHelper.Floor(MathHelper.Floor(eyeY));
        int floorZ = MathHelper.Floor(Z);
        int id = World.Reader.GetBlockId(floorX, floorEyeY, floorZ);
        if (id != 0 && Block.Blocks[id].material == mat)
        {
            float fluidHeight = BlockFluid.getFluidHeightFromMeta(World.Reader.GetBlockMeta(floorX, floorEyeY, floorZ)) - 1.0F / 9.0F;
            float fluidSurfaceY = floorEyeY + 1 - fluidHeight;
            return eyeY < fluidSurfaceY;
        }

        return false;
    }

    protected void MoveNonSolid(float strafe, float forward, float speed)
    {
        float inputLength = MathHelper.Sqrt(strafe * strafe + forward * forward);
        if (!(inputLength >= 0.01F))
        {
            return;
        }

        if (inputLength < 1.0F)
        {
            inputLength = 1.0F;
        }

        inputLength = speed / inputLength;
        strafe *= inputLength;
        forward *= inputLength;
        float sinYaw = MathHelper.Sin(Yaw * (float)Math.PI / 180.0F);
        float cosYaw = MathHelper.Cos(Yaw * (float)Math.PI / 180.0F);
        VelocityX += strafe * cosYaw - forward * sinYaw;
        VelocityZ += forward * cosYaw + strafe * sinYaw;
    }

    public float GetBrightnessAtEyes(float tickDelta)
    {
        int floorX = MathHelper.Floor(X);
        double eyeOffset = (BoundingBox.MaxY - BoundingBox.MinY) * 0.66D;
        int floorY = MathHelper.Floor(Y - StandingEyeHeight + eyeOffset);
        int floorZ = MathHelper.Floor(Z);

        int minX = MathHelper.Floor(BoundingBox.MinX);
        int minY = MathHelper.Floor(BoundingBox.MinY);
        int minZ = MathHelper.Floor(BoundingBox.MinZ);
        int maxX = MathHelper.Floor(BoundingBox.MaxX);
        int maxY = MathHelper.Floor(BoundingBox.MaxY);
        int maxZ = MathHelper.Floor(BoundingBox.MaxZ);

        int h = ChuckFormat.WorldHeight - 1;
        minY = Math.Clamp(minY, 0, h);
        maxY = Math.Clamp(maxY, 0, h);

        if (!World.ChunkHost.IsRegionLoaded(minX, minY, minZ, maxX, maxY, maxZ))
        {
            return MinBrightness;
        }

        float lum = World.Lighting.GetLuminance(floorX, floorY, floorZ);
        if (lum < MinBrightness)
        {
            lum = MinBrightness;
        }

        return lum;
    }

    public virtual void SetWorld(IWorldContext world) => World = world;

    public void SetPositionAndAngles(double x, double y, double z, float yaw, float pitch)
    {
        PrevX = X = x;
        PrevY = Y = y;
        PrevZ = Z = z;
        PrevYaw = Yaw = yaw;
        PrevPitch = Pitch = pitch;
        CameraOffset = 0.0F;
        double diff = PrevYaw - yaw;
        if (diff < -180.0D)
        {
            PrevYaw += 360.0F;
        }

        if (diff >= 180.0D)
        {
            PrevYaw -= 360.0F;
        }

        SetPosition(X, Y, Z);
        SetRotation(yaw, pitch);
    }

    public void SetPositionAndAnglesKeepPrevAngles(double x, double y, double z, float yaw, float pitch)
    {
        LastTickX = PrevX = X = x;
        LastTickY = PrevY = Y = y + StandingEyeHeight;
        LastTickZ = PrevZ = Z = z;
        Yaw = yaw;
        Pitch = pitch;
        SetPosition(X, Y, Z);
    }

    public double GetSquaredDistance(double x, double y, double z)
    {
        double diffX = X - x;
        double diffY = Y - y;
        double diffZ = Z - z;
        return diffX * diffX + diffY * diffY + diffZ * diffZ;
    }

    public double GetSquaredDistance(Entity entity) => GetSquaredDistance(entity.X, entity.Y, entity.Z);

    public double GetDistance(double x, double y, double z) => MathHelper.Sqrt(GetSquaredDistance(x, y, z));

    public float GetDistance(Entity entity) => (float)GetDistance(entity.X, entity.Y, entity.Z);

    public virtual void OnPlayerInteraction(EntityPlayer player)
    {
    }

    public virtual void OnCollision(Entity entity)
    {
        if (Equals(entity.Passenger, this) || Equals(entity.Vehicle, this))
        {
            return;
        }

        double diffX = entity.X - X;
        double diffY = entity.Z - Z;
        double max = Math.Max(Math.Abs(diffX), Math.Abs(diffY));
        if (!(max >= 0.01F))
        {
            return;
        }

        max = MathHelper.Sqrt(max);
        diffX /= max;
        diffY /= max;
        double maxMulInverse = 1.0D / max;
        if (maxMulInverse > 1.0D)
        {
            maxMulInverse = 1.0D;
        }

        diffX *= maxMulInverse;
        diffY *= maxMulInverse;
        diffX *= 0.05F;
        diffY *= 0.05F;
        diffX *= 1.0F - PushSpeedReduction;
        diffY *= 1.0F - PushSpeedReduction;
        const double maxHorizontalImpulsePerCollision = 0.05D;
        const double maxHorizontalSpeed = 0.05D;
        if (diffX > maxHorizontalImpulsePerCollision)
        {
            diffX = maxHorizontalImpulsePerCollision;
        }
        else if (diffX < -maxHorizontalImpulsePerCollision)
        {
            diffX = -maxHorizontalImpulsePerCollision;
        }

        if (diffY > maxHorizontalImpulsePerCollision)
        {
            diffY = maxHorizontalImpulsePerCollision;
        }
        else if (diffY < -maxHorizontalImpulsePerCollision)
        {
            diffY = -maxHorizontalImpulsePerCollision;
        }

        double impulseMag = MathHelper.Sqrt(diffX * diffX + diffY * diffY);
        if (impulseMag > maxHorizontalImpulsePerCollision)
        {
            double s = maxHorizontalImpulsePerCollision / impulseMag;
            diffX *= s;
            diffY *= s;
        }

        AddVelocity(-diffX, 0.0D, -diffY);
        entity.AddVelocity(diffX, 0.0D, diffY);

        double speedThis = MathHelper.Sqrt(VelocityX * VelocityX + VelocityZ * VelocityZ);
        if (speedThis > maxHorizontalSpeed)
        {
            double s = maxHorizontalSpeed / speedThis;
            VelocityX *= s;
            VelocityZ *= s;
        }

        double speedOther = MathHelper.Sqrt(entity.VelocityX * entity.VelocityX + entity.VelocityZ * entity.VelocityZ);
        if (speedOther > maxHorizontalSpeed)
        {
            double s = maxHorizontalSpeed / speedOther;
            entity.VelocityX *= s;
            entity.VelocityZ *= s;
        }
    }

    public virtual void AddVelocity(double vx, double vy, double vz)
    {
        VelocityX += vx;
        VelocityY += vy;
        VelocityZ += vz;
    }

    protected void ScheduleVelocityUpdate() => VelocityModified = true;

    public virtual bool Damage(Entity? entity, int amount)
    {
        ScheduleVelocityUpdate();
        return false;
    }

    public virtual void UpdateKilledAchievement(Entity entity, int score)
    {
    }

    public virtual bool ShouldRender(Vec3D vec) => ShouldRender(GetSquaredDistance(vec.x, vec.y, vec.z));

    protected virtual bool ShouldRender(double sqDist)
    {
        double edgeLength = BoundingBox.AverageEdgeLength;
        edgeLength *= 64.0D * RenderDistanceWeight;
        return sqDist < edgeLength * edgeLength;
    }

    public bool SaveSelfNbt(NBTTagCompound nbt)
    {
        string? id = GetRegistryEntry();
        if (Dead || id == null)
        {
            return false;
        }

        nbt.SetString("id", id);
        Write(nbt);
        return true;
    }

    public void Write(NBTTagCompound nbt)
    {
        nbt.SetTag("Pos", newDoubleNbtList(X, Y + CameraOffset, Z));
        nbt.SetTag("Motion", newDoubleNbtList(VelocityX, VelocityY, VelocityZ));
        nbt.SetTag("Rotation", newFloatNbtList(Yaw, Pitch));
        nbt.SetFloat("FallDistance", FallDistance);
        nbt.SetShort("Fire", (short)FireTicks);
        nbt.SetShort("Air", (short)Air);
        nbt.SetBoolean("OnGround", OnGround);
        WriteNbt(nbt);
    }

    public void Read(NBTTagCompound nbt)
    {
        NBTTagList pos = nbt.GetTagList("Pos");
        NBTTagList mot = nbt.GetTagList("Motion");
        NBTTagList rot = nbt.GetTagList("Rotation");

        VelocityX = ((NBTTagDouble)mot.TagAt(0)).Value;
        VelocityY = ((NBTTagDouble)mot.TagAt(1)).Value;
        VelocityZ = ((NBTTagDouble)mot.TagAt(2)).Value;

        if (Math.Abs(VelocityX) > 10.0D)
        {
            VelocityX = 0.0D;
        }

        if (Math.Abs(VelocityY) > 10.0D)
        {
            VelocityY = 0.0D;
        }

        if (Math.Abs(VelocityZ) > 10.0D)
        {
            VelocityZ = 0.0D;
        }

        PrevX = LastTickX = X = ((NBTTagDouble)pos.TagAt(0)).Value;
        PrevY = LastTickY = Y = ((NBTTagDouble)pos.TagAt(1)).Value;
        PrevZ = LastTickZ = Z = ((NBTTagDouble)pos.TagAt(2)).Value;

        PrevYaw = Yaw = ((NBTTagFloat)rot.TagAt(0)).Value;
        PrevPitch = Pitch = ((NBTTagFloat)rot.TagAt(1)).Value;

        FallDistance = nbt.GetFloat("FallDistance");
        FireTicks = nbt.GetShort("Fire");
        Air = nbt.GetShort("Air");
        OnGround = nbt.GetBoolean("OnGround");

        SetPosition(X, Y, Z);
        SetRotation(Yaw, Pitch);
        ReadNbt(nbt);
    }

    private string? GetRegistryEntry() => Type?.Id;

    protected abstract void ReadNbt(NBTTagCompound nbt);

    protected abstract void WriteNbt(NBTTagCompound nbt);

    private static NBTTagList newDoubleNbtList(params double[] arr)
    {
        NBTTagList nbt = new();
        foreach (double t in arr)
        {
            nbt.SetTag(new NBTTagDouble(t));
        }

        return nbt;
    }

    private static NBTTagList newFloatNbtList(params float[] arr)
    {
        NBTTagList nbt = new();
        foreach (float t in arr)
        {
            nbt.SetTag(new NBTTagFloat(t));
        }

        return nbt;
    }

    public virtual float GetShadowRadius() => Height / 2.0F;

    protected void DropItem(int id, int count) => DropItem(id, count, 0.0F);

    protected EntityItem DropItem(int id, int count, float y) => DropItem(new ItemStack(id, count, 0), y);

    protected EntityItem DropItem(ItemStack stack, float y)
    {
        EntityItem item = new(World, X, Y + y, Z, stack)
        {
            DelayBeforeCanPickup = 10
        };
        World.SpawnEntity(item);
        return item;
    }

    public virtual bool IsInsideWall()
    {
        for (int i = 0; i < 8; ++i)
        {
            float offsetX = (((i >> 0) % 2) - 0.5F) * Width * 0.9F;
            float offsetY = (((i >> 1) % 2) - 0.5F) * 0.1F;
            float offsetZ = (((i >> 2) % 2) - 0.5F) * Width * 0.9F;
            int x = MathHelper.Floor(X + (double)offsetX);
            int y = MathHelper.Floor(Y + (double)EyeHeight + (double)offsetY);
            int z = MathHelper.Floor(Z + (double)offsetZ);
            if (World.Reader.ShouldSuffocate(x, y, z))
            {
                return true;
            }
        }

        return false;
    }

    public virtual bool Interact(EntityPlayer player) => false;

    public virtual Box? GetCollisionAgainstShape(Entity entity) => null;

    public virtual void TickRiding()
    {
        if (Vehicle is { Dead: true })
        {
            Vehicle = null;
            return;
        }

        VelocityX = 0.0D;
        VelocityY = 0.0D;
        VelocityZ = 0.0D;
        Tick();
        if (Vehicle == null) return;

        Vehicle.UpdatePassengerPosition();
        _vehicleYawDelta += Vehicle.Yaw - Vehicle.PrevYaw;

        _vehiclePitchDelta += Vehicle.Pitch - Vehicle.PrevPitch;

        while (_vehicleYawDelta >= 180.0D) _vehicleYawDelta -= 360.0D;
        while (_vehicleYawDelta < -180.0D) _vehicleYawDelta += 360.0D;
        while (_vehiclePitchDelta >= 180.0D) _vehiclePitchDelta -= 360.0D;
        while (_vehiclePitchDelta < -180.0D) _vehiclePitchDelta += 360.0D;

        double yawDelta = _vehicleYawDelta * 0.5D;
        double pitchDelta = _vehiclePitchDelta * 0.5D;
        const double limit = 10.0F;
        if (yawDelta > limit) yawDelta = limit;
        if (yawDelta < -limit) yawDelta = -limit;
        if (pitchDelta < -limit) pitchDelta = -limit;

        _vehicleYawDelta -= yawDelta;
        _vehiclePitchDelta -= pitchDelta;
        Yaw = (float)(Yaw + yawDelta);
        Pitch = (float)(Pitch + pitchDelta);
    }

    public virtual void UpdatePassengerPosition() => Passenger?.SetPosition(X, Y + PassengerRidingHeight + Passenger.StandingEyeHeight, Z);

    public virtual void SetVehicle(Entity? entity)
    {
        _vehiclePitchDelta = 0.0D;
        _vehicleYawDelta = 0.0D;
        if (entity == null)
        {
            if (Vehicle != null)
            {
                SetPositionAndAnglesKeepPrevAngles(Vehicle.X, Vehicle.BoundingBox.MinY + Vehicle.Height, Vehicle.Z, Yaw, Pitch);
                Vehicle.Passenger = null;
            }

            Vehicle = null;
        }
        else if (Equals(Vehicle, entity))
        {
            Vehicle.Passenger = null;
            Vehicle = null;
            SetPositionAndAnglesKeepPrevAngles(entity.X, entity.BoundingBox.MinY + entity.Height, entity.Z, Yaw, Pitch);
        }
        else
        {
            Vehicle?.Passenger = null;
            entity.Passenger?.Vehicle = null;
            Vehicle = entity;
            entity.Passenger = this;
        }
    }

    public virtual void SetPositionAndAnglesAvoidEntities(double x, double y, double z, float yaw, float pitch, int newPosRotationIncrements)
    {
        SetPosition(x, y, z);
        SetRotation(yaw, pitch);
        List<Box> collisions = World.Entities.GetEntityCollisionsScratch(this, BoundingBox.Contract(1.0D / 32.0D, 0.0D, 1.0D / 32.0D));
        if (collisions.Count <= 0) return;

        double maxMaxY = 0.0D;
        foreach (Box box in collisions)
        {
            if (box.MaxY > maxMaxY)
            {
                maxMaxY = box.MaxY;
            }
        }

        y += maxMaxY - BoundingBox.MinY;
        SetPosition(x, y, z);
    }

    public virtual void TickPortalCooldown()
    {
    }

    public virtual void SetVelocityClient(double vx, double vy, double vz)
    {
        VelocityX = vx;
        VelocityY = vy;
        VelocityZ = vz;
    }

    public virtual void ProcessServerEntityStatus(sbyte statusId)
    {
    }

    public virtual void AnimateHurt()
    {
    }

    public virtual void UpdateCloak()
    {
    }

    public virtual void SetEquipmentStack(int slotIndex, int itemId, int damage)
    {
    }

    public virtual bool IsSneaking() => GetFlag(1);

    public void SetSneaking(bool sneaking) => SetFlag(1, sneaking);

    private bool GetFlag(int index) => (_flags.Value & (1 << index)) != 0;

    private void SetFlag(int index, bool value)
    {
        byte oldValue = _flags.Value;
        byte newValue;
        if (value) newValue = (byte)(oldValue | (1 << index));
        else newValue = (byte)(oldValue & ~(1 << index));
        _flags.Value = newValue;
    }

    public virtual void OnStruckByLightning(EntityLightningBolt bolt)
    {
        Damage(5);
        ++FireTicks;
        if (FireTicks == 0) FireTicks = 300;
    }

    public virtual void OnKillOther(EntityLiving entityLiving)
    {
    }

    protected virtual bool PushOutOfBlocks(double x, double y, double z)
    {
        // Only players should attempt "push out of blocks".
        if (this is not EntityPlayer) return false;

        int floorX = MathHelper.Floor(x);
        int floorY = MathHelper.Floor(y);
        int floorZ = MathHelper.Floor(z);
        double fracX = x - floorX;
        double fracY = y - floorY;
        double fracZ = z - floorZ;
        if (!World.Reader.ShouldSuffocate(floorX, floorY, floorZ)) return false;

        bool canPushWest = !World.Reader.ShouldSuffocate(floorX - 1, floorY, floorZ);
        bool canPushEast = !World.Reader.ShouldSuffocate(floorX + 1, floorY, floorZ);
        bool canPushDown = !World.Reader.ShouldSuffocate(floorX, floorY - 1, floorZ);
        bool canPushUp = !World.Reader.ShouldSuffocate(floorX, floorY + 1, floorZ);
        bool canPushNorth = !World.Reader.ShouldSuffocate(floorX, floorY, floorZ - 1);
        bool canPushSouth = !World.Reader.ShouldSuffocate(floorX, floorY, floorZ + 1);
        int pushDirection = -1;
        double closestEdgeDistance = double.MaxValue;
        if (canPushWest && fracX < closestEdgeDistance)
        {
            closestEdgeDistance = fracX;
            pushDirection = 0;
        }

        if (canPushEast && 1.0D - fracX < closestEdgeDistance)
        {
            closestEdgeDistance = 1.0D - fracX;
            pushDirection = 1;
        }

        if (canPushDown && fracY < closestEdgeDistance)
        {
            closestEdgeDistance = fracY;
            pushDirection = 2;
        }

        if (canPushUp && 1.0D - fracY < closestEdgeDistance)
        {
            closestEdgeDistance = 1.0D - fracY;
            pushDirection = 3;
        }

        if (canPushNorth && fracZ < closestEdgeDistance)
        {
            closestEdgeDistance = fracZ;
            pushDirection = 4;
        }

        if (canPushSouth && 1.0D - fracZ < closestEdgeDistance)
        {
            pushDirection = 5;
        }

        float pushStrength = Random.NextFloat() * 0.2F + 0.1F;
        switch (pushDirection)
        {
            case 0:
                VelocityX = -pushStrength;
                break;
            case 1:
                VelocityX = pushStrength;
                break;
            case 2:
                VelocityY = -pushStrength;
                break;
            case 3:
                VelocityY = pushStrength;
                break;
            case 4:
                VelocityZ = -pushStrength;
                break;
            case 5:
                VelocityZ = pushStrength;
                break;
        }

        return false;
    }

    public override bool Equals(object? other) => other is Entity e && e.ID == ID;

    public override int GetHashCode() => ID;
}
