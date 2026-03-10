using BetaSharp.Blocks;
using BetaSharp.Blocks.Materials;
using BetaSharp.Items;
using BetaSharp.NBT;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core;

namespace BetaSharp.Entities;

public abstract class Entity
{
    private static int nextEntityID;
    public IBlockWorldContext _level;
    public int age;
    public int air = 300;
    public Box boundingBox = new(0.0D, 0.0D, 0.0D, 0.0D, 0.0D, 0.0D);
    public float cameraOffset;
    public int chunkSlice;
    public int chunkX;
    public int chunkZ;
    public string cloakUrl;
    protected DataWatcher dataWatcher = new();
    public bool dead;
    protected float fallDistance;
    public int fireImmunityTicks = 1;
    public int fireTicks;
    private bool firstTick = true;
    public bool hasCollided;
    public int hearts = 0;
    public float height = 1.8F;
    public bool horizontalCollison;
    public float horizontalSpeed;
    public int id = nextEntityID++;
    public bool ignoreFrustumCheck;
    protected bool inWater;
    protected bool isImmuneToFire = false;
    public bool isPersistent = false;
    public bool keepVelocityOnCollision = true;
    public double lastTickX;
    public double lastTickY;
    public double lastTickZ;
    protected int maxAir = 300;
    public float minBrightness = 0.0F;
    protected int nextStepSoundDistance = 1;
    public bool noClip = false;
    public bool onGround;
    public Entity? passenger;
    public float pitch;
    public bool preventEntitySpawning = false;
    public float prevHorizontalSpeed;
    public float prevPitch;
    public double prevX;
    public double prevY;
    public float prevYaw;
    public double prevZ;
    public float pushSpeedReduction = 0.0F;
    protected JavaRandom random = new();
    public double renderDistanceWeight = 1.0D;
    public bool slowed;
    public float standingEyeHeight = 0.0F;
    public float stepHeight = 0.0F;
    public int trackedPosX;
    public int trackedPosY;
    public int trackedPosZ;
    public Entity? vehicle;
    private double vehiclePitchDelta;
    private double vehicleYawDelta;
    public bool velocityModified;
    public double velocityX;
    public double velocityY;
    public double velocityZ;
    public bool verticalCollision;
    public float width = 0.6F;
    public double x;
    public double y;
    public float yaw;
    public double z;

    public Entity(IBlockWorldContext level)
    {
        _level = level;
        setPosition(0.0D, 0.0D, 0.0D);
        dataWatcher.AddObject(0, (byte)0);
        initDataTracker();
    }

    public Vec3D Position => new(x, y, z);

    protected abstract void initDataTracker();

    public DataWatcher getDataWatcher() => dataWatcher;

    public override bool Equals(object other) => other is Entity e && e.id == id;

    public override int GetHashCode() => id;

    public virtual void teleportToTop()
    {
        if (_level != null)
        {
            while (y > 0.0D)
            {
                setPosition(x, y, z);
                if (_level.Entities.GetEntityCollisionsScratch(this, boundingBox).Count == 0)
                {
                    break;
                }

                ++y;
            }

            velocityX = velocityY = velocityZ = 0.0D;
            pitch = 0.0F;
        }
    }

    public virtual void markDead() => dead = true;

    protected virtual void setBoundingBoxSpacing(float width, float height)
    {
        this.width = width;
        this.height = height;
    }

    protected void setRotation(float yaw, float pitch)
    {
        this.yaw = yaw % 360.0F;
        this.pitch = pitch % 360.0F;
    }

    public void setPosition(double x, double y, double z)
    {
        this.x = x;
        this.y = y;
        this.z = z;
        float halfWidth = width / 2.0F;
        float height = this.height;
        boundingBox = new Box(x - halfWidth, y - standingEyeHeight + cameraOffset, z - halfWidth, x + halfWidth, y - standingEyeHeight + cameraOffset + height, z + halfWidth);
    }

    public void changeLookDirection(float yaw, float pitch)
    {
        float oldPitch = this.pitch;
        float oldYaw = this.yaw;
        this.yaw = (float)(this.yaw + yaw * 0.15D);
        this.pitch = (float)(this.pitch - pitch * 0.15D);
        if (this.pitch < -90.0F)
        {
            this.pitch = -90.0F;
        }

        if (this.pitch > 90.0F)
        {
            this.pitch = 90.0F;
        }

        prevPitch += this.pitch - oldPitch;
        prevYaw += this.yaw - oldYaw;
    }

    public virtual void tick() => baseTick();

