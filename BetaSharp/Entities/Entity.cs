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
    public abstract EntityType? Type { get; }
    private static int _nextEntityID;
    public int ID { get; set; } = _nextEntityID++;

    /// <summary>
    /// Muitipler for rendering, based of the render distance, 
    /// </summary>
    public double RenderDistanceWeight { get; set; } = 1.0D;

    /// <summary>
    /// Prevents another entity spawning near/in this entity.
    /// </summary>
    public bool PreventEntitySpawning { get; set; } = false;

    public Entity? Passenger { get; set; }
    public Entity? Vehicle { get; set; }
    public IWorldContext World { get; set; }
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
    public Box BoundingBox = new Box(0.0D, 0.0D, 0.0D, 0.0D, 0.0D, 0.0D);
    public bool OnGround { get; set; }

    /// <summary>
    /// If a collision occured in the X or Z directions.
    /// </summary>
    public bool HorizontalCollison { get; set; }

    /// <summary>
    /// If a collision occured in the Y direction.
    /// </summary>
    public bool VerticalCollision { get; set; }

    /// <summary>
    /// If a collision occured in either the X, Y, OR Z directions.
    /// </summary>
    public bool HasCollided { get; set; }

    public bool VelocityModified { get; set; }
    public bool Slowed { get; set; }
    public bool KeepVelocityOnCollision { get; set; } = true;
    public bool Dead { get; set; }

    public float StandingEyeHeight { get; set; } = 0.0F;
    public float Width { get; set; } = 0.6F;
    public float Height { get; set; } = 1.8F;
    public float PrevHorizontalSpeed { get; set; }
    public float HorizontalSpeed { get; set; }
    protected float FallDistance { get; set; }
    private int _nextStepSoundDistance = 1;
    public double LastTickX { get; set; }
    public double LastTickY { get; set; }
    public double LastTickZ { get; set; }
    public float CameraOffset { get; set; }
    public float StepHeight { get; set; } = 0.0F;
    public bool NoClip { get; set; } = false;
    public float PushSpeedReduction { get; set; } = 0.0F;
    protected JavaRandom Random { get; set; } = new();
    public int Age { get; set; } = 0;
    public int FireImmunityTicks { get; set; } = 1;
    public int FireTicks { get; set; }
    protected int MaxAir { get; set; } = 300;
    protected bool InWater { get; set; }
    public int Hearts { get; set; } = 0;
    public int Air { get; set; } = 300;
    private bool _firstTick = true;
    public string CloakUrl { get; set; }
    protected bool IsImmuneToFire { get; set; } = false;
    public DataSynchronizer DataSynchronizer { get; set; } = new();
    public float MinBrightness { get; set; } = 0.0F;
    private double _vehiclePitchDelta;
    private double _vehicleYawDelta;
    public bool IsPersistent { get; set; } = false;
    public int ChunkX { get; set; }
    public int ChunkSlice { get; set; }
    public int ChunkZ { get; set; }
    public int TrackedPosX { get; set; }
    public int TrackedPosY { get; set; }
    public int TrackedPosZ { get; set; }

    /// <summary>
    /// If a entity should render even IF its outside the viewing angle.
    /// </summary>
    public bool IgnoreFrustumCheck { get; set; }
    private readonly SyncedProperty<byte> _flags;

    public Entity(IWorldContext world)
    {
        this.World = world;
        SetPosition(0.0D, 0.0D, 0.0D);
        _flags = DataSynchronizer.MakeProperty<byte>(0, 0);
    }

    public Vec3D Position => new Vec3D(X, Y, Z);

    /// <summary>
    /// Keep moving up until theres no collison.
    /// </summary>
    /// <remarks>
    /// Note that the Pitch will be reset to 0, and the Motion
    /// will be fully zeroed, so the entity might fall for a bit
    /// if the position was off at the start.</remarks>
    public virtual void TeleportToTop()
    {
        if (World != null)
        {
            while (Y > 0.0D)
            {
                SetPosition(X, Y, Z);
                if (World.Entities.GetEntityCollisionsScratch(this, BoundingBox).Count == 0)
                {
                    break;
                }

                ++Y;
            }

            VelocityX = VelocityY = VelocityZ = 0.0D;
            Pitch = 0.0F;
        }
    }

    public virtual void MarkDead()
    {
        Dead = true;
    }

    protected virtual void SetBoundingBoxSpacing(float width, float height)
    {
        this.Width = width;
        this.Height = height;
    }

    protected void SetRotation(float yaw, float pitch)
    {
        this.Yaw = yaw % 360.0F;
        this.Pitch = pitch % 360.0F;
    }

    public void SetPosition(double x, double y, double z)
    {
        this.X = x;
        this.Y = y;
        this.Z = z;
        float halfWidth = Width / 2.0F;
        float height = this.Height;
        BoundingBox = new Box(x - (double)halfWidth, y - (double)StandingEyeHeight + (double)CameraOffset, z - (double)halfWidth, x + (double)halfWidth, y - (double)StandingEyeHeight + (double)CameraOffset + (double)height, z + (double)halfWidth);
    }

    /// <summary>
    /// Change the current look direction, with capping the pitch.
    /// </summary>
    public void ChangeLookDirection(float yaw, float pitch)
    {
        float oldPitch = this.Pitch;
        float oldYaw = this.Yaw;
        this.Yaw = (float)((double)this.Yaw + (double)yaw * 0.15D);
        this.Pitch = (float)((double)this.Pitch - (double)pitch * 0.15D);
        if (this.Pitch < -90.0F)
        {
            this.Pitch = -90.0F;
        }

        if (this.Pitch > 90.0F)
        {
            this.Pitch = 90.0F;
        }

        PrevPitch += this.Pitch - oldPitch;
        PrevYaw += this.Yaw - oldYaw;
    }

    public virtual void Tick()
    {
        BaseTick();
    }

    public virtual void BaseTick()
    {
        if (Vehicle != null && Vehicle.Dead)
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
                float volume = MathHelper.Sqrt(VelocityX * VelocityX * (double)0.2F + VelocityY * VelocityY + VelocityZ * VelocityZ * (double)0.2F) * 0.2F;
                if (volume > 1.0F)
                {
                    volume = 1.0F;
                }

                World.Broadcaster.PlaySoundAtEntity(this, "random.splash", volume, 1.0F + (Random.NextFloat() - Random.NextFloat()) * 0.4F);
                float floorMinY = (float)MathHelper.Floor(BoundingBox.MinY);

                double xOffset;
                double zOffset;
                for (int i = 0; (float)i < 1.0F + Width * 20.0F; ++i)
                {
                    xOffset = (Random.NextFloat() * 2.0F - 1.0F) * Width;
                    zOffset = (Random.NextFloat() * 2.0F - 1.0F) * Width;
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

        if (IsTouchingLava())
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

    protected void SetOnFire()
    {
        if (!IsImmuneToFire)
        {
            Damage((Entity)null, 4);
            FireTicks = 600;
        }

    }

    protected virtual void TickInVoid()
    {
        MarkDead();
    }

    public bool GetEntitiesInside(double x, double y, double z)
    {
        Box box = BoundingBox.Offset(x, y, z);
        List<Box> entitiesInbound = World.Entities.GetEntityCollisionsScratch(this, box);
        return entitiesInbound.Count > 0 ? false : !World.Reader.IsMaterialInBox(box, m => m.IsFluid);
    }

    /// <summary>
    /// Move by a certain amount, making sure to handle collisions and the such.
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
            this.X = (BoundingBox.MinX + BoundingBox.MaxX) / 2.0D;
            this.Y = BoundingBox.MinY + (double)StandingEyeHeight - (double)CameraOffset;
            this.Z = (BoundingBox.MinZ + BoundingBox.MaxZ) / 2.0D;
        }
        else
        {
            CameraOffset *= 0.4F;
            double mx = this.X;
            double my = this.Z;
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

            this.X = (BoundingBox.MinX + BoundingBox.MaxX) / 2.0D;
            this.Y = BoundingBox.MinY + (double)StandingEyeHeight - (double)CameraOffset;
            this.Z = (BoundingBox.MinZ + BoundingBox.MaxZ) / 2.0D;
            HorizontalCollison = originalX != x || originalZ != z;
            VerticalCollision = originalY != y;
            OnGround = originalY != y && originalY < 0.0D;
            HasCollided = HorizontalCollison || VerticalCollision;
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

            originalStepX = this.X - mx;
            originalStepZ = this.Z - my;
            int blockY;
            int blockX;
            int blockZ;
            if (BypassesSteppingEffects() && !sneakingOnGround && Vehicle == null)
            {
                HorizontalSpeed = (float)((double)HorizontalSpeed + (double)MathHelper.Sqrt(originalStepX * originalStepX + originalStepZ * originalStepZ) * 0.6D);

                if (OnGround)
                {
                    blockX = MathHelper.Floor(this.X);
                    blockY = MathHelper.Floor(this.Y - (double)0.2F - (double)StandingEyeHeight);
                    blockZ = MathHelper.Floor(this.Z);
                    blockId = World.Reader.GetBlockId(blockX, blockY, blockZ);
                    if (World.Reader.GetBlockId(blockX, blockY - 1, blockZ) == Block.Fence.ID)
                    {
                        blockId = World.Reader.GetBlockId(blockX, blockY - 1, blockZ);
                    }

                    if (HorizontalSpeed > (float)_nextStepSoundDistance && blockId > 0)
                    {
                        _nextStepSoundDistance = (int)HorizontalSpeed + 1;
                        BlockSoundGroup soundGroup = Block.Blocks[blockId].SoundGroup;
                        if (World.Reader.GetBlockId(blockX, blockY + 1, blockZ) == Block.Snow.ID)
                        {
                            soundGroup = Block.Snow.SoundGroup;
                            World.Broadcaster.PlaySoundAtEntity(this, soundGroup.StepSound, soundGroup.Volume * 0.15F, soundGroup.Pitch);
                        }
                        else if (!Block.Blocks[blockId].Material.IsFluid)
                        {
                            World.Broadcaster.PlaySoundAtEntity(this, soundGroup.StepSound, soundGroup.Volume * 0.15F, soundGroup.Pitch);
                        }

                        Block.Blocks[blockId].OnSteppedOn(new OnEntityStepEvent(World, this, blockX, blockY, blockZ));
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
                                Block.Blocks[collisionBlockId].OnEntityCollision(new OnEntityCollisionEvent(World, this, collisionX, collisionY, collisionZ));
                            }
                        }
                    }
                }
            }

            bool wet = IsWet();
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

    protected virtual bool BypassesSteppingEffects()
    {
        return true;
    }

    protected virtual void Fall(double fallDistance, bool onGround)
    {
        if (onGround)
        {
            if (this.FallDistance > 0.0F)
            {
                OnLanding(this.FallDistance);
                this.FallDistance = 0.0F;
            }
        }
        else if (fallDistance < 0.0D)
        {
            this.FallDistance = (float)((double)this.FallDistance - fallDistance);
        }

    }

    public virtual Box? GetBoundingBox()
    {
        return null;
    }

    protected virtual void Damage(int amt)
    {
        if (!IsImmuneToFire)
        {
            Damage(null, amt);
        }

    }

    protected virtual void OnLanding(float fallDistance)
    {
        if (Passenger != null)
        {
            Passenger.OnLanding(fallDistance);
        }

    }

    public bool IsWet()
    {
        return InWater || World.Environment.IsRainingAt(MathHelper.Floor(X), MathHelper.Floor(Y), MathHelper.Floor(Z));
    }

    public virtual bool IsInWater()
    {
        return InWater;
    }

    public virtual bool CheckWaterCollisions()
    {
        return World.Reader.UpdateMovementInFluid(BoundingBox.Expand(0.0D, (double)-0.4F, 0.0D).Contract(0.001D, 0.001D, 0.001D), Material.Water, this);
    }

    public bool IsInFluid(Material mat)
    {
        double eyeY = Y + (double)GetEyeHeight();
        int floorX = MathHelper.Floor(X);
        int floorEyeY = MathHelper.Floor((float)MathHelper.Floor(eyeY));
        int floorZ = MathHelper.Floor(Z);
        int id = World.Reader.GetBlockId(floorX, floorEyeY, floorZ);
        if (id != 0 && Block.Blocks[id].Material == mat)
        {
            float fluidHeight = BlockFluid.GetFluidHeightFromMeta(World.Reader.GetBlockMeta(floorX, floorEyeY, floorZ)) - 1.0F / 9.0F;
            float fluidSurfaceY = (float)(floorEyeY + 1) - fluidHeight;
            return eyeY < (double)fluidSurfaceY;
        }
        else
        {
            return false;
        }
    }

    public virtual float GetEyeHeight()
    {
        return 0.0F;
    }

    public bool IsTouchingLava()
    {
        return World.Reader.IsMaterialInBox(BoundingBox.Expand(-0.1F, -0.4F, -0.1F), m => m == Material.Lava);
    }

    public void MoveNonSolid(float strafe, float forward, float speed)
    {
        float inputLength = MathHelper.Sqrt(strafe * strafe + forward * forward);
        if (inputLength >= 0.01F)
        {
            if (inputLength < 1.0F)
            {
                inputLength = 1.0F;
            }

            inputLength = speed / inputLength;
            strafe *= inputLength;
            forward *= inputLength;
            float sinYaw = MathHelper.Sin(Yaw * (float)System.Math.PI / 180.0F);
            float cosYaw = MathHelper.Cos(Yaw * (float)System.Math.PI / 180.0F);
            VelocityX += (double)(strafe * cosYaw - forward * sinYaw);
            VelocityZ += (double)(forward * cosYaw + strafe * sinYaw);
        }
    }

    public virtual float GetBrightnessAtEyes(float tickDelta)
    {
        int floorX = MathHelper.Floor(X);
        double eyeOffset = (BoundingBox.MaxY - BoundingBox.MinY) * 0.66D;
        int floorY = MathHelper.Floor(Y - (double)StandingEyeHeight + eyeOffset);
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

        if (World.ChunkHost.IsRegionLoaded(minX, minY, minZ, maxX, maxY, maxZ))
        {
            float lum = World.Lighting.GetLuminance(floorX, floorY, floorZ);
            if (lum < MinBrightness)
            {
                lum = MinBrightness;
            }

            return lum;
        }
        else
        {
            return MinBrightness;
        }
    }

    public virtual void SetWorld(IWorldContext world)
    {
        this.World = world;
    }

    public void SetPositionAndAngles(double x, double y, double z, float yaw, float pitch)
    {
        PrevX = this.X = x;
        PrevY = this.Y = y;
        PrevZ = this.Z = z;
        PrevYaw = this.Yaw = yaw;
        PrevPitch = this.Pitch = pitch;
        CameraOffset = 0.0F;
        double diff = (double)(PrevYaw - yaw);
        if (diff < -180.0D)
        {
            PrevYaw += 360.0F;
        }

        if (diff >= 180.0D)
        {
            PrevYaw -= 360.0F;
        }

        SetPosition(this.X, this.Y, this.Z);
        SetRotation(yaw, pitch);
    }

    public void SetPositionAndAnglesKeepPrevAngles(double x, double y, double z, float yaw, float pitch)
    {
        LastTickX = PrevX = this.X = x;
        LastTickY = PrevY = this.Y = y + (double)StandingEyeHeight;
        LastTickZ = PrevZ = this.Z = z;
        this.Yaw = yaw;
        this.Pitch = pitch;
        SetPosition(this.X, this.Y, this.Z);
    }

    public double GetSquaredDistance(double x, double y, double z)
    {
        double diffX = X - x;
        double diffY = Y - y;
        double diffZ = Z - z;
        return diffX * diffX + diffY * diffY + diffZ * diffZ;
    }

    public double GetSquaredDistance(Entity entity) => GetSquaredDistance(entity.X, entity.Y, entity.Z);

    public double GetDistance(double x, double y, double z) => (double)MathHelper.Sqrt(GetSquaredDistance(x, y, z));

    public float GetDistance(Entity entity) => (float) GetDistance(entity.X, entity.Y, entity.Z);

    public virtual void OnPlayerInteraction(EntityPlayer player)
    {
    }

    public virtual void OnCollision(Entity entity)
    {
        if (entity.Passenger != this && entity.Vehicle != this)
        {
            double diffX = entity.X - X;
            double diffY = entity.Z - Z;
            double max = Math.Max(Math.Abs(diffX), Math.Abs(diffY));
            if (max >= (double)0.01F)
            {
                max = (double)MathHelper.Sqrt(max);
                diffX /= max;
                diffY /= max;
                double maxMulInverse = 1.0D / max;
                if (maxMulInverse > 1.0D)
                {
                    maxMulInverse = 1.0D;
                }

                diffX *= maxMulInverse;
                diffY *= maxMulInverse;
                diffX *= (double)0.05F;
                diffY *= (double)0.05F;
                diffX *= (double)(1.0F - PushSpeedReduction);
                diffY *= (double)(1.0F - PushSpeedReduction);
                const double maxHorizontalImpulsePerCollision = 0.05D;
                const double maxHorizontalSpeed = 0.05D;
                if (diffX > maxHorizontalImpulsePerCollision) diffX = maxHorizontalImpulsePerCollision;
                else if (diffX < -maxHorizontalImpulsePerCollision) diffX = -maxHorizontalImpulsePerCollision;

                if (diffY > maxHorizontalImpulsePerCollision) diffY = maxHorizontalImpulsePerCollision;
                else if (diffY < -maxHorizontalImpulsePerCollision) diffY = -maxHorizontalImpulsePerCollision;

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

        }
    }

    public virtual void AddVelocity(double vx, double vy, double vz)
    {
        VelocityX += vx;
        VelocityY += vy;
        VelocityZ += vz;
    }

    protected void ScheduleVelocityUpdate()
    {
        VelocityModified = true;
    }

    public virtual bool Damage(Entity? entity, int amount)
    {
        ScheduleVelocityUpdate();
        return false;
    }

    public virtual bool IsCollidable()
    {
        return false;
    }

    public virtual bool IsPushable()
    {
        return false;
    }

    public virtual void UpdateKilledAchievement(Entity entity, int score)
    {
    }

    public virtual bool ShouldRender(Vec3D vec)
    {
        double diffX = X - vec.x;
        double diffY = Y - vec.y;
        double diffZ = Z - vec.z;
        double squaredDistance = diffX * diffX + diffY * diffY + diffZ * diffZ;
        return ShouldRender(GetSquaredDistance(vec.x, vec.y, vec.z));
    }

    public virtual bool ShouldRender(double sqDist)
    {
        double renderDistance = BoundingBox.AverageEdgeLength;
        renderDistance *= 64.0D * RenderDistanceWeight;
        return sqDist < renderDistance * renderDistance;
    }

    public virtual string GetTexture()
    {
        return null;
    }

    public bool SaveSelfNbt(NBTTagCompound nbt)
    {
        string id = GetRegistryEntry();
        if (!Dead && id != null)
        {
            nbt.SetString("id", id);
            Write(nbt);
            return true;
        }
        else
        {
            return false;
        }
    }

    public void Write(NBTTagCompound nbt)
    {
        nbt.SetTag("Pos", NewDoubleNBTList(X, Y + (double)CameraOffset, Z));
        nbt.SetTag("Motion", NewDoubleNBTList(VelocityX, VelocityY, VelocityZ));
        nbt.SetTag("Rotation", NewFloatNBTList(Yaw, Pitch));
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

    protected string? GetRegistryEntry()
    {
        return Type?.Id;
    }

    public abstract void ReadNbt(NBTTagCompound nbt);

    public abstract void WriteNbt(NBTTagCompound nbt);

    protected static NBTTagList NewDoubleNBTList(params double[] arr)
    {
        NBTTagList nbt = new();

        for (int i = 0; i < arr.Length; ++i)
        {
            nbt.SetTag(new NBTTagDouble(arr[i]));
        }

        return nbt;
    }

    protected static NBTTagList NewFloatNBTList(params float[] arr)
    {
        NBTTagList nbt = new();

        for (int i = 0; i < arr.Length; ++i)
        {
            nbt.SetTag(new NBTTagFloat(arr[i]));
        }

        return nbt;
    }

    public virtual float GetShadowRadius()
    {
        return Height / 2.0F;
    }

    public EntityItem DropItem(int id, int count)
    {
        return DropItem(id, count, 0.0F);
    }

    public EntityItem DropItem(int id, int count, float y)
    {
        return DropItem(new ItemStack(id, count, 0), y);
    }

    public EntityItem DropItem(ItemStack stack, float y)
    {
        EntityItem item = new EntityItem(World, X, Y + (double)y, Z, stack);
        item.delayBeforeCanPickup = 10;
        World.SpawnEntity(item);
        return item;
    }

    public virtual bool IsAlive()
    {
        return !Dead;
    }

    public virtual bool IsInsideWall()
    {
        for (int i = 0; i < 8; ++i)
        {
            float offsetX = ((float)((i >> 0) % 2) - 0.5F) * Width * 0.9F;
            float offsetY = ((float)((i >> 1) % 2) - 0.5F) * 0.1F;
            float offsetZ = ((float)((i >> 2) % 2) - 0.5F) * Width * 0.9F;
            int x = MathHelper.Floor(X + (double)offsetX);
            int y = MathHelper.Floor(Y + (double)GetEyeHeight() + (double)offsetY);
            int z = MathHelper.Floor(Z + (double)offsetZ);
            if (World.Reader.ShouldSuffocate(x, y, z))
            {
                return true;
            }
        }

        return false;
    }

    public virtual bool Interact(EntityPlayer player)
    {
        return false;
    }

    public virtual Box? GetCollisionAgainstShape(Entity entity)
    {
        return null;
    }

    public virtual void TickRiding()
    {
        if (Vehicle.Dead)
        {
            Vehicle = null;
        }
        else
        {
            VelocityX = 0.0D;
            VelocityY = 0.0D;
            VelocityZ = 0.0D;
            Tick();
            if (Vehicle != null)
            {
                Vehicle.UpdatePassengerPosition();
                _vehicleYawDelta += (double)(Vehicle.Yaw - Vehicle.PrevYaw);

                for (_vehiclePitchDelta += (double)(Vehicle.Pitch - Vehicle.PrevPitch); _vehicleYawDelta >= 180.0D; _vehicleYawDelta -= 360.0D)
                {
                }

                while (_vehicleYawDelta < -180.0D)
                {
                    _vehicleYawDelta += 360.0D;
                }

                while (_vehiclePitchDelta >= 180.0D)
                {
                    _vehiclePitchDelta -= 360.0D;
                }

                while (_vehiclePitchDelta < -180.0D)
                {
                    _vehiclePitchDelta += 360.0D;
                }

                double yawDelta = _vehicleYawDelta * 0.5D;
                double pitchDelta = _vehiclePitchDelta * 0.5D;
                double limit = 10.0F;
                if (yawDelta > limit)
                {
                    yawDelta = limit;
                }

                if (yawDelta < -limit)
                {
                    yawDelta = -limit;
                }

                if (pitchDelta > limit)
                {
                    pitchDelta = limit;
                }

                if (pitchDelta < limit)
                {
                    pitchDelta = limit;
                }

                _vehicleYawDelta -= yawDelta;
                _vehiclePitchDelta -= pitchDelta;
                Yaw = (float)((double)Yaw + yawDelta);
                Pitch = (float)((double)Pitch + pitchDelta);
            }
        }
    }

    public virtual void UpdatePassengerPosition()
    {
        Passenger.SetPosition(X, Y + GetPassengerRidingHeight() + Passenger.GetStandingEyeHeight(), Z);
    }

    public virtual double GetStandingEyeHeight()
    {
        return (double)StandingEyeHeight;
    }

    public virtual double GetPassengerRidingHeight()
    {
        return (double)Height * 0.75D;
    }

    public virtual void SetVehicle(Entity entity)
    {
        _vehiclePitchDelta = 0.0D;
        _vehicleYawDelta = 0.0D;
        if (entity == null)
        {
            if (Vehicle != null)
            {
                SetPositionAndAnglesKeepPrevAngles(Vehicle.X, Vehicle.BoundingBox.MinY + (double)Vehicle.Height, Vehicle.Z, Yaw, Pitch);
                Vehicle.Passenger = null;
            }

            Vehicle = null;
        }
        else if (Vehicle == entity)
        {
            Vehicle.Passenger = null;
            Vehicle = null;
            SetPositionAndAnglesKeepPrevAngles(entity.X, entity.BoundingBox.MinY + (double)entity.Height, entity.Z, Yaw, Pitch);
        }
        else
        {
            if (Vehicle != null)
            {
                Vehicle.Passenger = null;
            }

            if (entity.Passenger != null)
            {
                entity.Passenger.Vehicle = null;
            }

            Vehicle = entity;
            entity.Passenger = this;
        }
    }

    public virtual void SetPositionAndAnglesAvoidEntities(double x, double y, double z, float yaw, float pitch, int newPosRotationIncrements)
    {
        SetPosition(x, y, z);
        SetRotation(yaw, pitch);
        var collisions = World.Entities.GetEntityCollisionsScratch(this, BoundingBox.Contract(1.0D / 32.0D, 0.0D, 1.0D / 32.0D));
        if (collisions.Count > 0)
        {
            double maxMaxY = 0.0D;

            for (int i = 0; i < collisions.Count; ++i)
            {
                Box box = collisions[i];
                if (box.MaxY > maxMaxY)
                {
                    maxMaxY = box.MaxY;
                }
            }

            y += maxMaxY - BoundingBox.MinY;
            SetPosition(x, y, z);
        }
    }

    public virtual float GetTargetingMargin()
    {
        return 0.1F;
    }

    public virtual Vec3D? GetLookVector()
    {
        return null;
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

    public virtual void ProcessServerEntityStatus(sbyte statusID)
    {
    }

    public virtual void AnimateHurt()
    {
    }

    public virtual void UpdateCloak()
    {
    }

    public virtual void SetEquipmentStack(int slotIndex, int itemID, int damage)
    {
    }

    public bool IsOnFire()
    {
        return FireTicks > 0 || GetFlag(0);
    }

    public bool HasVehicle()
    {
        return Vehicle != null || GetFlag(2);
    }

    public virtual ItemStack[] GetEquipment()
    {
        return null;
    }

    public virtual bool IsSneaking()
    {
        return GetFlag(1);
    }

    public void SetSneaking(bool sneaking)
    {
        SetFlag(1, sneaking);
    }

    protected bool GetFlag(int index)
    {
        return (_flags.Value & (1 << index)) != 0;
    }

    protected void SetFlag(int index, bool value)
    {
        byte oldValue = _flags.Value;
        byte newValue;
        if (value)
        {
            newValue = (byte)(oldValue | (1 << index));
        }
        else
        {
            newValue = (byte)(oldValue & ~(1 << index));
        }

        _flags.Value = newValue;
    }

    public virtual void OnStruckByLightning(EntityLightningBolt bolt)
    {
        Damage(5);
        ++FireTicks;
        if (FireTicks == 0)
        {
            FireTicks = 300;
        }

    }

    public virtual void OnKillOther(EntityLiving other)
    {
    }

    protected virtual bool PushOutOfBlocks(double x, double y, double z)
    {
        // Only players should attempt "push out of blocks".
        if (this is not EntityPlayer)
        {
            return false;
        }

        int floorX = MathHelper.Floor(x);
        int floorY = MathHelper.Floor(y);
        int floorZ = MathHelper.Floor(z);
        double fracX = x - floorX;
        double fracY = y - floorY;
        double fracZ = z - floorZ;
        if (World.Reader.ShouldSuffocate(floorX, floorY, floorZ))
        {
            bool canPushWest = !World.Reader.ShouldSuffocate(floorX - 1, floorY, floorZ);
            bool canPushEast = !World.Reader.ShouldSuffocate(floorX + 1, floorY, floorZ);
            bool canPushDown = !World.Reader.ShouldSuffocate(floorX, floorY - 1, floorZ);
            bool canPushUp = !World.Reader.ShouldSuffocate(floorX, floorY + 1, floorZ);
            bool canPushNorth = !World.Reader.ShouldSuffocate(floorX, floorY, floorZ - 1);
            bool canPushSouth = !World.Reader.ShouldSuffocate(floorX, floorY, floorZ + 1);
            int pushDirection = -1;
            double closestEdgeDistance = 9999.0D;
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
                closestEdgeDistance = 1.0D - fracZ;
                pushDirection = 5;
            }

            float pushStrength = Random.NextFloat() * 0.2F + 0.1F;
            if (pushDirection == 0)
            {
                VelocityX = (double)(-pushStrength);
            }

            if (pushDirection == 1)
            {
                VelocityX = (double)pushStrength;
            }

            if (pushDirection == 2)
            {
                VelocityY = (double)(-pushStrength);
            }

            if (pushDirection == 3)
            {
                VelocityY = (double)pushStrength;
            }

            if (pushDirection == 4)
            {
                VelocityZ = (double)(-pushStrength);
            }

            if (pushDirection == 5)
            {
                VelocityZ = (double)pushStrength;
            }
        }

        return false;
    }

    public override bool Equals(object other)
    {
        return other is Entity e && e.ID == ID;
    }

    public override int GetHashCode()
    {
        return ID;
    }
}
