using BetaSharp.Blocks;
using BetaSharp.Blocks.Materials;
using BetaSharp.Items;
using BetaSharp.NBT;
using BetaSharp.Util.Hit;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds;

namespace BetaSharp.Entities;

public class EntityLiving : Entity
{
    public int maxHealth = 20;
    public float limbSwingPhase;
    public float limbSwingScale;
    public float bodyYaw;
    public float lastBodyYaw;
    protected float lastWalkProgress;
    protected float walkProgress;
    protected float totalWalkDistance;
    protected float lastTotalWalkDistance;
    protected bool canLookAround = true;
    protected string texture = "/mob/char.png";
    protected bool field_9355_A = true;
    protected float rotationOffset = 0.0F;
    protected string modelName = null;
    protected float modelScale = 1.0F;
    protected int scoreAmount = 0;
    protected float field_9345_F = 0.0F;
    public bool interpolateOnly = false;
    public float lastSwingAnimationProgress;
    public float swingAnimationProgress;
    public int health = 10;
    public int lastHealth;
    private int livingSoundTime;
    public int hurtTime;
    public int maxHurtTime;
    public float attackedAtYaw;
    public int deathTime;
    public int attackTime;
    public float cameraPitch;
    public float tilt;
    protected bool unused_flag;
    public int field_9326_T = -1;
    public float field_9325_U = Random.Shared.NextSingle() * 0.9f + 0.1f;
    public float lastWalkAnimationSpeed;
    public float walkAnimationSpeed;
    public float animationPhase;
    protected int newPosRotationIncrements;
    protected double newPosX;
    protected double newPosY;
    protected double newPosZ;
    protected double newRotationYaw;
    protected double newRotationPitch;
    protected int damageForDisplay;
    protected int entityAge;
    protected float sidewaysSpeed;
    protected float forwardSpeed;
    protected float rotationSpeed;
    protected bool jumping;
    protected float defaultPitch = 0.0F;
    protected float movementSpeed = 0.7F;
    private Entity lookTarget;
    protected int lookTimer;

    public EntityLiving(World world) : base(world)
    {
        preventEntitySpawning = true;
        limbSwingScale = (Random.Shared.NextSingle() + 1.0f) * 0.01f;
        setPosition(x, y, z);
        limbSwingPhase = Random.Shared.NextSingle() * 12398.0f;
        yaw = (Random.Shared.NextSingle() * (float)Math.PI) * 2.0f;
        stepHeight = 0.5F;
    }

    public virtual void PostSpawn()
    {

    }

    protected override void initDataTracker()
    {
    }

    public bool canSee(Entity entity)
    {
        return world.raycast(new Vec3D(x, y + (double)getEyeHeight(), z), new Vec3D(entity.x, entity.y + (double)entity.getEyeHeight(), entity.z)).Type == HitResultType.MISS;
    }

    public override string getTexture()
    {
        return texture;
    }

    public override bool isCollidable()
    {
        return !dead;
    }

    public override bool isPushable()
    {
        return !dead;
    }

    public override float getEyeHeight()
    {
        return height * 0.85F;
    }

    public virtual int getTalkInterval()
    {
        return 80;
    }

    public void playLivingSound()
    {
        string sound = getLivingSound();
        if (sound != null)
        {
            world.playSound(this, sound, getSoundVolume(), (random.NextFloat() - random.NextFloat()) * 0.2F + 1.0F);
        }

    }