    public virtual void baseTick()
    {
        if (vehicle != null && vehicle.dead)
        {
            vehicle = null;
        }

        ++age;
        prevHorizontalSpeed = horizontalSpeed;
        prevX = x;
        prevY = y;
        prevZ = z;
        prevPitch = pitch;
        prevYaw = yaw;
        if (checkWaterCollisions())
        {
            if (!inWater && !firstTick)
            {
                float var1 = MathHelper.Sqrt(velocityX * velocityX * 0.2F + velocityY * velocityY + velocityZ * velocityZ * 0.2F) * 0.2F;
                if (var1 > 1.0F)
                {
                    var1 = 1.0F;
                }

                _level.Broadcaster.PlaySoundAtEntity(this, "random.splash", var1, 1.0F + (random.NextFloat() - random.NextFloat()) * 0.4F);
                float var2 = MathHelper.Floor(boundingBox.MinY);

                int var3;
                float var4;
                float var5;
                for (var3 = 0; var3 < 1.0F + width * 20.0F; ++var3)
                {
                    var4 = (random.NextFloat() * 2.0F - 1.0F) * width;
                    var5 = (random.NextFloat() * 2.0F - 1.0F) * width;
                    _level.Broadcaster.AddParticle("bubble", x + var4, var2 + 1.0F, z + var5, velocityX, velocityY - random.NextFloat() * 0.2F, velocityZ);
                }

                for (var3 = 0; var3 < 1.0F + width * 20.0F; ++var3)
                {
                    var4 = (random.NextFloat() * 2.0F - 1.0F) * width;
                    var5 = (random.NextFloat() * 2.0F - 1.0F) * width;
                    _level.Broadcaster.AddParticle("splash", x + var4, var2 + 1.0F, z + var5, velocityX, velocityY, velocityZ);
                }
            }

            fallDistance = 0.0F;
            inWater = true;
            fireTicks = 0;
        }
        else
        {
            inWater = false;
        }

        if (_level.IsRemote)
        {
            fireTicks = 0;
        }
        else if (fireTicks > 0)
        {
            if (isImmuneToFire)
            {
                fireTicks -= 4;
                if (fireTicks < 0)
                {
                    fireTicks = 0;
                }
            }
            else
            {
                if (fireTicks % 20 == 0)
                {
                    damage(null, 1);
                }

                --fireTicks;
            }
        }

        if (isTouchingLava())
        {
            setOnFire();
        }

        if (y < -64.0D)
        {
            tickInVoid();
        }

        if (!_level.IsRemote)
        {
            setFlag(0, fireTicks > 0);
            setFlag(2, vehicle != null);
        }

        firstTick = false;
    }

    protected void setOnFire()
    {
        if (!isImmuneToFire)
        {
            damage(null, 4);
            fireTicks = 600;
        }
    }

    protected virtual void tickInVoid() => markDead();

    public bool getEntitiesInside(double x, double y, double z)
    {
        Box box = boundingBox.Offset(x, y, z);
        List<Box> entitiesInbound = _level.Entities.GetEntityCollisionsScratch(this, box);
        return entitiesInbound.Count > 0 ? false : !_level.BlocksReader.IsBoxSubmergedInFluid(box);
    }

