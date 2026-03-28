using BetaSharp.Blocks.Materials;
using BetaSharp.Items;
using BetaSharp.NBT;
using BetaSharp.Util.Hit;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Entities;

public class EntityFish : Entity
{
    public override EntityType Type => EntityRegistry.FishHook;
    private int xTile;
    private int yTile;
    private int zTile;
    private int inTile;
    private bool inGround;
    public int shake;
    public EntityPlayer angler;
    private int ticksInGround;
    private int ticksInAir;
    private int ticksCatchable;
    public Entity bobber;
    private int positionUpdateTicks;
    private double targetX;
    private double targetY;
    private double targetZ;
    private double targetYaw;
    private double targetPitch;
    private double clientVelocityX;
    private double clientVelocityY;
    private double clientVelocityZ;

    public EntityFish(IWorldContext world) : base(world)
    {
        xTile = -1;
        yTile = -1;
        zTile = -1;
        inTile = 0;
        inGround = false;
        shake = 0;
        ticksInAir = 0;
        ticksCatchable = 0;
        bobber = null;
        setBoundingBoxSpacing(0.25F, 0.25F);
        ignoreFrustumCheck = true;
    }

    public EntityFish(IWorldContext world, double x, double y, double z) : this(world)
    {
        setPosition(x, y, z);
        ignoreFrustumCheck = true;
    }

    public EntityFish(IWorldContext world, EntityPlayer player) : base(world)
    {
        xTile = -1;
        yTile = -1;
        zTile = -1;
        inTile = 0;
        inGround = false;
        shake = 0;
        ticksInAir = 0;
        ticksCatchable = 0;
        bobber = null;
        ignoreFrustumCheck = true;
        angler = player;
        angler.fishHook = this;
        setBoundingBoxSpacing(0.25F, 0.25F);
        setPositionAndAnglesKeepPrevAngles(player.x, player.y + 1.62D - (double)player.standingEyeHeight, player.z, player.yaw, player.pitch);
        x -= (double)(MathHelper.Cos(yaw / 180.0F * (float)System.Math.PI) * 0.16F);
        y -= (double)0.1F;
        z -= (double)(MathHelper.Sin(yaw / 180.0F * (float)System.Math.PI) * 0.16F);
        setPosition(x, y, z);
        standingEyeHeight = 0.0F;
        float speed = 0.4F;
        base.velocityX = (double)(-MathHelper.Sin(yaw / 180.0F * (float)System.Math.PI) * MathHelper.Cos(pitch / 180.0F * (float)System.Math.PI) * speed);
        base.velocityZ = (double)(MathHelper.Cos(yaw / 180.0F * (float)System.Math.PI) * MathHelper.Cos(pitch / 180.0F * (float)System.Math.PI) * speed);
        base.velocityY = (double)(-MathHelper.Sin(pitch / 180.0F * (float)System.Math.PI) * speed);
        setHeading(base.velocityX, base.velocityY, base.velocityZ, 1.5F, 1.0F);
    }


    public override bool shouldRender(double distanceSquared)
    {
        double renderDistance = boundingBox.AverageEdgeLength * 4.0D;
        renderDistance *= 64.0D;
        return distanceSquared < renderDistance * renderDistance;
    }

    public void setHeading(double dirX, double dirY, double dirZ, float speed, float spread)
    {
        float length = MathHelper.Sqrt(dirX * dirX + dirY * dirY + dirZ * dirZ);
        dirX /= (double)length;
        dirY /= (double)length;
        dirZ /= (double)length;
        dirX += random.NextGaussian() * (double)0.0075F * (double)spread;
        dirY += random.NextGaussian() * (double)0.0075F * (double)spread;
        dirZ += random.NextGaussian() * (double)0.0075F * (double)spread;
        dirX *= (double)speed;
        dirY *= (double)speed;
        dirZ *= (double)speed;
        base.velocityX = dirX;
        base.velocityY = dirY;
        base.velocityZ = dirZ;
        float horizontalLength = MathHelper.Sqrt(dirX * dirX + dirZ * dirZ);
        prevYaw = yaw = (float)(System.Math.Atan2(dirX, dirZ) * 180.0D / (double)((float)System.Math.PI));
        prevPitch = pitch = (float)(System.Math.Atan2(dirY, (double)horizontalLength) * 180.0D / (double)((float)System.Math.PI));
        ticksInGround = 0;
    }