    public override void baseTick()
    {
        lastSwingAnimationProgress = swingAnimationProgress;
        base.baseTick();
        if (random.NextInt(1000) < livingSoundTime++)
        {
            livingSoundTime = -getTalkInterval();
            playLivingSound();
        }

        if (isAlive() && isInsideWall())
        {
            damage(null, 1);
        }

        if (isImmuneToFire || world.isRemote)
        {
            fireTicks = 0;
        }

        int i;
        if (isAlive() && isInFluid(Material.Water) && !canBreatheUnderwater())
        {
            --air;
            if (air == -20)
            {
                air = 0;

                for (i = 0; i < 8; ++i)
                {
                    float offsetX = random.NextFloat() - random.NextFloat();
                    float offsetY = random.NextFloat() - random.NextFloat();
                    float offsetZ = random.NextFloat() - random.NextFloat();
                    world.addParticle("bubble", x + (double)offsetX, y + (double)offsetY, z + (double)offsetZ, velocityX, velocityY, velocityZ);
                }

                damage(null, 2);
            }

            fireTicks = 0;
        }
        else
        {
            air = maxAir;
        }

        cameraPitch = tilt;
        if (attackTime > 0)
        {
            --attackTime;
        }

        if (hurtTime > 0)
        {
            --hurtTime;
        }

        if (hearts > 0)
        {
            --hearts;
        }

        if (health <= 0)
        {
            ++deathTime;
            if (deathTime > 20)
            {
                onEntityDeath();
                markDead();

                for (i = 0; i < 20; ++i)
                {
                    double velX = random.NextGaussian() * 0.02D;
                    double velY = random.NextGaussian() * 0.02D;
                    double velZ = random.NextGaussian() * 0.02D;
                    world.addParticle("explode", x + (double)(random.NextFloat() * width * 2.0F) - (double)width, y + (double)(random.NextFloat() * height), z + (double)(random.NextFloat() * width * 2.0F) - (double)width, velX, velY, velZ);
                }
            }
        }

        lastTotalWalkDistance = totalWalkDistance;
        lastBodyYaw = bodyYaw;
        prevYaw = yaw;
        prevPitch = pitch;
    }

    public override void move(double x, double y, double z)
    {
        if (!interpolateOnly/* || this is ClientPlayerEntity*/) base.move(x, y, z);
    }

    public void animateSpawn()
    {
        for (int i = 0; i < 20; ++i)
        {
            double velX = random.NextGaussian() * 0.02D;
            double velY = random.NextGaussian() * 0.02D;
            double velZ = random.NextGaussian() * 0.02D;
            double spread = 10.0D;
            world.addParticle("explode", x + (double)(random.NextFloat() * width * 2.0F) - (double)width - velX * spread, y + (double)(random.NextFloat() * height) - velY * spread, z + (double)(random.NextFloat() * width * 2.0F) - (double)width - velZ * spread, velX, velY, velZ);
        }

    }

    public override void tickRiding()
    {
        base.tickRiding();
        lastWalkProgress = walkProgress;
        walkProgress = 0.0F;
    }

    public override void setPositionAndAnglesAvoidEntities(double newPosX, double newPosY, double newPosZ, float newRotationYaw, float newRotationPitch, int newPosRotationIncrements)
    {
        standingEyeHeight = 0.0F;
        this.newPosX = newPosX;
        this.newPosY = newPosY;
        this.newPosZ = newPosZ;
        this.newRotationYaw = (double)newRotationYaw;
        this.newRotationPitch = (double)newRotationPitch;
        this.newPosRotationIncrements = newPosRotationIncrements;
    }

    public override void tick()
    {
        base.tick();
        tickMovement();
        double dx = x - prevX;
        double dz = z - prevZ;
        float horizontalDistance = MathHelper.Sqrt(dx * dx + dz * dz);
        float computedYaw = bodyYaw;
        float walkSpeed = 0.0F;
        lastWalkProgress = walkProgress;
        float walkAmount = 0.0F;
        if (horizontalDistance > 0.05F)
        {
            walkAmount = 1.0F;
            walkSpeed = horizontalDistance * 3.0F;
            computedYaw = (float)System.Math.Atan2(dz, dx) * 180.0F / (float)System.Math.PI - 90.0F;
        }

        if (swingAnimationProgress > 0.0F)
        {
            computedYaw = base.yaw;
        }

        if (!onGround)
        {
            walkAmount = 0.0F;
        }

        walkProgress += (walkAmount - walkProgress) * 0.3F;

        float yawDelta;
        for (yawDelta = computedYaw - bodyYaw; yawDelta < -180.0F; yawDelta += 360.0F)
        {
        }

        while (yawDelta >= 180.0F)
        {
            yawDelta -= 360.0F;
        }

        bodyYaw += yawDelta * 0.3F;

        float headYawDelta;
        for (headYawDelta = base.yaw - bodyYaw; headYawDelta < -180.0F; headYawDelta += 360.0F)
        {
        }

        while (headYawDelta >= 180.0F)
        {
            headYawDelta -= 360.0F;
        }

        bool headFacingBackward = headYawDelta < -90.0F || headYawDelta >= 90.0F;
        if (headYawDelta < -75.0F)
        {
            headYawDelta = -75.0F;
        }

        if (headYawDelta >= 75.0F)
        {
            headYawDelta = 75.0F;
        }

        bodyYaw = base.yaw - headYawDelta;
        if (headYawDelta * headYawDelta > 2500.0F)
        {
            bodyYaw += headYawDelta * 0.2F;
        }

        if (headFacingBackward)
        {
            walkSpeed *= -1.0F;
        }

        while (base.yaw - prevYaw < -180.0F)
        {
            prevYaw -= 360.0F;
        }

        while (base.yaw - prevYaw >= 180.0F)
        {
            prevYaw += 360.0F;
        }

        while (bodyYaw - lastBodyYaw < -180.0F)
        {
            lastBodyYaw -= 360.0F;
        }

        while (bodyYaw - lastBodyYaw >= 180.0F)
        {
            lastBodyYaw += 360.0F;
        }

        while (pitch - prevPitch < -180.0F)
        {
            prevPitch -= 360.0F;
        }

        while (pitch - prevPitch >= 180.0F)
        {
            prevPitch += 360.0F;
        }

        totalWalkDistance += walkSpeed;
    }

