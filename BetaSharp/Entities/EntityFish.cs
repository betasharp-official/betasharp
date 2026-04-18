using BetaSharp.Blocks.Materials;
using BetaSharp.Items;
using BetaSharp.NBT;
using BetaSharp.Util.Hit;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Entities;

public class EntityFish : Entity
{
    private const double PullStrength = 0.1D;
    private Entity? _bobber;
    private double _clientVelocityX;
    private double _clientVelocityY;
    private double _clientVelocityZ;
    private bool _inGround;
    private int _inTile;
    private int _positionUpdateTicks;
    private int _shake;
    private double _targetPitch;
    private double _targetX;
    private double _targetY;
    private double _targetYaw;
    private double _targetZ;
    private int _ticksCatchable;
    private int _ticksInAir;
    private int _ticksInGround;
    private BlockPos _tile = new(-1, -1, -1);
    public EntityPlayer? Angler;

    public EntityFish(IWorldContext world) : base(world)
    {
        _inTile = 0;
        _inGround = false;
        _shake = 0;
        _ticksInAir = 0;
        _ticksCatchable = 0;
        _bobber = null;
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
        _inTile = 0;
        _inGround = false;
        _shake = 0;
        _ticksInAir = 0;
        _ticksCatchable = 0;
        _bobber = null;
        IgnoreFrustumCheck = true;
        Angler = player;
        Angler.FishHook = this;
        SetBoundingBoxSpacing(0.25F, 0.25F);
        SetPositionAndAnglesKeepPrevAngles(player.X, player.Y + 1.62D - player.StandingEyeHeight, player.Z, player.Yaw, player.Pitch);
        X -= MathHelper.Cos(Yaw / 180.0F * (float)Math.PI) * 0.16F;
        Y -= 0.1F;
        Z -= MathHelper.Sin(Yaw / 180.0F * (float)Math.PI) * 0.16F;
        SetPosition(X, Y, Z);
        StandingEyeHeight = 0.0F;
        const float speed = 0.4F;
        VelocityX = -MathHelper.Sin(Yaw / 180.0F * (float)Math.PI) * MathHelper.Cos(Pitch / 180.0F * (float)Math.PI) * speed;
        VelocityZ = MathHelper.Cos(Yaw / 180.0F * (float)Math.PI) * MathHelper.Cos(Pitch / 180.0F * (float)Math.PI) * speed;
        VelocityY = -MathHelper.Sin(Pitch / 180.0F * (float)Math.PI) * speed;
        SetHeading(VelocityX, VelocityY, VelocityZ, 1.5F, 1.0F);
    }

    public override EntityType Type => EntityRegistry.FishHook;

    protected sealed override void SetBoundingBoxSpacing(float widthOffset, float heightOffset) => base.SetBoundingBoxSpacing(widthOffset, heightOffset);


    protected override bool ShouldRender(double distanceSquared)
    {
        double renderDistance = BoundingBox.AverageEdgeLength * 4.0D;
        renderDistance *= 64.0D;
        return distanceSquared < renderDistance * renderDistance;
    }

    private void SetHeading(double dirX, double dirY, double dirZ, float speed, float spread)
    {
        float length = MathHelper.Sqrt(dirX * dirX + dirY * dirY + dirZ * dirZ);
        dirX /= length;
        dirY /= length;
        dirZ /= length;
        dirX += Random.NextGaussian() * 0.0075F * spread;
        dirY += Random.NextGaussian() * 0.0075F * spread;
        dirZ += Random.NextGaussian() * 0.0075F * spread;
        dirX *= speed;
        dirY *= speed;
        dirZ *= speed;
        VelocityX = dirX;
        VelocityY = dirY;
        VelocityZ = dirZ;
        float horizontalLength = MathHelper.Sqrt(dirX * dirX + dirZ * dirZ);
        PrevYaw = Yaw = (float)(Math.Atan2(dirX, dirZ) * 180.0D / (float)Math.PI);
        PrevPitch = Pitch = (float)(Math.Atan2(dirY, horizontalLength) * 180.0D / (float)Math.PI);
        _ticksInGround = 0;
    }