    public virtual void move(double x, double y, double z)
    {
        if (noClip)
        {
            boundingBox.Translate(x, y, z);
            this.x = (boundingBox.MinX + boundingBox.MaxX) / 2.0D;
            this.y = boundingBox.MinY + standingEyeHeight - cameraOffset;
            this.z = (boundingBox.MinZ + boundingBox.MaxZ) / 2.0D;
        }
        else
        {
            cameraOffset *= 0.4F;
            double var7 = this.x;
            double var9 = this.z;
            if (slowed)
            {
                slowed = false;
                x *= 0.25D;
                y *= 0.05F;
                z *= 0.25D;
                velocityX = 0.0D;
                velocityY = 0.0D;
                velocityZ = 0.0D;
            }

            double var11 = x;
            double var13 = y;
            double var15 = z;
            Box var17 = boundingBox;
            bool var18 = onGround && isSneaking();
            if (var18)
            {
                double var19;
                for (var19 = 0.05D; x != 0.0D && _level.Entities.GetEntityCollisionsScratch(this, boundingBox.Offset(x, -1.0D, 0.0D)).Count == 0; var11 = x)
                {
                    if (x < var19 && x >= -var19)
                    {
                        x = 0.0D;
                    }
                    else if (x > 0.0D)
                    {
                        x -= var19;
                    }
                    else
                    {
                        x += var19;
                    }
                }

                for (; z != 0.0D && _level.Entities.GetEntityCollisionsScratch(this, boundingBox.Offset(0.0D, -1.0D, z)).Count == 0; var15 = z)
                {
                    if (z < var19 && z >= -var19)
                    {
                        z = 0.0D;
                    }
                    else if (z > 0.0D)
                    {
                        z -= var19;
                    }
                    else
                    {
                        z += var19;
                    }
                }
            }

            List<Box> entitiesInbound = _level.Entities.GetEntityCollisionsScratch(this, boundingBox.Stretch(x, y, z));

            for (int var20 = 0; var20 < entitiesInbound.Count; ++var20)
            {
                y = entitiesInbound[var20].GetYOffset(boundingBox, y);
            }

            boundingBox.Translate(0.0D, y, 0.0D);
            if (!keepVelocityOnCollision && var13 != y)
            {
                z = 0.0D;
                y = z;
                x = z;
            }

            bool var36 = onGround || (var13 != y && var13 < 0.0D);

            int i;
            for (i = 0; i < entitiesInbound.Count; ++i)
            {
                x = entitiesInbound[i].GetXOffset(boundingBox, x);
            }

            boundingBox.Translate(x, 0.0D, 0.0D);
            if (!keepVelocityOnCollision && var11 != x)
            {
                z = 0.0D;
                y = z;
                x = z;
            }

            for (i = 0; i < entitiesInbound.Count; ++i)
            {
                z = entitiesInbound[i].GetZOffset(boundingBox, z);
            }

            boundingBox.Translate(0.0D, 0.0D, z);
            if (!keepVelocityOnCollision && var15 != z)
            {
                z = 0.0D;
                y = z;
                x = z;
            }

            double var23;
            int var28;
            double var37;
            if (stepHeight > 0.0F && var36 && (var18 || cameraOffset < 0.05F) && (var11 != x || var15 != z))
            {
                var37 = x;
                var23 = y;
                double var25 = z;
                x = var11;
                y = stepHeight;
                z = var15;
                Box var27 = boundingBox;
                boundingBox = var17;
                entitiesInbound = _level.Entities.GetEntityCollisionsScratch(this, boundingBox.Stretch(var11, y, var15));

                for (var28 = 0; var28 < entitiesInbound.Count; ++var28)
                {
                    y = entitiesInbound[var28].GetYOffset(boundingBox, y);
                }

                boundingBox.Translate(0.0D, y, 0.0D);
                if (!keepVelocityOnCollision && var13 != y)
                {
                    z = 0.0D;
                    y = z;
                    x = z;
                }

                for (var28 = 0; var28 < entitiesInbound.Count; ++var28)
                {
                    x = entitiesInbound[var28].GetXOffset(boundingBox, x);
                }

                boundingBox.Translate(x, 0.0D, 0.0D);
                if (!keepVelocityOnCollision && var11 != x)
                {
                    z = 0.0D;
                    y = z;
                    x = z;
                }

                for (var28 = 0; var28 < entitiesInbound.Count; ++var28)
                {
                    z = entitiesInbound[var28].GetZOffset(boundingBox, z);
                }

                boundingBox.Translate(0.0D, 0.0D, z);
                if (!keepVelocityOnCollision && var15 != z)
                {
                    z = 0.0D;
                    y = z;
                    x = z;
                }

                if (!keepVelocityOnCollision && var13 != y)
                {
                    z = 0.0D;
                    y = z;
                    x = z;
                }
                else
                {
                    y = -stepHeight;

                    for (var28 = 0; var28 < entitiesInbound.Count; ++var28)
                    {
                        y = entitiesInbound[var28].GetYOffset(boundingBox, y);
                    }

                    boundingBox.Translate(0.0D, y, 0.0D);
                }

                if (var37 * var37 + var25 * var25 >= x * x + z * z)
                {
                    x = var37;
                    y = var23;
                    z = var25;
                    boundingBox = var27;
                }
                else
                {
                    double var41 = boundingBox.MinY - (int)boundingBox.MinY;
                    if (var41 > 0.0D)
                    {
                        cameraOffset = (float)(cameraOffset + var41 + 0.01D);
                    }
                }
            }

            this.x = (boundingBox.MinX + boundingBox.MaxX) / 2.0D;
            this.y = boundingBox.MinY + standingEyeHeight - cameraOffset;
            this.z = (boundingBox.MinZ + boundingBox.MaxZ) / 2.0D;
            horizontalCollison = var11 != x || var15 != z;
            verticalCollision = var13 != y;
            onGround = var13 != y && var13 < 0.0D;
            hasCollided = horizontalCollison || verticalCollision;
            fall(y, onGround);
            if (var11 != x)
            {
                velocityX = 0.0D;
            }

            if (var13 != y)
            {
                velocityY = 0.0D;
            }

            if (var15 != z)
            {
                velocityZ = 0.0D;
            }

            var37 = this.x - var7;
            var23 = this.z - var9;
            int var26;
            int var38;
            int var39;
            if (bypassesSteppingEffects() && !var18 && vehicle == null)
            {
                horizontalSpeed = (float)(horizontalSpeed + MathHelper.Sqrt(var37 * var37 + var23 * var23) * 0.6D);

                if (onGround)
                {
                    var38 = MathHelper.Floor(this.x);
                    var26 = MathHelper.Floor(this.y - 0.2F - standingEyeHeight);
                    var39 = MathHelper.Floor(this.z);
                    var28 = _level.BlocksReader.GetBlockId(var38, var26, var39);
                    if (_level.BlocksReader.GetBlockId(var38, var26 - 1, var39) == Block.Fence.id)
                    {
                        var28 = _level.BlocksReader.GetBlockId(var38, var26 - 1, var39);
                    }

                    if (horizontalSpeed > nextStepSoundDistance && var28 > 0)
                    {
                        ++nextStepSoundDistance;
                        BlockSoundGroup soundGroup = Block.Blocks[var28].soundGroup;
                        if (_level.BlocksReader.GetBlockId(var38, var26 + 1, var39) == Block.Snow.id)
                        {
                            soundGroup = Block.Snow.soundGroup;
                            _level.Broadcaster.PlaySoundAtEntity(this, soundGroup.StepSound, soundGroup.Volume * 0.3F, soundGroup.Pitch);
                        }
                        else if (!Block.Blocks[var28].material.IsFluid)
                        {
                            _level.Broadcaster.PlaySoundAtEntity(this, soundGroup.StepSound, soundGroup.Volume * 0.3F, soundGroup.Pitch);
                        }

                        Block.Blocks[var28].onSteppedOn(new OnEntityStepEvt(_level, this, var38, var26, var39));
                    }
                }
            }

            var38 = MathHelper.Floor(boundingBox.MinX + 0.001D);
            var26 = MathHelper.Floor(boundingBox.MinY + 0.001D);
            var39 = MathHelper.Floor(boundingBox.MinZ + 0.001D);
            var28 = MathHelper.Floor(boundingBox.MaxX - 0.001D);
            int var40 = MathHelper.Floor(boundingBox.MaxY - 0.001D);
            int var30 = MathHelper.Floor(boundingBox.MaxZ - 0.001D);
            if (_level.BlockHost.IsRegionLoaded(var38, var26, var39, var28, var40, var30))
            {
                for (int var31 = var38; var31 <= var28; ++var31)
                {
                    for (int var32 = var26; var32 <= var40; ++var32)
                    {
                        for (int var33 = var39; var33 <= var30; ++var33)
                        {
                            int var34 = _level.BlocksReader.GetBlockId(var31, var32, var33);
                            if (var34 > 0)
                            {
                                Block.Blocks[var34].onEntityCollision(new OnEntityCollisionEvt(_level, this, var31, var32, var33));
                            }
                        }
                    }
                }
            }

            bool var42 = isWet();
            if (_level.BlocksReader.IsFireOrLavaInBox(boundingBox.Contract(0.001D, 0.001D, 0.001D)))
            {
                damage(1);
                if (!var42)
                {
                    ++fireTicks;
                    if (fireTicks == 0)
                    {
                        fireTicks = 300;
                    }
                }
            }
            else if (fireTicks <= 0)
            {
                fireTicks = -fireImmunityTicks;
            }

            if (var42 && fireTicks > 0)
            {
                _level.Broadcaster.PlaySoundAtEntity(this, "random.fizz", 0.7F, 1.6F + (random.NextFloat() - random.NextFloat()) * 0.4F);
                fireTicks = -fireImmunityTicks;
            }
        }
    }