    protected override void setBoundingBoxSpacing(float widthOffset, float heightOffset)
    {
        base.setBoundingBoxSpacing(widthOffset, heightOffset);
    }

    public virtual void heal(int amount)
    {
        if (health > 0)
        {
            health += amount;
            if (health > 20)
            {
                health = 20;
            }

            hearts = maxHealth / 2;
        }
    }

    public override bool damage(Entity entity, int amount)
    {
        if (world.isRemote)
        {
            return false;
        }
        else
        {
            entityAge = 0;
            if (health <= 0)
            {
                return false;
            }
            else
            {
                walkAnimationSpeed = 1.5F;
                bool var3 = true;
                if ((float)hearts > (float)maxHealth / 2.0F)
                {
                    if (amount <= damageForDisplay)
                    {
                        return false;
                    }

                    applyDamage(amount - damageForDisplay);
                    damageForDisplay = amount;
                    var3 = false;
                }
                else
                {
                    damageForDisplay = amount;
                    lastHealth = health;
                    hearts = maxHealth;
                    applyDamage(amount);
                    hurtTime = maxHurtTime = 10;
                }

                attackedAtYaw = 0.0F;
                if (var3)
                {
                    world.broadcastEntityEvent(this, (byte)2);
                    scheduleVelocityUpdate();
                    if (entity != null)
                    {
                        double var4 = entity.x - x;

                        double var6;
                        for (var6 = entity.z - z; var4 * var4 + var6 * var6 < 1.0E-4D; var6 = (Random.Shared.NextDouble() - Random.Shared.NextDouble()) * 0.01D)
                        {
                            var4 = (Random.Shared.NextDouble() - Random.Shared.NextDouble()) * 0.01D;
                        }

                        attackedAtYaw = (float)(System.Math.Atan2(var6, var4) * 180.0D / (double)((float)System.Math.PI)) - yaw;
                        knockBack(entity, amount, var4, var6);
                    }
                    else
                    {
                        attackedAtYaw = (float)((int)(Random.Shared.NextDouble() * 2.0D) * 180);
                    }
                }

                if (health <= 0)
                {
                    if (var3)
                    {
                        world.playSound(this, getDeathSound(), getSoundVolume(), (random.NextFloat() - random.NextFloat()) * 0.2F + 1.0F);
                    }

                    onKilledBy(entity);
                }
                else if (var3)
                {
                    world.playSound(this, getHurtSound(), getSoundVolume(), (random.NextFloat() - random.NextFloat()) * 0.2F + 1.0F);
                }

                return true;
            }
        }
    }

    public override void animateHurt()
    {
        hurtTime = maxHurtTime = 10;
        attackedAtYaw = 0.0F;
    }

    protected virtual void applyDamage(int amount)
    {
        health -= amount;
    }

    protected virtual float getSoundVolume()
    {
        return 1.0F;
    }

    protected virtual string getLivingSound()
    {
        return null;
    }

    protected virtual string getHurtSound()
    {
        return "random.hurt";
    }

    protected virtual string getDeathSound()
    {
        return "random.hurt";
    }

    public void knockBack(Entity entity, int amount, double dx, double dy)
    {
        float var7 = MathHelper.Sqrt(dx * dx + dy * dy);
        float var8 = 0.4F;
        velocityX /= 2.0D;
        velocityY /= 2.0D;
        velocityZ /= 2.0D;
        velocityX -= dx / (double)var7 * (double)var8;
        velocityY += (double)0.4F;
        velocityZ -= dy / (double)var7 * (double)var8;
        if (velocityY > (double)0.4F)
        {
            velocityY = (double)0.4F;
        }

    }

