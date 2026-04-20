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
        SetBoundingBoxSpacing(0.25F, 0.25F);
        IgnoreFrustumCheck = true;
    }

    public EntityFish(IWorldContext world, double x, double y, double z) : this(world)
    {
        SetPosition(x, y, z);
        IgnoreFrustumCheck = true;
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
        IgnoreFrustumCheck = true;
        angler = player;
        angler.fishHook = this;
        SetBoundingBoxSpacing(0.25F, 0.25F);
        SetPositionAndAnglesKeepPrevAngles(player.X, player.Y + 1.62D - (double)player.StandingEyeHeight, player.Z, player.Yaw, player.Pitch);
        X -= (double)(MathHelper.Cos(Yaw / 180.0F * (float)System.Math.PI) * 0.16F);
        Y -= (double)0.1F;
        Z -= (double)(MathHelper.Sin(Yaw / 180.0F * (float)System.Math.PI) * 0.16F);
        SetPosition(X, Y, Z);
        StandingEyeHeight = 0.0F;
        float speed = 0.4F;
        base.VelocityX = (double)(-MathHelper.Sin(Yaw / 180.0F * (float)System.Math.PI) * MathHelper.Cos(Pitch / 180.0F * (float)System.Math.PI) * speed);
        base.VelocityZ = (double)(MathHelper.Cos(Yaw / 180.0F * (float)System.Math.PI) * MathHelper.Cos(Pitch / 180.0F * (float)System.Math.PI) * speed);
        base.VelocityY = (double)(-MathHelper.Sin(Pitch / 180.0F * (float)System.Math.PI) * speed);
        setHeading(base.VelocityX, base.VelocityY, base.VelocityZ, 1.5F, 1.0F);
    }


    public override bool ShouldRender(double distanceSquared)
    {
        double renderDistance = BoundingBox.AverageEdgeLength * 4.0D;
        renderDistance *= 64.0D;
        return distanceSquared < renderDistance * renderDistance;
    }

    public void setHeading(double dirX, double dirY, double dirZ, float speed, float spread)
    {
        float length = MathHelper.Sqrt(dirX * dirX + dirY * dirY + dirZ * dirZ);
        dirX /= (double)length;
        dirY /= (double)length;
        dirZ /= (double)length;
        dirX += Random.NextGaussian() * (double)0.0075F * (double)spread;
        dirY += Random.NextGaussian() * (double)0.0075F * (double)spread;
        dirZ += Random.NextGaussian() * (double)0.0075F * (double)spread;
        dirX *= (double)speed;
        dirY *= (double)speed;
        dirZ *= (double)speed;
        base.VelocityX = dirX;
        base.VelocityY = dirY;
        base.VelocityZ = dirZ;
        float horizontalLength = MathHelper.Sqrt(dirX * dirX + dirZ * dirZ);
        PrevYaw = Yaw = (float)(System.Math.Atan2(dirX, dirZ) * 180.0D / (double)((float)System.Math.PI));
        PrevPitch = Pitch = (float)(System.Math.Atan2(dirY, (double)horizontalLength) * 180.0D / (double)((float)System.Math.PI));
        ticksInGround = 0;
    }

    public override void SetPositionAndAnglesAvoidEntities(double newX, double newY, double newZ, float newYaw, float newPitch, int interpolationSteps)
    {
        targetX = newX;
        targetY = newY;
        targetZ = newZ;
        targetYaw = (double)newYaw;
        targetPitch = (double)newPitch;
        positionUpdateTicks = interpolationSteps;
        base.VelocityX = clientVelocityX;
        base.VelocityY = clientVelocityY;
        base.VelocityZ = clientVelocityZ;
    }

    public override void SetVelocityClient(double motionX, double motionY, double motionZ)
    {
        clientVelocityX = base.VelocityX = motionX;
        clientVelocityY = base.VelocityY = motionY;
        clientVelocityZ = base.VelocityZ = motionZ;
    }

    public override void Tick()
    {
        base.Tick();
        if (positionUpdateTicks > 0)
        {
            double interpX = X + (targetX - X) / (double)positionUpdateTicks;
            double interpY = Y + (targetY - Y) / (double)positionUpdateTicks;
            double interpZ = Z + (targetZ - Z) / (double)positionUpdateTicks;

            double yawDelta;
            for (yawDelta = targetYaw - (double)Yaw; yawDelta < -180.0D; yawDelta += 360.0D)
            {
            }

            while (yawDelta >= 180.0D)
            {
                yawDelta -= 360.0D;
            }

            Yaw = (float)((double)Yaw + yawDelta / (double)positionUpdateTicks);
            Pitch = (float)((double)Pitch + (targetPitch - (double)Pitch) / (double)positionUpdateTicks);
            --positionUpdateTicks;
            SetPosition(interpX, interpY, interpZ);
            SetRotation(Yaw, Pitch);
        }
        else
        {
            if (!World.IsRemote)
            {
                ItemStack heldItem = angler.getHand();
                if (angler.Dead || !angler.IsAlive() || heldItem == null || heldItem.getItem() != Item.FishingRod || GetSquaredDistance(angler) > 1024.0D)
                {
                    MarkDead();
                    angler.fishHook = null;
                    return;
                }

                if (bobber != null)
                {
                    if (!bobber.Dead)
                    {
                        X = bobber.X;
                        Y = bobber.BoundingBox.MinY + (double)bobber.Height * 0.8D;
                        Z = bobber.Z;
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
                int blockId = World.Reader.GetBlockId(xTile, yTile, zTile);
                if (blockId == inTile)
                {
                    ++ticksInGround;
                    if (ticksInGround == 1200)
                    {
                        MarkDead();
                    }

                    return;
                }

                inGround = false;
                base.VelocityX *= (double)(Random.NextFloat() * 0.2F);
                base.VelocityY *= (double)(Random.NextFloat() * 0.2F);
                base.VelocityZ *= (double)(Random.NextFloat() * 0.2F);
                ticksInGround = 0;
                ticksInAir = 0;
            }
            else
            {
                ++ticksInAir;
            }

            Vec3D rayStart = new Vec3D(X, Y, Z);
            Vec3D rayEnd = new Vec3D(X + base.VelocityX, Y + base.VelocityY, Z + base.VelocityZ);
            HitResult hit = World.Reader.Raycast(rayStart, rayEnd);
            rayStart = new Vec3D(X, Y, Z);
            rayEnd = new Vec3D(X + base.VelocityX, Y + base.VelocityY, Z + base.VelocityZ);
            if (hit.Type != HitResultType.MISS)
            {
                rayEnd = new Vec3D(hit.Pos.x, hit.Pos.y, hit.Pos.z);
            }

            Entity hitEntity = null;
            var entities = World.Entities.GetEntities(this, BoundingBox.Stretch(base.VelocityX, base.VelocityY, base.VelocityZ).Expand(1.0D, 1.0D, 1.0D));
            double minHitDistance = 0.0D;

            double buoyancy;
            for (int i = 0; i < entities.Count; ++i)
            {
                Entity entity = entities[i];
                if (entity.IsCollidable() && (entity != angler || ticksInAir >= 5))
                {
                    float expandAmount = 0.3F;
                    Box expandedBox = entity.BoundingBox.Expand((double)expandAmount, (double)expandAmount, (double)expandAmount);
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
                    if (hit.Entity.Damage(angler, 0))
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
                base.Move(base.VelocityX, base.VelocityY, base.VelocityZ);
                float horizontalSpeed = MathHelper.Sqrt(base.VelocityX * base.VelocityX + base.VelocityZ * base.VelocityZ);
                Yaw = (float)(System.Math.Atan2(base.VelocityX, base.VelocityZ) * 180.0D / (double)((float)System.Math.PI));

                for (Pitch = (float)(System.Math.Atan2(base.VelocityY, (double)horizontalSpeed) * 180.0D / (double)((float)System.Math.PI)); Pitch - PrevPitch < -180.0F; PrevPitch -= 360.0F)
                {
                }

                while (Pitch - PrevPitch >= 180.0F)
                {
                    PrevPitch += 360.0F;
                }

                while (Yaw - PrevYaw < -180.0F)
                {
                    PrevYaw -= 360.0F;
                }

                while (Yaw - PrevYaw >= 180.0F)
                {
                    PrevYaw += 360.0F;
                }

                Pitch = PrevPitch + (Pitch - PrevPitch) * 0.2F;
                Yaw = PrevYaw + (Yaw - PrevYaw) * 0.2F;
                float drag = 0.92F;
                if (OnGround || HorizontalCollison)
                {
                    drag = 0.5F;
                }

                byte waterCheckSegments = 5;
                double waterSubmersion = 0.0D;

                for (int segment = 0; segment < waterCheckSegments; ++segment)
                {
                    double segmentBottom = BoundingBox.MinY + (BoundingBox.MaxY - BoundingBox.MinY) * (double)(segment + 0) / (double)waterCheckSegments - 0.125D + 0.125D;
                    double segmentTop = BoundingBox.MinY + (BoundingBox.MaxY - BoundingBox.MinY) * (double)(segment + 1) / (double)waterCheckSegments - 0.125D + 0.125D;
                    Box segmentBox = new Box(BoundingBox.MinX, segmentBottom, BoundingBox.MinZ, BoundingBox.MaxX, segmentTop, BoundingBox.MaxZ);
                    if (World.Reader.IsMaterialInBox(segmentBox, m => m == Material.Water))
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
                        if (World.Environment.IsRainingAt(MathHelper.Floor(X), MathHelper.Floor(Y) + 1, MathHelper.Floor(Z)))
                        {
                            catchDelay = 300;
                        }

                        if (Random.NextInt(catchDelay) == 0)
                        {
                            ticksCatchable = Random.NextInt(30) + 10;
                            base.VelocityY -= (double)0.2F;
                            World.Broadcaster.PlaySoundAtEntity(this, "random.splash", 0.25F, 1.0F + (Random.NextFloat() - Random.NextFloat()) * 0.4F);
                            float waterSurface = (float)MathHelper.Floor(BoundingBox.MinY);

                            int particle;
                            float offsetX;
                            float offsetZ;
                            for (particle = 0; (float)particle < 1.0F + Width * 20.0F; ++particle)
                            {
                                offsetX = (Random.NextFloat() * 2.0F - 1.0F) * Width;
                                offsetZ = (Random.NextFloat() * 2.0F - 1.0F) * Width;
                                World.Broadcaster.AddParticle("bubble", X + (double)offsetX, (double)(waterSurface + 1.0F), Z + (double)offsetZ, base.VelocityX, base.VelocityY - (double)(Random.NextFloat() * 0.2F), base.VelocityZ);
                            }

                            for (particle = 0; (float)particle < 1.0F + Width * 20.0F; ++particle)
                            {
                                offsetX = (Random.NextFloat() * 2.0F - 1.0F) * Width;
                                offsetZ = (Random.NextFloat() * 2.0F - 1.0F) * Width;
                                World.Broadcaster.AddParticle("splash", X + (double)offsetX, (double)(waterSurface + 1.0F), Z + (double)offsetZ, base.VelocityX, base.VelocityY, base.VelocityZ);
                            }
                        }
                    }
                }

                if (ticksCatchable > 0)
                {
                    base.VelocityY -= (double)(Random.NextFloat() * Random.NextFloat() * Random.NextFloat()) * 0.2D;
                }

                buoyancy = waterSubmersion * 2.0D - 1.0D;
                base.VelocityY += (double)0.04F * buoyancy;
                if (waterSubmersion > 0.0D)
                {
                    drag = (float)((double)drag * 0.9D);
                    base.VelocityY *= 0.8D;
                }

                base.VelocityX *= (double)drag;
                base.VelocityY *= (double)drag;
                base.VelocityZ *= (double)drag;
                SetPosition(X, Y, Z);
            }
        }
    }

    public override void WriteNbt(NBTTagCompound nbt)
    {
        nbt.SetShort("xTile", (short)xTile);
        nbt.SetShort("yTile", (short)yTile);
        nbt.SetShort("zTile", (short)zTile);
        nbt.SetByte("inTile", (sbyte)inTile);
        nbt.SetByte("shake", (sbyte)shake);
        nbt.SetByte("inGround", (sbyte)(inGround ? 1 : 0));
    }

    public override void ReadNbt(NBTTagCompound nbt)
    {
        xTile = nbt.GetShort("xTile");
        yTile = nbt.GetShort("yTile");
        zTile = nbt.GetShort("zTile");
        inTile = nbt.GetByte("inTile") & 255;
        shake = nbt.GetByte("shake") & 255;
        inGround = nbt.GetByte("inGround") == 1;
    }

    public override float GetShadowRadius()
    {
        return 0.0F;
    }

    public int catchFish()
    {
        byte result = 0;
        if (bobber != null)
        {
            double deltaX = angler.X - X;
            double deltaY = angler.Y - Y;
            double deltaZ = angler.Z - Z;
            double distance = (double)MathHelper.Sqrt(deltaX * deltaX + deltaY * deltaY + deltaZ * deltaZ);
            double pullStrength = 0.1D;
            bobber.VelocityX += deltaX * pullStrength;
            bobber.VelocityY += deltaY * pullStrength + (double)MathHelper.Sqrt(distance) * 0.08D;
            bobber.VelocityZ += deltaZ * pullStrength;
            result = 3;
        }
        else if (ticksCatchable > 0)
        {
            EntityItem fishItem = new EntityItem(World, X, Y, Z, new ItemStack(Item.RawFish));
            double deltaX = angler.X - X;
            double deltaY = angler.Y - Y;
            double deltaZ = angler.Z - Z;
            double distance = (double)MathHelper.Sqrt(deltaX * deltaX + deltaY * deltaY + deltaZ * deltaZ);
            double pullStrength = 0.1D;
            fishItem.VelocityX = deltaX * pullStrength;
            fishItem.VelocityY = deltaY * pullStrength + (double)MathHelper.Sqrt(distance) * 0.08D;
            fishItem.VelocityZ = deltaZ * pullStrength;
            World.SpawnEntity(fishItem);
            angler.increaseStat(Stats.Stats.FishCaughtStat, 1);
            result = 1;
        }

        if (inGround)
        {
            result = 2;
        }

        MarkDead();
        angler.fishHook = null;
        return result;
    }
}
