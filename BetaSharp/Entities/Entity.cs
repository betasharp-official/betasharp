using BetaSharp.Blocks;
using BetaSharp.Blocks.Materials;
using BetaSharp.Items;
using BetaSharp.NBT;
using BetaSharp.Util;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds;
using Math = System.Math;

namespace BetaSharp.Entities;

public abstract class Entity
{
    public abstract EntityType? Type { get; }
    private static int nextEntityID;
    public int id = nextEntityID++;
    public double renderDistanceWeight = 1.0D;
    public bool preventEntitySpawning = false;
    public Entity passenger;
    public Entity vehicle;
    public World world;
    public double prevX;
    public double prevY;
    public double prevZ;
    public double x;
    public double y;
    public double z;
    public double velocityX;
    public double velocityY;
    public double velocityZ;
    public float yaw;
    public float pitch;
    public float prevYaw;
    public float prevPitch;
    public Box boundingBox = new Box(0.0D, 0.0D, 0.0D, 0.0D, 0.0D, 0.0D);
    public bool onGround;
    public bool horizontalCollison;
    public bool verticalCollision;
    public bool hasCollided;
    public bool velocityModified;
    public bool slowed;
    public bool keepVelocityOnCollision = true;
    public bool dead;
    public float standingEyeHeight = 0.0F;
    public float width = 0.6F;
    public float height = 1.8F;
    public float prevHorizontalSpeed;
    public float horizontalSpeed;
    protected float fallDistance;
    private int nextStepSoundDistance = 1;
    public double lastTickX;
    public double lastTickY;
    public double lastTickZ;
    public float cameraOffset;
    public float stepHeight = 0.0F;
    public bool noClip = false;
    public float pushSpeedReduction = 0.0F;
    protected JavaRandom random = new();
    public int age = 0;
    public int fireImmunityTicks = 1;
    public int fireTicks;
    protected int maxAir = 300;
    protected bool inWater;
    public int hearts = 0;
    public int air = 300;
    private bool firstTick = true;
    public string cloakUrl;
    protected bool isImmuneToFire = false;
    public DataSynchronizer DataSynchronizer = new();
    public float minBrightness = 0.0F;
    private double vehiclePitchDelta;
    private double vehicleYawDelta;
    public bool isPersistent = false;
    public int chunkX;
    public int chunkSlice;
    public int chunkZ;
    public int trackedPosX;
    public int trackedPosY;
    public int trackedPosZ;
    public bool ignoreFrustumCheck;
    private readonly SyncedProperty<byte> Flags;

    public Entity(World world)
    {
        this.world = world;
        setPosition(0.0D, 0.0D, 0.0D);
        Flags = DataSynchronizer.MakeProperty<byte>(0, 0);
    }

    public Vec3D Position => new Vec3D(x, y, z);

    public virtual void teleportToTop()
    {
        if (world != null)
        {
            while (y > 0.0D)
            {
                setPosition(x, y, z);
                if (world.GetEntityCollisionsScratch(this, boundingBox).Count == 0)
                {
                    break;
                }

                ++y;
            }

            velocityX = velocityY = velocityZ = 0.0D;
            pitch = 0.0F;
        }
    }