    public virtual void onKilledBy(Entity var1)
    {
        if (scoreAmount >= 0 && var1 != null)
        {
            var1.updateKilledAchievement(this, scoreAmount);
        }

        if (var1 != null)
        {
            var1.onKillOther(this);
        }

        unused_flag = true;
        if (!world.isRemote)
        {
            dropFewItems();
        }

        world.broadcastEntityEvent(this, (byte)3);
    }

    protected virtual void dropFewItems()
    {
        int var1 = getDropItemId();
        if (var1 > 0)
        {
            int var2 = random.NextInt(3);

            for (int var3 = 0; var3 < var2; ++var3)
            {
                dropItem(var1, 1);
            }
        }

    }

    protected virtual int getDropItemId()
    {
        return 0;
    }

    protected override void onLanding(float fallDistance)
    {
        base.onLanding(fallDistance);
        int var2 = (int)java.lang.Math.ceil((double)(fallDistance - 3.0F));
        if (var2 > 0)
        {
            damage(null, var2);
            int var3 = world.getBlockId(MathHelper.Floor(x), MathHelper.Floor(y - (double)0.2F - (double)standingEyeHeight), MathHelper.Floor(z));
            if (var3 > 0)
            {
                BlockSoundGroup soundGroup = Block.Blocks[var3].soundGroup;
                world.playSound(this, soundGroup.StepSound, soundGroup.Volume * 0.5F, soundGroup.Pitch * (12.0F / 16.0F));
            }
        }

    }

    public virtual void travel(float strafe, float forward)
    {
        double previousY;
        if (isInWater())
        {
            previousY = y;
            moveNonSolid(strafe, forward, 0.02F);
            move(velocityX, velocityY, velocityZ);
            velocityX *= (double)0.8F;
            velocityY *= (double)0.8F;
            velocityZ *= (double)0.8F;
            velocityY -= 0.02D;
            if (horizontalCollison && getEntitiesInside(velocityX, velocityY + (double)0.6F - y + previousY, velocityZ))
            {
                velocityY = (double)0.3F;
            }
        }
        else if (isTouchingLava())
        {
            previousY = y;
            moveNonSolid(strafe, forward, 0.02F);
            move(velocityX, velocityY, velocityZ);
            velocityX *= 0.5D;
            velocityY *= 0.5D;
            velocityZ *= 0.5D;
            velocityY -= 0.02D;
            if (horizontalCollison && getEntitiesInside(velocityX, velocityY + (double)0.6F - y + previousY, velocityZ))
            {
                velocityY = (double)0.3F;
            }
        }
        else
        {
            float friction = 0.91F;
            if (onGround)
            {
                friction = 546.0F * 0.1F * 0.1F * 0.1F;
                int groundBlockId = world.getBlockId(MathHelper.Floor(x), MathHelper.Floor(boundingBox.MinY) - 1, MathHelper.Floor(z));
                if (groundBlockId > 0)
                {
                    friction = Block.Blocks[groundBlockId].slipperiness * 0.91F;
                }
            }

            float movementFactor = 0.16277136F / (friction * friction * friction);
            moveNonSolid(strafe, forward, onGround ? 0.1F * movementFactor : 0.02F);
            friction = 0.91F;
            if (onGround)
            {
                friction = 546.0F * 0.1F * 0.1F * 0.1F;
                int groundBlockId = world.getBlockId(MathHelper.Floor(x), MathHelper.Floor(boundingBox.MinY) - 1, MathHelper.Floor(z));
                if (groundBlockId > 0)
                {
                    friction = Block.Blocks[groundBlockId].slipperiness * 0.91F;
                }
            }

            if (isOnLadder())
            {
                float ladderSpeedClamp = 0.15F;
                if (velocityX < (double)(-ladderSpeedClamp))
                {
                    velocityX = (double)(-ladderSpeedClamp);
                }

                if (velocityX > (double)ladderSpeedClamp)
                {
                    velocityX = (double)ladderSpeedClamp;
                }

                if (velocityZ < (double)(-ladderSpeedClamp))
                {
                    velocityZ = (double)(-ladderSpeedClamp);
                }

                if (velocityZ > (double)ladderSpeedClamp)
                {
                    velocityZ = (double)ladderSpeedClamp;
                }

                fallDistance = 0.0F;
                if (velocityY < -0.15D)
                {
                    velocityY = -0.15D;
                }

                if (isSneaking() && velocityY < 0.0D)
                {
                    velocityY = 0.0D;
                }
            }

            move(velocityX, velocityY, velocityZ);
            if (horizontalCollison && isOnLadder())
            {
                velocityY = 0.2D;
            }

            velocityY -= 0.08D;
            velocityY *= (double)0.98F;
            velocityX *= (double)friction;
            velocityZ *= (double)friction;
        }

        lastWalkAnimationSpeed = walkAnimationSpeed;
        previousY = x - prevX;
        double deltaZ = z - prevZ;
        float distanceMoved = MathHelper.Sqrt(previousY * previousY + deltaZ * deltaZ) * 4.0F;
        if (distanceMoved > 1.0F)
        {
            distanceMoved = 1.0F;
        }

        walkAnimationSpeed += (distanceMoved - walkAnimationSpeed) * 0.4F;
        animationPhase += walkAnimationSpeed;
    }