    public override void SetPositionAndAnglesAvoidEntities(double newX, double newY, double newZ, float newYaw, float newPitch, int interpolationSteps)
    {
        _targetX = newX;
        _targetY = newY;
        _targetZ = newZ;
        _targetYaw = newYaw;
        _targetPitch = newPitch;
        _positionUpdateTicks = interpolationSteps;
        VelocityX = _clientVelocityX;
        VelocityY = _clientVelocityY;
        VelocityZ = _clientVelocityZ;
    }

    public override void SetVelocityClient(double motionX, double motionY, double motionZ)
    {
        _clientVelocityX = VelocityX = motionX;
        _clientVelocityY = VelocityY = motionY;
        _clientVelocityZ = VelocityZ = motionZ;
    }

    public override void Tick()
    {
        base.Tick();
        if (_positionUpdateTicks > 0)
        {
            double interpX = X + (_targetX - X) / _positionUpdateTicks;
            double interpY = Y + (_targetY - Y) / _positionUpdateTicks;
            double interpZ = Z + (_targetZ - Z) / _positionUpdateTicks;

            double yawDelta = _targetYaw - Yaw;

            while (yawDelta < -180.0D) yawDelta += 360.0D;
            while (yawDelta >= 180.0D) yawDelta -= 360.0D;

            Yaw = (float)(Yaw + yawDelta / _positionUpdateTicks);
            Pitch = (float)(Pitch + (_targetPitch - Pitch) / _positionUpdateTicks);
            --_positionUpdateTicks;
            SetPosition(interpX, interpY, interpZ);
            SetRotation(Yaw, Pitch);
            return;
        }

        if (!World.IsRemote)
        {
            ItemStack? heldItem = Angler?.GetHand();
            if (Angler != null && (Angler.Dead || !Angler.IsAlive || heldItem == null || heldItem.getItem() != Item.FishingRod || GetSquaredDistance(Angler) > 1024.0D))
            {
                MarkDead();
                Angler.FishHook = null;
                return;
            }

            if (_bobber != null)
            {
                if (!_bobber.Dead)
                {
                    X = _bobber.X;
                    Y = _bobber.BoundingBox.MinY + _bobber.Height * 0.8D;
                    Z = _bobber.Z;
                    return;
                }

                _bobber = null;
            }
        }

        if (_shake > 0)
        {
            --_shake;
        }

        if (_inGround)
        {
            int blockId = World.Reader.GetBlockId(_tile.x, _tile.y, _tile.z);
            if (blockId == _inTile)
            {
                ++_ticksInGround;
                if (_ticksInGround == 1200)
                {
                    MarkDead();
                }

                return;
            }

            _inGround = false;
            VelocityX *= Random.NextFloat() * 0.2F;
            VelocityY *= Random.NextFloat() * 0.2F;
            VelocityZ *= Random.NextFloat() * 0.2F;
            _ticksInGround = 0;
            _ticksInAir = 0;
        }
        else
        {
            ++_ticksInAir;
        }

        Vec3D rayStart = new(X, Y, Z);
        Vec3D rayEnd = new(X + VelocityX, Y + VelocityY, Z + VelocityZ);
        HitResult hit = World.Reader.Raycast(rayStart, rayEnd);
        rayStart = new Vec3D(X, Y, Z);
        rayEnd = new Vec3D(X + VelocityX, Y + VelocityY, Z + VelocityZ);
        if (hit.Type != HitResultType.MISS)
        {
            rayEnd = new Vec3D(hit.Pos.x, hit.Pos.y, hit.Pos.z);
        }

        Entity? hitEntity = null;
        List<Entity> entities = World.Entities.GetEntities(this, BoundingBox.Stretch(VelocityX, VelocityY, VelocityZ).Expand(1.0D, 1.0D, 1.0D));
        double minHitDistance = 0.0D;

        double buoyancy;
        foreach (Entity entity in entities)
        {
            if (!entity.HasCollision || (Equals(entity, Angler) && _ticksInAir < 5)) continue;

            const float expandAmount = 0.3F;
            Box expandedBox = entity.BoundingBox.Expand(expandAmount, expandAmount, expandAmount);
            HitResult entityHit = expandedBox.Raycast(rayStart, rayEnd);
            if (entityHit.Type == HitResultType.MISS) continue;

            buoyancy = rayStart.distanceTo(entityHit.Pos);
            if (!(buoyancy < minHitDistance) && minHitDistance != 0.0D) continue;

            hitEntity = entity;
            minHitDistance = buoyancy;
        }

        if (hitEntity != null)
        {
            hit = new HitResult(hitEntity);
        }

        if (hit.Type != HitResultType.MISS)
        {
            if (hit.Entity != null)
            {
                if (hit.Entity.Damage(Angler, 0))
                {
                    _bobber = hit.Entity;
                }
            }
            else
            {
                _inGround = true;
            }
        }

        if (_inGround) return;

        base.Move(VelocityX, VelocityY, VelocityZ);
        float horizontalSpeed = MathHelper.Sqrt(VelocityX * VelocityX + VelocityZ * VelocityZ);
        Yaw = (float)(Math.Atan2(VelocityX, VelocityZ) * 180.0D / (float)Math.PI);

        Pitch = (float)(Math.Atan2(VelocityY, horizontalSpeed) * 180.0D / Math.PI);

        while (Pitch - PrevPitch < -180.0F) PrevPitch -= 360.0F;
        while (Pitch - PrevPitch >= 180.0F) PrevPitch += 360.0F;

        while (Yaw - PrevYaw < -180.0F) PrevYaw -= 360.0F;
        while (Yaw - PrevYaw >= 180.0F) PrevYaw += 360.0F;

        Pitch = PrevPitch + (Pitch - PrevPitch) * 0.2F;
        Yaw = PrevYaw + (Yaw - PrevYaw) * 0.2F;
        float drag = 0.92F;
        if (OnGround || HorizontalCollision)
        {
            drag = 0.5F;
        }

        byte waterCheckSegments = 5;
        double waterSubmersion = 0.0D;

        for (int segment = 0; segment < waterCheckSegments; ++segment)
        {
            double segmentBottom = BoundingBox.MinY + (BoundingBox.MaxY - BoundingBox.MinY) * (segment + 0) / waterCheckSegments - 0.125D + 0.125D;
            double segmentTop = BoundingBox.MinY + (BoundingBox.MaxY - BoundingBox.MinY) * (segment + 1) / waterCheckSegments - 0.125D + 0.125D;
            Box segmentBox = new(BoundingBox.MinX, segmentBottom, BoundingBox.MinZ, BoundingBox.MaxX, segmentTop, BoundingBox.MaxZ);
            if (World.Reader.IsMaterialInBox(segmentBox, m => m == Material.Water))
            {
                waterSubmersion += 1.0D / waterCheckSegments;
            }
        }

        if (waterSubmersion > 0.0D)
        {
            if (_ticksCatchable > 0)
            {
                --_ticksCatchable;
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
                    _ticksCatchable = Random.NextInt(30) + 10;
                    VelocityY -= 0.2F;
                    World.Broadcaster.PlaySoundAtEntity(this, "random.splash", 0.25F, 1.0F + (Random.NextFloat() - Random.NextFloat()) * 0.4F);
                    float waterSurface = MathHelper.Floor(BoundingBox.MinY);

                    int particle;
                    float offsetX;
                    float offsetZ;
                    for (particle = 0; particle < 1.0F + Width * 20.0F; ++particle)
                    {
                        offsetX = (Random.NextFloat() * 2.0F - 1.0F) * Width;
                        offsetZ = (Random.NextFloat() * 2.0F - 1.0F) * Width;
                        World.Broadcaster.AddParticle("bubble", X + offsetX, waterSurface + 1.0F, Z + offsetZ, VelocityX, VelocityY - Random.NextFloat() * 0.2F, VelocityZ);
                    }

                    for (particle = 0; particle < 1.0F + Width * 20.0F; ++particle)
                    {
                        offsetX = (Random.NextFloat() * 2.0F - 1.0F) * Width;
                        offsetZ = (Random.NextFloat() * 2.0F - 1.0F) * Width;
                        World.Broadcaster.AddParticle("splash", X + offsetX, waterSurface + 1.0F, Z + offsetZ, VelocityX, VelocityY, VelocityZ);
                    }
                }
            }
        }