    public override void setPositionAndAnglesAvoidEntities(double newX, double newY, double newZ, float newYaw, float newPitch, int interpolationSteps)
    {
        targetX = newX;
        targetY = newY;
        targetZ = newZ;
        targetYaw = (double)newYaw;
        targetPitch = (double)newPitch;
        positionUpdateTicks = interpolationSteps;
        base.velocityX = clientVelocityX;
        base.velocityY = clientVelocityY;
        base.velocityZ = clientVelocityZ;
    }

    public override void setVelocityClient(double motionX, double motionY, double motionZ)
    {
        clientVelocityX = base.velocityX = motionX;
        clientVelocityY = base.velocityY = motionY;
        clientVelocityZ = base.velocityZ = motionZ;
    }

    public override void tick()
    {
        base.tick();
        if (positionUpdateTicks > 0)
        {
            double interpX = x + (targetX - x) / (double)positionUpdateTicks;
            double interpY = y + (targetY - y) / (double)positionUpdateTicks;
            double interpZ = z + (targetZ - z) / (double)positionUpdateTicks;

            double yawDelta;
            for (yawDelta = targetYaw - (double)yaw; yawDelta < -180.0D; yawDelta += 360.0D)
            {
            }

            while (yawDelta >= 180.0D)
            {
                yawDelta -= 360.0D;
            }

            yaw = (float)((double)yaw + yawDelta / (double)positionUpdateTicks);
            pitch = (float)((double)pitch + (targetPitch - (double)pitch) / (double)positionUpdateTicks);
            --positionUpdateTicks;
            setPosition(interpX, interpY, interpZ);
            setRotation(yaw, pitch);
        }
        else
        {
            if (!world.IsRemote)
            {
                ItemStack heldItem = angler.getHand();
                if (angler.dead || !angler.isAlive() || heldItem == null || heldItem.getItem() != Item.FishingRod || getSquaredDistance(angler) > 1024.0D)
                {
                    markDead();
                    angler.fishHook = null;
                    return;
                }

                if (bobber != null)
                {
                    if (!bobber.dead)
                    {
                        x = bobber.x;
                        y = bobber.boundingBox.MinY + (double)bobber.height * 0.8D;
                        z = bobber.z;
                        return;
                    }

                    bobber = null;
                }
            }

            if (shake > 0)
            {
                --shake;
            }

            if (inGround)
            {
                int blockId = world.Reader.GetBlockId(xTile, yTile, zTile);
                if (blockId == inTile)
                {
                    ++ticksInGround;
                    if (ticksInGround == 1200)
                    {
                        markDead();
                    }

                    return;
                }

                inGround = false;
                base.velocityX *= (double)(random.NextFloat() * 0.2F);
                base.velocityY *= (double)(random.NextFloat() * 0.2F);
                base.velocityZ *= (double)(random.NextFloat() * 0.2F);
                ticksInGround = 0;
                ticksInAir = 0;
            }
            else
            {
                ++ticksInAir;
            }

            Vec3D rayStart = new Vec3D(x, y, z);
            Vec3D rayEnd = new Vec3D(x + base.velocityX, y + base.velocityY, z + base.velocityZ);
            HitResult hit = world.Reader.Raycast(rayStart, rayEnd);
            rayStart = new Vec3D(x, y, z);
            rayEnd = new Vec3D(x + base.velocityX, y + base.velocityY, z + base.velocityZ);
            if (hit.Type != HitResultType.MISS)
            {
                rayEnd = new Vec3D(hit.Pos.x, hit.Pos.y, hit.Pos.z);
            }

            Entity hitEntity = null;
            var entities = world.Entities.GetEntities(this, boundingBox.Stretch(base.velocityX, base.velocityY, base.velocityZ).Expand(1.0D, 1.0D, 1.0D));
            double minHitDistance = 0.0D;

            double buoyancy;
            for (int i = 0; i < entities.Count; ++i)
            {
                Entity entity = entities[i];
                if (entity.isCollidable() && (entity != angler || ticksInAir >= 5))
                {
                    float expandAmount = 0.3F;
                    Box expandedBox = entity.boundingBox.Expand((double)expandAmount, (double)expandAmount, (double)expandAmount);
                    HitResult entityHit = expandedBox.Raycast(rayStart, rayEnd);
                    if (entityHit.Type != HitResultType.MISS)
                    {
                        buoyancy = rayStart.distanceTo(entityHit.Pos);
                        if (buoyancy < minHitDistance || minHitDistance == 0.0D)
                        {
                            hitEntity = entity;
                            minHitDistance = buoyancy;
                        }
                    }
                }
            }

            if (hitEntity != null)
            {
                hit = new HitResult(hitEntity);
            }

            if (hit.Type != HitResultType.MISS)
            {
                if (hit.Entity != null)
                {
                    if (hit.Entity.damage(angler, 0))
                    {
                        bobber = hit.Entity;
                    }
                }
                else
                {
                    inGround = true;
                }
            }

            if (!inGround)
            {
                base.move(base.velocityX, base.velocityY, base.velocityZ);
                float horizontalSpeed = MathHelper.Sqrt(base.velocityX * base.velocityX + base.velocityZ * base.velocityZ);
                yaw = (float)(System.Math.Atan2(base.velocityX, base.velocityZ) * 180.0D / (double)((float)System.Math.PI));

                for (pitch = (float)(System.Math.Atan2(base.velocityY, (double)horizontalSpeed) * 180.0D / (double)((float)System.Math.PI)); pitch - prevPitch < -180.0F; prevPitch -= 360.0F)
                {
                }

                while (pitch - prevPitch >= 180.0F)
                {
                    prevPitch += 360.0F;
                }

                while (yaw - prevYaw < -180.0F)
                {
                    prevYaw -= 360.0F;
                }

                while (yaw - prevYaw >= 180.0F)
                {
                    prevYaw += 360.0F;
                }

                pitch = prevPitch + (pitch - prevPitch) * 0.2F;
                yaw = prevYaw + (yaw - prevYaw) * 0.2F;
                float drag = 0.92F;
                if (onGround || horizontalCollison)
                {
                    drag = 0.5F;
                }

                byte waterCheckSegments = 5;
                double waterSubmersion = 0.0D;

                for (int segment = 0; segment < waterCheckSegments; ++segment)
                {
                    double segmentBottom = boundingBox.MinY + (boundingBox.MaxY - boundingBox.MinY) * (double)(segment + 0) / (double)waterCheckSegments - 0.125D + 0.125D;
                    double segmentTop = boundingBox.MinY + (boundingBox.MaxY - boundingBox.MinY) * (double)(segment + 1) / (double)waterCheckSegments - 0.125D + 0.125D;
                    Box segmentBox = new Box(boundingBox.MinX, segmentBottom, boundingBox.MinZ, boundingBox.MaxX, segmentTop, boundingBox.MaxZ);
                    if (world.Reader.IsMaterialInBox(segmentBox, m => m == Material.Water))
                    {
                        waterSubmersion += 1.0D / (double)waterCheckSegments;
                    }
                }

                if (waterSubmersion > 0.0D)
                {
                    if (ticksCatchable > 0)
                    {
                        --ticksCatchable;
                    }
                    else
                    {
                        short catchDelay = 500;
                        if (world.Environment.IsRainingAt(MathHelper.Floor(x), MathHelper.Floor(y) + 1, MathHelper.Floor(z)))
                        {
                            catchDelay = 300;
                        }

                        if (random.NextInt(catchDelay) == 0)
                        {
                            ticksCatchable = random.NextInt(30) + 10;
                            base.velocityY -= (double)0.2F;
                            world.Broadcaster.PlaySoundAtEntity(this, "random.splash", 0.25F, 1.0F + (random.NextFloat() - random.NextFloat()) * 0.4F);
                            float waterSurface = (float)MathHelper.Floor(boundingBox.MinY);

                            int particle;
                            float offsetX;
                            float offsetZ;
                            for (particle = 0; (float)particle < 1.0F + width * 20.0F; ++particle)
                            {
                                offsetX = (random.NextFloat() * 2.0F - 1.0F) * width;
                                offsetZ = (random.NextFloat() * 2.0F - 1.0F) * width;
                                world.Broadcaster.AddParticle("bubble", x + (double)offsetX, (double)(waterSurface + 1.0F), z + (double)offsetZ, base.velocityX, base.velocityY - (double)(random.NextFloat() * 0.2F), base.velocityZ);
                            }

                            for (particle = 0; (float)particle < 1.0F + width * 20.0F; ++particle)
                            {
                                offsetX = (random.NextFloat() * 2.0F - 1.0F) * width;
                                offsetZ = (random.NextFloat() * 2.0F - 1.0F) * width;
                                world.Broadcaster.AddParticle("splash", x + (double)offsetX, (double)(waterSurface + 1.0F), z + (double)offsetZ, base.velocityX, base.velocityY, base.velocityZ);
                            }
                        }
                    }
                }

                if (ticksCatchable > 0)
                {
                    base.velocityY -= (double)(random.NextFloat() * random.NextFloat() * random.NextFloat()) * 0.2D;
                }

                buoyancy = waterSubmersion * 2.0D - 1.0D;
                base.velocityY += (double)0.04F * buoyancy;
                if (waterSubmersion > 0.0D)
                {
                    drag = (float)((double)drag * 0.9D);
                    base.velocityY *= 0.8D;
                }

                base.velocityX *= (double)drag;
                base.velocityY *= (double)drag;
                base.velocityZ *= (double)drag;
                setPosition(x, y, z);
            }
        }
    }