    protected virtual bool bypassesSteppingEffects() => true;

    protected virtual void fall(double fallDistance, bool onGround)
    {
        if (onGround)
        {
            if (this.fallDistance > 0.0F)
            {
                onLanding(this.fallDistance);
                this.fallDistance = 0.0F;
            }
        }
        else if (fallDistance < 0.0D)
        {
            this.fallDistance = (float)(this.fallDistance - fallDistance);
        }
    }

    public virtual Box? getBoundingBox() => null;

    protected virtual void damage(int var1)
    {
        if (!isImmuneToFire)
        {
            damage(null, var1);
        }
    }

    protected virtual void onLanding(float fallDistance)
    {
        if (passenger != null)
        {
            passenger.onLanding(fallDistance);
        }
    }

    public bool isWet() => inWater || _level.Environment.IsRainingAt(MathHelper.Floor(x), MathHelper.Floor(y), MathHelper.Floor(z));

    public virtual bool isInWater() => inWater;

    public virtual bool checkWaterCollisions() => _level.BlocksReader.UpdateMovementInFluid(boundingBox.Expand(0.0D, -0.4F, 0.0D).Contract(0.001D, 0.001D, 0.001D), Material.Water, this);

    public bool isInFluid(Material var1)
    {
        double var2 = y + getEyeHeight();
        int var4 = MathHelper.Floor(x);
        int var5 = MathHelper.Floor(MathHelper.Floor(var2));
        int var6 = MathHelper.Floor(z);
        int var7 = _level.BlocksReader.GetBlockId(var4, var5, var6);
        if (var7 != 0 && Block.Blocks[var7].material == var1)
        {
            float var8 = BlockFluid.getFluidHeightFromMeta(_level.BlocksReader.GetMeta(var4, var5, var6)) - 1.0F / 9.0F;
            float var9 = var5 + 1 - var8;
            return var2 < var9;
        }

        return false;
    }