        if (_ticksCatchable > 0)
        {
            VelocityY -= Random.NextFloat() * Random.NextFloat() * Random.NextFloat() * 0.2D;
        }

        buoyancy = waterSubmersion * 2.0D - 1.0D;
        VelocityY += 0.04F * buoyancy;
        if (waterSubmersion > 0.0D)
        {
            drag = (float)(drag * 0.9D);
            VelocityY *= 0.8D;
        }

        VelocityX *= drag;
        VelocityY *= drag;
        VelocityZ *= drag;
        SetPosition(X, Y, Z);
    }

    protected override void WriteNbt(NBTTagCompound nbt)
    {
        nbt.SetShort("xTile", (short)_tile.x);
        nbt.SetShort("yTile", (short)_tile.y);
        nbt.SetShort("zTile", (short)_tile.z);
        nbt.SetByte("inTile", (sbyte)_inTile);
        nbt.SetByte("shake", (sbyte)_shake);
        nbt.SetByte("inGround", (sbyte)(_inGround ? 1 : 0));
    }

    protected override void ReadNbt(NBTTagCompound nbt)
    {
        _tile = new BlockPos(nbt.GetShort("xTile"), nbt.GetShort("yTile"), nbt.GetShort("zTile"));
        _inTile = nbt.GetByte("inTile") & 255;
        _shake = nbt.GetByte("shake") & 255;
        _inGround = nbt.GetByte("inGround") == 1;
    }

    public override float GetShadowRadius() => 0.0F;

    public int catchFish()
    {
        byte result = 0;
        if (_bobber != null)
        {
            if (Angler != null)
            {
                double deltaX = Angler.X - X;
                double deltaY = Angler.Y - Y;
                double deltaZ = Angler.Z - Z;
                double distance = MathHelper.Sqrt(deltaX * deltaX + deltaY * deltaY + deltaZ * deltaZ);

                _bobber.VelocityX += deltaX * PullStrength;
                _bobber.VelocityY += deltaY * PullStrength + MathHelper.Sqrt(distance) * 0.08D;
                _bobber.VelocityZ += deltaZ * PullStrength;
            }

            result = 3;
        }
        else if (_ticksCatchable > 0)
        {
            EntityItem fishItem = new(World, X, Y, Z, new ItemStack(Item.RawFish));
            if (Angler != null)
            {
                double deltaX = Angler.X - X;
                double deltaY = Angler.Y - Y;
                double deltaZ = Angler.Z - Z;
                double distance = MathHelper.Sqrt(deltaX * deltaX + deltaY * deltaY + deltaZ * deltaZ);

                fishItem.VelocityX = deltaX * PullStrength;
                fishItem.VelocityY = deltaY * PullStrength + MathHelper.Sqrt(distance) * 0.08D;
                fishItem.VelocityZ = deltaZ * PullStrength;
            }

            World.SpawnEntity(fishItem);
            Angler?.IncreaseStat(Stats.Stats.FishCaughtStat, 1);
            result = 1;
        }

        if (_inGround) result = 2;

        MarkDead();
        Angler?.FishHook = null;
        return result;
    }
}