    public override void writeNbt(NBTTagCompound nbt)
    {
        nbt.SetShort("xTile", (short)xTile);
        nbt.SetShort("yTile", (short)yTile);
        nbt.SetShort("zTile", (short)zTile);
        nbt.SetByte("inTile", (sbyte)inTile);
        nbt.SetByte("shake", (sbyte)shake);
        nbt.SetByte("inGround", (sbyte)(inGround ? 1 : 0));
    }

    public override void readNbt(NBTTagCompound nbt)
    {
        xTile = nbt.GetShort("xTile");
        yTile = nbt.GetShort("yTile");
        zTile = nbt.GetShort("zTile");
        inTile = nbt.GetByte("inTile") & 255;
        shake = nbt.GetByte("shake") & 255;
        inGround = nbt.GetByte("inGround") == 1;
    }

    public override float getShadowRadius()
    {
        return 0.0F;
    }

    public int catchFish()
    {
        byte result = 0;
        if (bobber != null)
        {
            double deltaX = angler.x - x;
            double deltaY = angler.y - y;
            double deltaZ = angler.z - z;
            double distance = (double)MathHelper.Sqrt(deltaX * deltaX + deltaY * deltaY + deltaZ * deltaZ);
            double pullStrength = 0.1D;
            bobber.velocityX += deltaX * pullStrength;
            bobber.velocityY += deltaY * pullStrength + (double)MathHelper.Sqrt(distance) * 0.08D;
            bobber.velocityZ += deltaZ * pullStrength;
            result = 3;
        }
        else if (ticksCatchable > 0)
        {
            EntityItem fishItem = new EntityItem(world, x, y, z, new ItemStack(Item.RawFish));
            double deltaX = angler.x - x;
            double deltaY = angler.y - y;
            double deltaZ = angler.z - z;
            double distance = (double)MathHelper.Sqrt(deltaX * deltaX + deltaY * deltaY + deltaZ * deltaZ);
            double pullStrength = 0.1D;
            fishItem.velocityX = deltaX * pullStrength;
            fishItem.velocityY = deltaY * pullStrength + (double)MathHelper.Sqrt(distance) * 0.08D;
            fishItem.velocityZ = deltaZ * pullStrength;
            world.SpawnEntity(fishItem);
            angler.increaseStat(Stats.Stats.FishCaughtStat, 1);
            result = 1;
        }

        if (inGround)
        {
            result = 2;
        }

        markDead();
        angler.fishHook = null;
        return result;
    }
}