    public virtual bool isOnLadder()
    {
        int x = MathHelper.Floor(base.x);
        int y = MathHelper.Floor(boundingBox.MinY);
        int z = MathHelper.Floor(base.z);
        return world.getBlockId(x, y, z) == Block.Ladder.id;
    }

    public override void writeNbt(NBTTagCompound nbt)
    {
        nbt.SetShort("Health", (short)health);
        nbt.SetShort("HurtTime", (short)hurtTime);
        nbt.SetShort("DeathTime", (short)deathTime);
        nbt.SetShort("AttackTime", (short)attackTime);
    }

    public override void readNbt(NBTTagCompound nbt)
    {
        health = nbt.GetShort("Health");
        if (!nbt.HasKey("Health"))
        {
            health = 10;
        }

        hurtTime = nbt.GetShort("HurtTime");
        deathTime = nbt.GetShort("DeathTime");
        attackTime = nbt.GetShort("AttackTime");
    }

    public override bool isAlive()
    {
        return !dead && health > 0;
    }

    public virtual bool canBreatheUnderwater()
    {
        return false;
    }

    public virtual void tickMovement()
    {
        if (newPosRotationIncrements > 0)
        {
            double newX = x + (newPosX - x) / (double)newPosRotationIncrements;
            double newY = y + (newPosY - y) / (double)newPosRotationIncrements;
            double newZ = z + (newPosZ - z) / (double)newPosRotationIncrements;

            double yawDelta;
            for (yawDelta = newRotationYaw - (double)yaw; yawDelta < -180.0D; yawDelta += 360.0D)
            {
            }

            while (yawDelta >= 180.0D)
            {
                yawDelta -= 360.0D;
            }

            yaw = (float)((double)yaw + yawDelta / (double)newPosRotationIncrements);
            pitch = (float)((double)pitch + (newRotationPitch - (double)pitch) / (double)newPosRotationIncrements);
            --newPosRotationIncrements;
            setPosition(newX, newY, newZ);
            setRotation(yaw, pitch);
            var collisions = world.GetEntityCollisionsScratch(this, boundingBox.Contract(1.0D / 32.0D, 0.0D, 1.0D / 32.0D));
            if (collisions.Count > 0)
            {
                double highestCollisionY = 0.0D;

                for (int i = 0; i < collisions.Count; ++i)
                {
                    Box box = collisions[i];
                    if (box.MaxY > highestCollisionY)
                    {
                        highestCollisionY = box.MaxY;
                    }
                }

                newY += highestCollisionY - boundingBox.MinY;
                setPosition(newX, newY, newZ);
            }
        }

        if (isMovementBlocked())
        {
            jumping = false;
            sidewaysSpeed = 0.0F;
            forwardSpeed = 0.0F;
            rotationSpeed = 0.0F;
        }
        else if (!interpolateOnly)
        {
            tickLiving();
        }

        bool isInWater = base.isInWater();
        bool isTouchingLava = base.isTouchingLava();
        if (jumping)
        {
            if (isInWater)
            {
                velocityY += (double)0.04F;
            }
            else if (isTouchingLava)
            {
                velocityY += (double)0.04F;
            }
            else if (onGround)
            {
                jump();
            }
        }

        sidewaysSpeed *= 0.98F;
        forwardSpeed *= 0.98F;
        rotationSpeed *= 0.9F;
        travel(sidewaysSpeed, forwardSpeed);
        var nearbyEntities = world.GetEntitiesScratch(this, boundingBox.Expand((double)0.2F, 0.0D, (double)0.2F));
        if (nearbyEntities != null && nearbyEntities.Count > 0)
        {
            for (int i = 0; i < nearbyEntities.Count; ++i)
            {
                Entity entity = nearbyEntities[i];
                if (entity.isPushable())
                {
                    entity.onCollision(this);
                }
            }
        }

    }