    public virtual void markDead()
    {
        dead = true;
    }

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
        boundingBox = new Box(x - (double)halfWidth, y - (double)standingEyeHeight + (double)cameraOffset, z - (double)halfWidth, x + (double)halfWidth, y - (double)standingEyeHeight + (double)cameraOffset + (double)height, z + (double)halfWidth);
    }

    public void changeLookDirection(float yaw, float pitch)
    {
        float oldPitch = this.pitch;
        float oldYaw = this.yaw;
        this.yaw = (float)((double)this.yaw + (double)yaw * 0.15D);
        this.pitch = (float)((double)this.pitch - (double)pitch * 0.15D);
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

    public virtual void tick()
    {
        baseTick();
    }

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
                float var1 = MathHelper.Sqrt(velocityX * velocityX * (double)0.2F + velocityY * velocityY + velocityZ * velocityZ * (double)0.2F) * 0.2F;
                if (var1 > 1.0F)
                {
                    var1 = 1.0F;
                }

                world.playSound(this, "random.splash", var1, 1.0F + (random.NextFloat() - random.NextFloat()) * 0.4F);
                float var2 = (float)MathHelper.Floor(boundingBox.MinY);

                int var3;
                float var4;
                float var5;
                for (var3 = 0; (float)var3 < 1.0F + width * 20.0F; ++var3)
                {
                    var4 = (random.NextFloat() * 2.0F - 1.0F) * width;
                    var5 = (random.NextFloat() * 2.0F - 1.0F) * width;
                    world.addParticle("bubble", x + (double)var4, (double)(var2 + 1.0F), z + (double)var5, velocityX, velocityY - (double)(random.NextFloat() * 0.2F), velocityZ);
                }

                for (var3 = 0; (float)var3 < 1.0F + width * 20.0F; ++var3)
                {
                    var4 = (random.NextFloat() * 2.0F - 1.0F) * width;
                    var5 = (random.NextFloat() * 2.0F - 1.0F) * width;
                    world.addParticle("splash", x + (double)var4, (double)(var2 + 1.0F), z + (double)var5, velocityX, velocityY, velocityZ);
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

        if (world.isRemote)
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
                    damage((Entity)null, 1);
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

        if (!world.isRemote)
        {
            SetFlag(0, fireTicks > 0);
            SetFlag(2, vehicle != null);
        }

        firstTick = false;
    }

    protected void setOnFire()
    {
        if (!isImmuneToFire)
        {
            damage((Entity)null, 4);
            fireTicks = 600;
        }

    }

    protected virtual void tickInVoid()
    {
        markDead();
    }

    public bool getEntitiesInside(double x, double y, double z)
    {
        Box box = boundingBox.Offset(x, y, z);
        var entitiesInbound = world.GetEntityCollisionsScratch(this, box);
        return entitiesInbound.Count > 0 ? false : !world.isBoxSubmergedInFluid(box);
    }

    public virtual void move(double x, double y, double z)
    {
        if (noClip)
        {
            boundingBox.Translate(x, y, z);
            this.x = (boundingBox.MinX + boundingBox.MaxX) / 2.0D;
            this.y = boundingBox.MinY + (double)standingEyeHeight - (double)cameraOffset;
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
                y *= (double)0.05F;
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
                for (var19 = 0.05D; x != 0.0D && world.GetEntityCollisionsScratch(this, boundingBox.Offset(x, -1.0D, 0.0D)).Count == 0; var11 = x)
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

                for (; z != 0.0D && world.GetEntityCollisionsScratch(this, boundingBox.Offset(0.0D, -1.0D, z)).Count == 0; var15 = z)
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

            var entitiesInbound = world.GetEntityCollisionsScratch(this, boundingBox.Stretch(x, y, z));

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

            bool var36 = onGround || var13 != y && var13 < 0.0D;

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
                y = (double)stepHeight;
                z = var15;
                Box var27 = boundingBox;
                boundingBox = var17;
                entitiesInbound = world.GetEntityCollisionsScratch(this, boundingBox.Stretch(var11, y, var15));

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
                    y = (double)(-stepHeight);

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
                    double var41 = boundingBox.MinY - (double)((int)boundingBox.MinY);
                    if (var41 > 0.0D)
                    {
                        cameraOffset = (float)((double)cameraOffset + var41 + 0.01D);
                    }
                }
            }

            this.x = (boundingBox.MinX + boundingBox.MaxX) / 2.0D;
            this.y = boundingBox.MinY + (double)standingEyeHeight - (double)cameraOffset;
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
                horizontalSpeed = (float)((double)horizontalSpeed + (double)MathHelper.Sqrt(var37 * var37 + var23 * var23) * 0.6D);

                if (onGround)
                {
                    var38 = MathHelper.Floor(this.x);
                    var26 = MathHelper.Floor(this.y - (double)0.2F - (double)standingEyeHeight);
                    var39 = MathHelper.Floor(this.z);
                    var28 = world.getBlockId(var38, var26, var39);
                    if (world.getBlockId(var38, var26 - 1, var39) == Block.Fence.id)
                    {
                        var28 = world.getBlockId(var38, var26 - 1, var39);
                    }

                    if (horizontalSpeed > (float)nextStepSoundDistance && var28 > 0)
                    {
                        nextStepSoundDistance = (int)horizontalSpeed + 1;
                        BlockSoundGroup soundGroup = Block.Blocks[var28].soundGroup;
                        if (world.getBlockId(var38, var26 + 1, var39) == Block.Snow.id)
                        {
                            soundGroup = Block.Snow.soundGroup;
                            world.playSound(this, soundGroup.StepSound, soundGroup.Volume * 0.15F, soundGroup.Pitch);
                        }
                        else if (!Block.Blocks[var28].material.IsFluid)
                        {
                            world.playSound(this, soundGroup.StepSound, soundGroup.Volume * 0.15F, soundGroup.Pitch);
                        }

                        Block.Blocks[var28].onSteppedOn(world, var38, var26, var39, this);
                    }
                }
            }

            var38 = MathHelper.Floor(boundingBox.MinX + 0.001D);
            var26 = MathHelper.Floor(boundingBox.MinY + 0.001D);
            var39 = MathHelper.Floor(boundingBox.MinZ + 0.001D);
            var28 = MathHelper.Floor(boundingBox.MaxX - 0.001D);
            int var40 = MathHelper.Floor(boundingBox.MaxY - 0.001D);
            int var30 = MathHelper.Floor(boundingBox.MaxZ - 0.001D);
            if (world.isRegionLoaded(var38, var26, var39, var28, var40, var30))
            {
                for (int var31 = var38; var31 <= var28; ++var31)
                {
                    for (int var32 = var26; var32 <= var40; ++var32)
                    {
                        for (int var33 = var39; var33 <= var30; ++var33)
                        {
                            int var34 = world.getBlockId(var31, var32, var33);
                            if (var34 > 0)
                            {
                                Block.Blocks[var34].onEntityCollision(world, var31, var32, var33, this);
                            }
                        }
                    }
                }
            }

            bool var42 = isWet();
            if (world.isFireOrLavaInBox(boundingBox.Contract(0.001D, 0.001D, 0.001D)))
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
                world.playSound(this, "random.fizz", 0.7F, 1.6F + (random.NextFloat() - random.NextFloat()) * 0.4F);
                fireTicks = -fireImmunityTicks;
            }

        }
    }

    protected virtual bool bypassesSteppingEffects()
    {
        return true;
    }

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
            this.fallDistance = (float)((double)this.fallDistance - fallDistance);
        }

    }

    public virtual Box? getBoundingBox()
    {
        return null;
    }

    protected virtual void damage(int var1)
    {
        if (!isImmuneToFire)
        {
            damage((Entity)null, var1);
        }

    }

    protected virtual void onLanding(float fallDistance)
    {
        if (passenger != null)
        {
            passenger.onLanding(fallDistance);
        }

    }

    public bool isWet()
    {
        return inWater || world.isRaining(MathHelper.Floor(x), MathHelper.Floor(y), MathHelper.Floor(z));
    }

    public virtual bool isInWater()
    {
        return inWater;
    }

    public virtual bool checkWaterCollisions()
    {
        return world.updateMovementInFluid(boundingBox.Expand(0.0D, (double)-0.4F, 0.0D).Contract(0.001D, 0.001D, 0.001D), Material.Water, this);
    }

    public bool isInFluid(Material var1)
    {
        double var2 = y + (double)getEyeHeight();
        int var4 = MathHelper.Floor(x);
        int var5 = MathHelper.Floor((float)MathHelper.Floor(var2));
        int var6 = MathHelper.Floor(z);
        int var7 = world.getBlockId(var4, var5, var6);
        if (var7 != 0 && Block.Blocks[var7].material == var1)
        {
            float var8 = BlockFluid.getFluidHeightFromMeta(world.getBlockMeta(var4, var5, var6)) - 1.0F / 9.0F;
            float var9 = (float)(var5 + 1) - var8;
            return var2 < (double)var9;
        }
        else
        {
            return false;
        }
    }

    public virtual float getEyeHeight()
    {
        return 0.0F;
    }

    public bool isTouchingLava()
    {
        return world.isMaterialInBox(boundingBox.Expand((double)-0.1F, (double)-0.4F, (double)-0.1F), Material.Lava);
    }

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
            float sinYaw = MathHelper.Sin(yaw * (float)System.Math.PI / 180.0F);
            float cosYaw = MathHelper.Cos(yaw * (float)System.Math.PI / 180.0F);
            velocityX += (double)(strafe * cosYaw - forward * sinYaw);
            velocityZ += (double)(forward * cosYaw + strafe * sinYaw);
        }
    }

    public virtual float getBrightnessAtEyes(float var1)
    {
        int var2 = MathHelper.Floor(x);
        double var3 = (boundingBox.MaxY - boundingBox.MinY) * 0.66D;
        int var5 = MathHelper.Floor(y - (double)standingEyeHeight + var3);
        int var6 = MathHelper.Floor(z);

        int minX = MathHelper.Floor(boundingBox.MinX);
        int minY = MathHelper.Floor(boundingBox.MinY);
        int minZ = MathHelper.Floor(boundingBox.MinZ);
        int maxX = MathHelper.Floor(boundingBox.MaxX);
        int maxY = MathHelper.Floor(boundingBox.MaxY);
        int maxZ = MathHelper.Floor(boundingBox.MaxZ);

        minY = Math.Min(127, Math.Max(0, minY));
        maxY = Math.Min(127, Math.Max(0, maxY));

        if (world.isRegionLoaded(minX, minY, minZ, maxX, maxY, maxZ))
        {
            float var7 = world.getLuminance(var2, var5, var6);
            if (var7 < minBrightness)
            {
                var7 = minBrightness;
            }

            return var7;
        }
        else
        {
            return minBrightness;
        }
    }

    public virtual void setWorld(World world)
    {
        this.world = world;
    }

    public void setPositionAndAngles(double x, double y, double z, float yaw, float pitch)
    {
        prevX = this.x = x;
        prevY = this.y = y;
        prevZ = this.z = z;
        prevYaw = this.yaw = yaw;
        prevPitch = this.pitch = pitch;
        cameraOffset = 0.0F;
        double var9 = (double)(prevYaw - yaw);
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
        lastTickY = prevY = this.y = y + (double)standingEyeHeight;
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
        return (double)MathHelper.Sqrt(var7 * var7 + var9 * var9 + var11 * var11);
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
            if (var6 >= (double)0.01F)
            {
                var6 = (double)MathHelper.Sqrt(var6);
                var2 /= var6;
                var4 /= var6;
                double var8 = 1.0D / var6;
                if (var8 > 1.0D)
                {
                    var8 = 1.0D;
                }

                var2 *= var8;
                var4 *= var8;
                var2 *= (double)0.05F;
                var4 *= (double)0.05F;
                var2 *= (double)(1.0F - pushSpeedReduction);
                var4 *= (double)(1.0F - pushSpeedReduction);
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

    protected void scheduleVelocityUpdate()
    {
        velocityModified = true;
    }

    public virtual bool damage(Entity entity, int amount)
    {
        scheduleVelocityUpdate();
        return false;
    }

    public virtual bool isCollidable()
    {
        return false;
    }

    public virtual bool isPushable()
    {
        return false;
    }

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

    public virtual string getTexture()
    {
        return null;
    }

    public bool saveSelfNbt(NBTTagCompound nbt)
    {
        string var2 = getRegistryEntry();
        if (!dead && var2 != null)
        {
            nbt.SetString("id", var2);
            write(nbt);
            return true;
        }
        else
        {
            return false;
        }
    }

    public void write(NBTTagCompound nbt)
    {
        nbt.SetTag("Pos", newDoubleNBTList(x, y + (double)cameraOffset, z));
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
        if (System.Math.Abs(velocityX) > 10.0D)
        {
            velocityX = 0.0D;
        }

        if (System.Math.Abs(velocityY) > 10.0D)
        {
            velocityY = 0.0D;
        }

        if (System.Math.Abs(velocityZ) > 10.0D)
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

    protected string getRegistryEntry()
    {
        return EntityRegistry.GetId(this);
    }

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

    public virtual float getShadowRadius()
    {
        return height / 2.0F;
    }

    public EntityItem dropItem(int var1, int var2)
    {
        return dropItem(var1, var2, 0.0F);
    }

    public EntityItem dropItem(int var1, int var2, float var3)
    {
        return dropItem(new ItemStack(var1, var2, 0), var3);
    }

    public EntityItem dropItem(ItemStack var1, float var2)
    {
        EntityItem var3 = new EntityItem(world, x, y + (double)var2, z, var1);
        var3.delayBeforeCanPickup = 10;
        world.SpawnEntity(var3);
        return var3;
    }

    public virtual bool isAlive()
    {
        return !dead;
    }

    public virtual bool isInsideWall()
    {
        for (int var1 = 0; var1 < 8; ++var1)
        {
            float var2 = ((float)((var1 >> 0) % 2) - 0.5F) * width * 0.9F;
            float var3 = ((float)((var1 >> 1) % 2) - 0.5F) * 0.1F;
            float var4 = ((float)((var1 >> 2) % 2) - 0.5F) * width * 0.9F;
            int var5 = MathHelper.Floor(x + (double)var2);
            int var6 = MathHelper.Floor(y + (double)getEyeHeight() + (double)var3);
            int var7 = MathHelper.Floor(z + (double)var4);
            if (world.shouldSuffocate(var5, var6, var7))
            {
                return true;
            }
        }

        return false;
    }

    public virtual bool interact(EntityPlayer player)
    {
        return false;
    }

    public virtual Box? getCollisionAgainstShape(Entity entity)
    {
        return null;
    }

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
                vehicleYawDelta += (double)(vehicle.yaw - vehicle.prevYaw);

                for (vehiclePitchDelta += (double)(vehicle.pitch - vehicle.prevPitch); vehicleYawDelta >= 180.0D; vehicleYawDelta -= 360.0D)
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
                if (var1 > (double)var5)
                {
                    var1 = (double)var5;
                }

                if (var1 < (double)(-var5))
                {
                    var1 = (double)(-var5);
                }

                if (var3 > (double)var5)
                {
                    var3 = (double)var5;
                }

                if (var3 < (double)(-var5))
                {
                    var3 = (double)(-var5);
                }

                vehicleYawDelta -= var1;
                vehiclePitchDelta -= var3;
                yaw = (float)((double)yaw + var1);
                pitch = (float)((double)pitch + var3);
            }
        }
    }

    public virtual void updatePassengerPosition()
    {
        passenger.setPosition(x, y + getPassengerRidingHeight() + passenger.getStandingEyeHeight(), z);
    }

    public virtual double getStandingEyeHeight()
    {
        return (double)standingEyeHeight;
    }

    public virtual double getPassengerRidingHeight()
    {
        return (double)height * 0.75D;
    }

    public virtual void setVehicle(Entity entity)
    {
        vehiclePitchDelta = 0.0D;
        vehicleYawDelta = 0.0D;
        if (entity == null)
        {
            if (vehicle != null)
            {
                setPositionAndAnglesKeepPrevAngles(vehicle.x, vehicle.boundingBox.MinY + (double)vehicle.height, vehicle.z, yaw, pitch);
                vehicle.passenger = null;
            }

            vehicle = null;
        }
        else if (vehicle == entity)
        {
            vehicle.passenger = null;
            vehicle = null;
            setPositionAndAnglesKeepPrevAngles(entity.x, entity.boundingBox.MinY + (double)entity.height, entity.z, yaw, pitch);
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
        var var10 = world.GetEntityCollisionsScratch(this, boundingBox.Contract(1.0D / 32.0D, 0.0D, 1.0D / 32.0D));
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

    public virtual float getTargetingMargin()
    {
        return 0.1F;
    }

    public virtual Vec3D? getLookVector()
    {
        return null;
    }

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

    public bool isOnFire()
    {
        return fireTicks > 0 || GetFlag(0);
    }

    public bool hasVehicle()
    {
        return vehicle != null || GetFlag(2);
    }

    public virtual ItemStack[] getEquipment()
    {
        return null;
    }

    public virtual bool isSneaking()
    {
        return GetFlag(1);
    }

    public void setSneaking(bool sneaking)
    {
        SetFlag(1, sneaking);
    }

    protected bool GetFlag(int index)
    {
        return (Flags.Value & (1 << index)) != 0;
    }

    protected void SetFlag(int index, bool value)
    {
        byte oldValue = Flags.Value;
        byte newValue;
        if (value)
        {
            newValue = (byte)(oldValue | (1 << index));
        }
        else
        {
            newValue = (byte)(oldValue & ~(1 << index));
        }

        Flags.Value = newValue;
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
        double fracX = x - (double)floorX;
        double fracY = y - (double)floorY;
        double fracZ = z - (double)floorZ;
        if (world.shouldSuffocate(floorX, floorY, floorZ))
        {
            bool canPushWest = !world.shouldSuffocate(floorX - 1, floorY, floorZ);
            bool canPushEast = !world.shouldSuffocate(floorX + 1, floorY, floorZ);
            bool canPushDown = !world.shouldSuffocate(floorX, floorY - 1, floorZ);
            bool canPushUp = !world.shouldSuffocate(floorX, floorY + 1, floorZ);
            bool canPushNorth = !world.shouldSuffocate(floorX, floorY, floorZ - 1);
            bool canPushSouth = !world.shouldSuffocate(floorX, floorY, floorZ + 1);
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
                velocityX = (double)(-pushStrength);
            }

            if (pushDirection == 1)
            {
                velocityX = (double)pushStrength;
            }

            if (pushDirection == 2)
            {
                velocityY = (double)(-pushStrength);
            }

            if (pushDirection == 3)
            {
                velocityY = (double)pushStrength;
            }

            if (pushDirection == 4)
            {
                velocityZ = (double)(-pushStrength);
            }

            if (pushDirection == 5)
            {
                velocityZ = (double)pushStrength;
            }
        }

        return false;
    }

    public override bool Equals(object other)
    {
        return other is Entity e && e.id == id;
    }

    public override int GetHashCode()
    {
        return id;
    }
}