    public virtual float getEyeHeight() => 0.0F;

    public bool isTouchingLava() => _level.BlocksReader.IsMaterialInBox(boundingBox.Expand(-0.1F, -0.4F, -0.1F), Material.Lava);

    public void moveNonSolid(float strafe, float forward, float speed)
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
            float sinYaw = MathHelper.Sin(yaw * (float)Math.PI / 180.0F);
            float cosYaw = MathHelper.Cos(yaw * (float)Math.PI / 180.0F);
            velocityX += strafe * cosYaw - forward * sinYaw;
            velocityZ += forward * cosYaw + strafe * sinYaw;
        }
    }

    public virtual float getBrightnessAtEyes(float var1)
    {
        int var2 = MathHelper.Floor(x);
        double var3 = (boundingBox.MaxY - boundingBox.MinY) * 0.66D;
        int var5 = MathHelper.Floor(y - standingEyeHeight + var3);
        int var6 = MathHelper.Floor(z);

        int minX = MathHelper.Floor(boundingBox.MinX);
        int minY = MathHelper.Floor(boundingBox.MinY);
        int minZ = MathHelper.Floor(boundingBox.MinZ);
        int maxX = MathHelper.Floor(boundingBox.MaxX);
        int maxY = MathHelper.Floor(boundingBox.MaxY);
        int maxZ = MathHelper.Floor(boundingBox.MaxZ);

        minY = Math.Min(127, Math.Max(0, minY));
        maxY = Math.Min(127, Math.Max(0, maxY));

        if (_level.BlockHost.IsRegionLoaded(minX, minY, minZ, maxX, maxY, maxZ))
        {
            float var7 = _level.Lighting.GetLuminance(var2, var5, var6);
            if (var7 < minBrightness)
            {
                var7 = minBrightness;
            }

            return var7;
        }

        return minBrightness;
    }

    public virtual void setWorld(IBlockWorldContext world) => _level = world;

    public void setPositionAndAngles(double x, double y, double z, float yaw, float pitch)
    {
        prevX = this.x = x;
        prevY = this.y = y;
        prevZ = this.z = z;
        prevYaw = this.yaw = yaw;
        prevPitch = this.pitch = pitch;
        cameraOffset = 0.0F;
        double var9 = prevYaw - yaw;
        if (var9 < -180.0D)
        {
            prevYaw += 360.0F;
        }

        if (var9 >= 180.0D)
        {
            prevYaw -= 360.0F;
        }

        setPosition(this.x, this.y, this.z);
        setRotation(yaw, pitch);
    }

    public void setPositionAndAnglesKeepPrevAngles(double x, double y, double z, float yaw, float pitch)
    {
        lastTickX = prevX = this.x = x;
        lastTickY = prevY = this.y = y + standingEyeHeight;
        lastTickZ = prevZ = this.z = z;
        this.yaw = yaw;
        this.pitch = pitch;
        setPosition(this.x, this.y, this.z);
    }

    public float getDistance(Entity entity)
    {
        float var2 = (float)(x - entity.x);
        float var3 = (float)(y - entity.y);
        float var4 = (float)(z - entity.z);
        return MathHelper.Sqrt(var2 * var2 + var3 * var3 + var4 * var4);
    }

    public double getSquaredDistance(double var1, double var3, double var5)
    {
        double var7 = x - var1;
        double var9 = y - var3;
        double var11 = z - var5;
        return var7 * var7 + var9 * var9 + var11 * var11;
    }

    public double getDistance(double var1, double var3, double var5)
    {
        double var7 = x - var1;
        double var9 = y - var3;
        double var11 = z - var5;
        return MathHelper.Sqrt(var7 * var7 + var9 * var9 + var11 * var11);
    }

    public double getSquaredDistance(Entity entity)
    {
        double var2 = x - entity.x;
        double var4 = y - entity.y;
        double var6 = z - entity.z;
        return var2 * var2 + var4 * var4 + var6 * var6;
    }

    public virtual void onPlayerInteraction(EntityPlayer player)
    {
    }

    public virtual void onCollision(Entity entity)
    {
        if (entity.passenger != this && entity.vehicle != this)
        {
            double var2 = entity.x - x;
            double var4 = entity.z - z;
            double var6 = Math.Max(Math.Abs(var2), Math.Abs(var4));
            if (var6 >= 0.01F)
            {
                var6 = MathHelper.Sqrt(var6);
                var2 /= var6;
                var4 /= var6;
                double var8 = 1.0D / var6;
                if (var8 > 1.0D)
                {
                    var8 = 1.0D;
                }

                var2 *= var8;
                var4 *= var8;
                var2 *= 0.05F;
                var4 *= 0.05F;
                var2 *= 1.0F - pushSpeedReduction;
                var4 *= 1.0F - pushSpeedReduction;
                addVelocity(-var2, 0.0D, -var4);
                entity.addVelocity(var2, 0.0D, var4);
            }
        }
    }

    public virtual void addVelocity(double var1, double var3, double var5)
    {
        velocityX += var1;
        velocityY += var3;
        velocityZ += var5;
    }

    protected void scheduleVelocityUpdate() => velocityModified = true;

    public virtual bool damage(Entity? entity, int amount)
    {
        scheduleVelocityUpdate();
        return false;
    }

    public virtual bool isCollidable() => false;

    public virtual bool isPushable() => false;

    public virtual void updateKilledAchievement(Entity entity, int var2)
    {
    }

    public virtual bool shouldRender(Vec3D var1)
    {
        double var2 = x - var1.x;
        double var4 = y - var1.y;
        double var6 = z - var1.z;
        double var8 = var2 * var2 + var4 * var4 + var6 * var6;
        return shouldRender(var8);
    }

    public virtual bool shouldRender(double var1)
    {
        double var3 = boundingBox.AverageEdgeLength;
        var3 *= 64.0D * renderDistanceWeight;
        return var1 < var3 * var3;
    }

    public virtual string getTexture() => null;

    public bool saveSelfNbt(NBTTagCompound nbt)
    {
        string var2 = getRegistryEntry();
        if (!dead && var2 != null)
        {
            nbt.SetString("id", var2);
            write(nbt);
            return true;
        }

        return false;
    }

    public void write(NBTTagCompound nbt)
    {
        nbt.SetTag("Pos", newDoubleNBTList(x, y + cameraOffset, z));
        nbt.SetTag("Motion", newDoubleNBTList(velocityX, velocityY, velocityZ));
        nbt.SetTag("Rotation", newFloatNBTList(yaw, pitch));
        nbt.SetFloat("FallDistance", fallDistance);
        nbt.SetShort("Fire", (short)fireTicks);
        nbt.SetShort("Air", (short)air);
        nbt.SetBoolean("OnGround", onGround);
        writeNbt(nbt);
    }

    public void read(NBTTagCompound nbt)
    {
        NBTTagList var2 = nbt.GetTagList("Pos");
        NBTTagList var3 = nbt.GetTagList("Motion");
        NBTTagList var4 = nbt.GetTagList("Rotation");
        velocityX = ((NBTTagDouble)var3.TagAt(0)).Value;
        velocityY = ((NBTTagDouble)var3.TagAt(1)).Value;
        velocityZ = ((NBTTagDouble)var3.TagAt(2)).Value;
        if (Math.Abs(velocityX) > 10.0D)
        {
            velocityX = 0.0D;
        }

        if (Math.Abs(velocityY) > 10.0D)
        {
            velocityY = 0.0D;
        }

        if (Math.Abs(velocityZ) > 10.0D)
        {
            velocityZ = 0.0D;
        }

        prevX = lastTickX = x = ((NBTTagDouble)var2.TagAt(0)).Value;
        prevY = lastTickY = y = ((NBTTagDouble)var2.TagAt(1)).Value;
        prevZ = lastTickZ = z = ((NBTTagDouble)var2.TagAt(2)).Value;
        prevYaw = yaw = ((NBTTagFloat)var4.TagAt(0)).Value;
        prevPitch = pitch = ((NBTTagFloat)var4.TagAt(1)).Value;
        fallDistance = nbt.GetFloat("FallDistance");
        fireTicks = nbt.GetShort("Fire");
        air = nbt.GetShort("Air");
        onGround = nbt.GetBoolean("OnGround");
        setPosition(x, y, z);
        setRotation(yaw, pitch);
        readNbt(nbt);
    }

    protected string getRegistryEntry() => EntityRegistry.GetId(this);

    public abstract void readNbt(NBTTagCompound nbt);

    public abstract void writeNbt(NBTTagCompound nbt);

    protected NBTTagList newDoubleNBTList(params double[] var1)
    {
        NBTTagList var2 = new();
        double[] var3 = var1;
        int var4 = var1.Length;

        for (int var5 = 0; var5 < var4; ++var5)
        {
            double var6 = var3[var5];
            var2.SetTag(new NBTTagDouble(var6));
        }

        return var2;
    }

    protected NBTTagList newFloatNBTList(params float[] var1)
    {
        NBTTagList var2 = new();
        float[] var3 = var1;
        int var4 = var1.Length;

        for (int var5 = 0; var5 < var4; ++var5)
        {
            float var6 = var3[var5];
            var2.SetTag(new NBTTagFloat(var6));
        }

        return var2;
    }

    public virtual float getShadowRadius() => height / 2.0F;

    public EntityItem dropItem(int var1, int var2) => dropItem(var1, var2, 0.0F);

    public EntityItem dropItem(int var1, int var2, float var3) => dropItem(new ItemStack(var1, var2, 0), var3);

    public EntityItem dropItem(ItemStack var1, float var2)
    {
        EntityItem var3 = new(_level, x, y + var2, z, var1);
        var3.delayBeforeCanPickup = 10;
        _level.SpawnEntity(var3);
        return var3;
    }

    public virtual bool isAlive() => !dead;

    public virtual bool isInsideWall()
    {
        for (int var1 = 0; var1 < 8; ++var1)
        {
            float var2 = ((var1 >> 0) % 2 - 0.5F) * width * 0.9F;
            float var3 = ((var1 >> 1) % 2 - 0.5F) * 0.1F;
            float var4 = ((var1 >> 2) % 2 - 0.5F) * width * 0.9F;
            int var5 = MathHelper.Floor(x + var2);
            int var6 = MathHelper.Floor(y + getEyeHeight() + var3);
            int var7 = MathHelper.Floor(z + var4);
            if (_level.BlocksReader.ShouldSuffocate(var5, var6, var7))
            {
                return true;
            }
        }

        return false;
    }

    public virtual bool interact(EntityPlayer player) => false;

    public virtual Box? getCollisionAgainstShape(Entity entity) => null;

    public virtual void tickRiding()
    {
        if (vehicle.dead)
        {
            vehicle = null;
        }
        else
        {
            velocityX = 0.0D;
            velocityY = 0.0D;
            velocityZ = 0.0D;
            tick();
            if (vehicle != null)
            {
                vehicle.updatePassengerPosition();
                vehicleYawDelta += vehicle.yaw - vehicle.prevYaw;

                for (vehiclePitchDelta += vehicle.pitch - vehicle.prevPitch; vehicleYawDelta >= 180.0D; vehicleYawDelta -= 360.0D)
                {
                }

                while (vehicleYawDelta < -180.0D)
                {
                    vehicleYawDelta += 360.0D;
                }

                while (vehiclePitchDelta >= 180.0D)
                {
                    vehiclePitchDelta -= 360.0D;
                }

                while (vehiclePitchDelta < -180.0D)
                {
                    vehiclePitchDelta += 360.0D;
                }

                double var1 = vehicleYawDelta * 0.5D;
                double var3 = vehiclePitchDelta * 0.5D;
                float var5 = 10.0F;
                if (var1 > var5)
                {
                    var1 = var5;
                }

                if (var1 < -var5)
                {
                    var1 = -var5;
                }

                if (var3 > var5)
                {
                    var3 = var5;
                }

                if (var3 < -var5)
                {
                    var3 = -var5;
                }

                vehicleYawDelta -= var1;
                vehiclePitchDelta -= var3;
                yaw = (float)(yaw + var1);
                pitch = (float)(pitch + var3);
            }
        }
    }

    public virtual void updatePassengerPosition() => passenger.setPosition(x, y + getPassengerRidingHeight() + passenger.getStandingEyeHeight(), z);

    public virtual double getStandingEyeHeight() => standingEyeHeight;

    public virtual double getPassengerRidingHeight() => height * 0.75D;

    public virtual void setVehicle(Entity? entity)
    {
        vehiclePitchDelta = 0.0D;
        vehicleYawDelta = 0.0D;
        if (entity == null)
        {
            if (vehicle != null)
            {
                setPositionAndAnglesKeepPrevAngles(vehicle.x, vehicle.boundingBox.MinY + vehicle.height, vehicle.z, yaw, pitch);
                vehicle.passenger = null;
            }

            vehicle = null;
        }
        else if (vehicle == entity)
        {
            vehicle.passenger = null;
            vehicle = null;
            setPositionAndAnglesKeepPrevAngles(entity.x, entity.boundingBox.MinY + entity.height, entity.z, yaw, pitch);
        }
        else
        {
            if (vehicle != null)
            {
                vehicle.passenger = null;
            }

            if (entity.passenger != null)
            {
                entity.passenger.vehicle = null;
            }

            vehicle = entity;
            entity.passenger = this;
        }
    }

    public virtual void setPositionAndAnglesAvoidEntities(double x, double y, double z, float var7, float var8, int var9)
    {
        setPosition(x, y, z);
        setRotation(var7, var8);
        var var10 = _level.Entities.GetEntityCollisionsScratch(this, boundingBox.Contract(1.0D / 32.0D, 0.0D, 1.0D / 32.0D));
        if (var10.Count > 0)
        {
            double var11 = 0.0D;

            for (int var13 = 0; var13 < var10.Count; ++var13)
            {
                Box var14 = var10[var13];
                if (var14.MaxY > var11)
                {
                    var11 = var14.MaxY;
                }
            }

            y += var11 - boundingBox.MinY;
            setPosition(x, y, z);
        }
    }

    public virtual float getTargetingMargin() => 0.1F;

    public virtual Vec3D? getLookVector() => null;

    public virtual void tickPortalCooldown()
    {
    }

    public virtual void setVelocityClient(double var1, double var3, double var5)
    {
        velocityX = var1;
        velocityY = var3;
        velocityZ = var5;
    }

    public virtual void processServerEntityStatus(sbyte var1)
    {
    }

    public virtual void animateHurt()
    {
    }

    public virtual void updateCloak()
    {
    }

    public virtual void setEquipmentStack(int var1, int var2, int var3)
    {
    }

    public bool isOnFire() => fireTicks > 0 || getFlag(0);

    public bool hasVehicle() => vehicle != null || getFlag(2);

    public virtual ItemStack[] getEquipment() => null;

    public virtual bool isSneaking() => getFlag(1);

    public void setSneaking(bool sneaking) => setFlag(1, sneaking);

    protected bool getFlag(int var1) => (dataWatcher.getWatchableObjectByte(0) & (1 << var1)) != 0;

    protected void setFlag(int var1, bool var2)
    {
        sbyte var3 = dataWatcher.getWatchableObjectByte(0);
        byte newValue;
        if (var2)
        {
            newValue = (byte)((byte)var3 | (1 << var1));
        }
        else
        {
            newValue = (byte)((byte)var3 & ~(1 << var1));
        }

        dataWatcher.UpdateObject(0, newValue);
    }

    public virtual void onStruckByLightning(EntityLightningBolt bolt)
    {
        damage(5);
        ++fireTicks;
        if (fireTicks == 0)
        {
            fireTicks = 300;
        }
    }

    public virtual void onKillOther(EntityLiving var1)
    {
    }

    protected virtual bool pushOutOfBlocks(double x, double y, double z)
    {
        int floorX = MathHelper.Floor(x);
        int floorY = MathHelper.Floor(y);
        int floorZ = MathHelper.Floor(z);
        double fracX = x - floorX;
        double fracY = y - floorY;
        double fracZ = z - floorZ;
        if (_level.BlocksReader.ShouldSuffocate(floorX, floorY, floorZ))
        {
            bool canPushWest = !_level.BlocksReader.ShouldSuffocate(floorX - 1, floorY, floorZ);
            bool canPushEast = !_level.BlocksReader.ShouldSuffocate(floorX + 1, floorY, floorZ);
            bool canPushDown = !_level.BlocksReader.ShouldSuffocate(floorX, floorY - 1, floorZ);
            bool canPushUp = !_level.BlocksReader.ShouldSuffocate(floorX, floorY + 1, floorZ);
            bool canPushNorth = !_level.BlocksReader.ShouldSuffocate(floorX, floorY, floorZ - 1);
            bool canPushSouth = !_level.BlocksReader.ShouldSuffocate(floorX, floorY, floorZ + 1);
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

            float pushStrength = random.NextFloat() * 0.2F + 0.1F;
            if (pushDirection == 0)
            {
                velocityX = -pushStrength;
            }

            if (pushDirection == 1)
            {
                velocityX = pushStrength;
            }

            if (pushDirection == 2)
            {
                velocityY = -pushStrength;
            }

            if (pushDirection == 3)
            {
                velocityY = pushStrength;
            }

            if (pushDirection == 4)
            {
                velocityZ = -pushStrength;
            }

            if (pushDirection == 5)
            {
                velocityZ = pushStrength;
            }
        }

        return false;
    }
}