    protected virtual bool isMovementBlocked()
    {
        return health <= 0;
    }

    protected virtual void jump()
    {
        velocityY = (double)0.42F;
    }

    protected virtual bool canDespawn()
    {
        return true;
    }

    protected void func_27021_X()
    {
        EntityPlayer player = world.getClosestPlayer(this, -1.0D);
        if (canDespawn() && player != null)
        {
            double dx = player.x - x;
            double dy = player.y - y;
            double dz = player.z - z;
            double squaredDistance = dx * dx + dy * dy + dz * dz;
            if (squaredDistance > 16384.0D)
            {
                markDead();
            }

            if (entityAge > 600 && random.NextInt(800) == 0)
            {
                if (squaredDistance < 1024.0D)
                {
                    entityAge = 0;
                }
                else
                {
                    markDead();
                }
            }
        }

    }

    public virtual void tickLiving()
    {
        ++entityAge;
        EntityPlayer closestPlayer = world.getClosestPlayer(this, -1.0D);
        func_27021_X();
        sidewaysSpeed = 0.0F;
        forwardSpeed = 0.0F;
        float lookRange = 8.0F;
        if (random.NextFloat() < 0.02F)
        {
            closestPlayer = world.getClosestPlayer(this, (double)lookRange);
            if (closestPlayer != null)
            {
                lookTarget = closestPlayer;
                lookTimer = 10 + random.NextInt(20);
            }
            else
            {
                rotationSpeed = (random.NextFloat() - 0.5F) * 20.0F;
            }
        }

        if (lookTarget != null)
        {
            faceEntity(lookTarget, 10.0F, (float)getMaxFallDistance());
            if (lookTimer-- <= 0 || lookTarget.dead || lookTarget.getSquaredDistance(this) > (double)(lookRange * lookRange))
            {
                lookTarget = null;
            }
        }
        else
        {
            if (random.NextFloat() < 0.05F)
            {
                rotationSpeed = (random.NextFloat() - 0.5F) * 20.0F;
            }

            yaw += rotationSpeed;
            pitch = defaultPitch;
        }

        bool isInWater = base.isInWater();
        bool isTouchingLava = base.isTouchingLava();
        if (isInWater || isTouchingLava)
        {
            jumping = random.NextFloat() < 0.8F;
        }

    }

    protected virtual int getMaxFallDistance()
    {
        return 40;
    }

    public void faceEntity(Entity entity, float yawSpeed, float pitchSpeed)
    {
        double dx = entity.x - x;
        double dz = entity.z - z;
        double dy;
        if (entity is EntityLiving)
        {
            EntityLiving ent = (EntityLiving)entity;
            dy = y + (double)getEyeHeight() - (ent.y + (double)ent.getEyeHeight());
        }
        else
        {
            dy = (entity.boundingBox.MinY + entity.boundingBox.MaxY) / 2.0D - (y + (double)getEyeHeight());
        }

        double horizontalDistance = (double)MathHelper.Sqrt(dx * dx + dz * dz);
        float targetYaw = (float)(System.Math.Atan2(dz, dx) * 180.0D / (double)((float)System.Math.PI)) - 90.0F;
        float targetPitch = (float)(-(System.Math.Atan2(dy, horizontalDistance) * 180.0D / (double)((float)System.Math.PI)));
        pitch = -updateRotation(pitch, targetPitch, pitchSpeed);
        yaw = updateRotation(yaw, targetYaw, yawSpeed);
    }

    public bool hasCurrentTarget()
    {
        return lookTarget != null;
    }

    public Entity getCurrentTarget()
    {
        return lookTarget;
    }

    private float updateRotation(float var1, float var2, float var3)
    {
        float var4;
        for (var4 = var2 - var1; var4 < -180.0F; var4 += 360.0F)
        {
        }

        while (var4 >= 180.0F)
        {
            var4 -= 360.0F;
        }

        if (var4 > var3)
        {
            var4 = var3;
        }

        if (var4 < -var3)
        {
            var4 = -var3;
        }

        return var1 + var4;
    }

    public void onEntityDeath()
    {
    }

    public virtual bool canSpawn()
    {
        return world.canSpawnEntity(boundingBox) && world.GetEntityCollisions(this, boundingBox).Count == 0 && !world.isBoxSubmergedInFluid(boundingBox);
    }

    protected override void tickInVoid()
    {
        damage(null, 4);
    }

    public float getSwingProgress(float partialTick)
    {
        float var2 = swingAnimationProgress - lastSwingAnimationProgress;
        if (var2 < 0.0F)
        {
            ++var2;
        }

        return lastSwingAnimationProgress + var2 * partialTick;
    }

    public Vec3D getPosition(float partialTick)
    {
        if (partialTick == 1.0F)
        {
            return new Vec3D(x, y, z);
        }
        else
        {
            double x = prevX + (base.x - prevX) * (double)partialTick;
            double y = prevY + (base.y - prevY) * (double)partialTick;
            double z = prevZ + (base.z - prevZ) * (double)partialTick;
            return new Vec3D(x, y, z);
        }
    }

    public override Vec3D? getLookVector()
    {
        return getLook(1.0F);
    }

    public Vec3D getLook(float partialTick)
    {
        float cosYaw;
        float sinYaw;
        float cosPitch;
        float sinPitch;
        if (partialTick == 1.0F)
        {
            cosYaw = MathHelper.Cos(-yaw * ((float)System.Math.PI / 180.0F) - (float)System.Math.PI);
            sinYaw = MathHelper.Sin(-yaw * ((float)System.Math.PI / 180.0F) - (float)System.Math.PI);
            cosPitch = -MathHelper.Cos(-pitch * ((float)System.Math.PI / 180.0F));
            sinPitch = MathHelper.Sin(-pitch * ((float)System.Math.PI / 180.0F));
            return new Vec3D((double)(sinYaw * cosPitch), (double)sinPitch, (double)(cosYaw * cosPitch));
        }
        else
        {
            cosYaw = prevPitch + (pitch - prevPitch) * partialTick;
            sinYaw = prevYaw + (yaw - prevYaw) * partialTick;
            cosPitch = MathHelper.Cos(-sinYaw * ((float)System.Math.PI / 180.0F) - (float)System.Math.PI);
            sinPitch = MathHelper.Sin(-sinYaw * ((float)System.Math.PI / 180.0F) - (float)System.Math.PI);
            float var6 = -MathHelper.Cos(-cosYaw * ((float)System.Math.PI / 180.0F));
            float var7 = MathHelper.Sin(-cosYaw * ((float)System.Math.PI / 180.0F));
            return new Vec3D((double)(sinPitch * var6), (double)var7, (double)(cosPitch * var6));
        }
    }

    public HitResult rayTrace(double range, float partialTick)
    {
        Vec3D startPos = getPosition(partialTick);
        Vec3D lookDir = getLook(partialTick);
        Vec3D endPos = startPos + range * lookDir;
        return world.raycast(startPos, endPos);
    }

    public virtual int getMaxSpawnedInChunk()
    {
        return 4;
    }

    public virtual ItemStack getHeldItem()
    {
        return null;
    }

    public override void processServerEntityStatus(sbyte statusId)
    {
        if (statusId == 2)
        {
            walkAnimationSpeed = 1.5F;
            hearts = maxHealth;
            hurtTime = maxHurtTime = 10;
            attackedAtYaw = 0.0F;
            world.playSound(this, getHurtSound(), getSoundVolume(), (random.NextFloat() - random.NextFloat()) * 0.2F + 1.0F);
            damage(null, 0);
        }
        else if (statusId == 3)
        {
            world.playSound(this, getDeathSound(), getSoundVolume(), (random.NextFloat() - random.NextFloat()) * 0.2F + 1.0F);
            health = 0;
            onKilledBy(null);
        }
        else
        {
            base.processServerEntityStatus(statusId);
        }

    }

    public virtual bool isSleeping()
    {
        return false;
    }

    public virtual int getItemStackTextureId(ItemStack item)
    {
        return item.getTextureId();
    }
}
