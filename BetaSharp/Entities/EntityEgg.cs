using BetaSharp.Items;
using BetaSharp.NBT;
using BetaSharp.Util.Hit;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Entities;

public class EntityEgg : Entity
{
    public override EntityType Type => EntityRegistry.Egg;
    private int xTile = -1;
    private int yTile = -1;
    private int zTile = -1;
    private int inTile;
    private bool inGround;
    public int shake;
    private EntityLiving thrower;
    private int ticksInGround;
    private int ticksInAir;

    public EntityEgg(IWorldContext world) : base(world)
    {
        SetBoundingBoxSpacing(0.25F, 0.25F);
    }


    public override bool ShouldRender(double distanceSquared)
    {
        double renderDistance = BoundingBox.AverageEdgeLength * 4.0D;
        renderDistance *= 64.0D;
        return distanceSquared < renderDistance * renderDistance;
    }

    public EntityEgg(IWorldContext world, EntityLiving owner) : base(world)
    {
        thrower = owner;
        SetBoundingBoxSpacing(0.25F, 0.25F);
        SetPositionAndAnglesKeepPrevAngles(owner.X, owner.Y + (double)owner.GetEyeHeight(), owner.Z, owner.Yaw, owner.Pitch);
        X -= (double)(MathHelper.Cos(Yaw / 180.0F * (float)Math.PI) * 0.16F);
        Y -= (double)0.1F;
        Z -= (double)(MathHelper.Sin(Yaw / 180.0F * (float)Math.PI) * 0.16F);
        SetPosition(X, Y, Z);
        StandingEyeHeight = 0.0F;
        float speed = 0.4F;
        VelocityX = (double)(-MathHelper.Sin(Yaw / 180.0F * (float)Math.PI) * MathHelper.Cos(Pitch / 180.0F * (float)Math.PI) * speed);
        VelocityZ = (double)(MathHelper.Cos(Yaw / 180.0F * (float)Math.PI) * MathHelper.Cos(Pitch / 180.0F * (float)Math.PI) * speed);
        VelocityY = (double)(-MathHelper.Sin(Pitch / 180.0F * (float)Math.PI) * speed);
        setHeading(VelocityX, VelocityY, VelocityZ, 1.5F, 1.0F);
    }

    public EntityEgg(IWorldContext world, double x, double y, double z) : base(world)
    {
        ticksInGround = 0;
        SetBoundingBoxSpacing(0.25F, 0.25F);
        SetPosition(x, y, z);
        StandingEyeHeight = 0.0F;
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
        VelocityX = dirX;
        VelocityY = dirY;
        VelocityZ = dirZ;
        float horizontalLength = MathHelper.Sqrt(dirX * dirX + dirZ * dirZ);
        PrevYaw = Yaw = (float)(System.Math.Atan2(dirX, dirZ) * 180.0D / (double)((float)Math.PI));
        PrevPitch = Pitch = (float)(System.Math.Atan2(dirY, (double)horizontalLength) * 180.0D / (double)((float)Math.PI));
        ticksInGround = 0;
    }

    public override void SetVelocityClient(double motionX, double motionY, double motionZ)
    {
        VelocityX = motionX;
        VelocityY = motionY;
        VelocityZ = motionZ;
        if (PrevPitch == 0.0F && PrevYaw == 0.0F)
        {
            float horizontalLength = MathHelper.Sqrt(motionX * motionX + motionZ * motionZ);
            PrevYaw = Yaw = (float)(System.Math.Atan2(motionX, motionZ) * 180.0D / (double)((float)Math.PI));
            PrevPitch = Pitch = (float)(System.Math.Atan2(motionY, (double)horizontalLength) * 180.0D / (double)((float)Math.PI));
        }

    }

    public override void Tick()
    {
        LastTickX = X;
        LastTickY = Y;
        LastTickZ = Z;
        base.Tick();
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
            VelocityX *= (double)(Random.NextFloat() * 0.2F);
            VelocityY *= (double)(Random.NextFloat() * 0.2F);
            VelocityZ *= (double)(Random.NextFloat() * 0.2F);
            ticksInGround = 0;
            ticksInAir = 0;
        }
        else
        {
            ++ticksInAir;
        }

        Vec3D rayStart = new Vec3D(X, Y, Z);
        Vec3D rayEnd = new Vec3D(X + VelocityX, Y + VelocityY, Z + VelocityZ);
        HitResult hit = World.Reader.Raycast(rayStart, rayEnd);
        rayStart = new Vec3D(X, Y, Z);
        rayEnd = new Vec3D(X + VelocityX, Y + VelocityY, Z + VelocityZ);
        if (hit.Type != HitResultType.MISS)
        {
            rayEnd = new Vec3D(hit.Pos.x, hit.Pos.y, hit.Pos.z);
        }

        if (!World.IsRemote)
        {
            Entity hitEntity = null;
            var entities = World.Entities.GetEntities(this, BoundingBox.Stretch(VelocityX, VelocityY, VelocityZ).Expand(1.0D, 1.0D, 1.0D));
            double minHitDistance = 0.0D;

            for (int i = 0; i < entities.Count; ++i)
            {
                Entity entity = entities[i];
                if (entity.IsCollidable() && (entity != thrower || ticksInAir >= 5))
                {
                    float expandAmount = 0.3F;
                    Box expandedBox = entity.BoundingBox.Expand((double)expandAmount, (double)expandAmount, (double)expandAmount);
                    HitResult entityHit = expandedBox.Raycast(rayStart, rayEnd);
                    if (entityHit.Type != HitResultType.MISS)
                    {
                        double distance = rayStart.distanceTo(entityHit.Pos);
                        if (distance < minHitDistance || minHitDistance == 0.0D)
                        {
                            hitEntity = entity;
                            minHitDistance = distance;
                        }
                    }
                }
            }

            if (hitEntity != null)
            {
                hit = new HitResult(hitEntity);
            }
        }

        if (hit.Type != HitResultType.MISS)
        {
            if (hit.Entity != null && hit.Entity.Damage(thrower, 0))
            {
            }

            if (!World.IsRemote && Random.NextInt(8) == 0)
            {
                byte chickenCount = 1;
                if (Random.NextInt(32) == 0)
                {
                    chickenCount = 4;
                }

                for (int i = 0; i < chickenCount; ++i)
                {
                    EntityChicken chicken = new EntityChicken(World);
                    chicken.SetPositionAndAnglesKeepPrevAngles(X, Y, Z, Yaw, 0.0F);
                    World.SpawnEntity(chicken);
                }
            }

            for (int i = 0; i < 8; ++i)
            {
                World.Broadcaster.AddParticle("snowballpoof", X, Y, Z, 0.0D, 0.0D, 0.0D);
            }

            MarkDead();
        }

        X += VelocityX;
        Y += VelocityY;
        Z += VelocityZ;
        float horizontalSpeed = MathHelper.Sqrt(VelocityX * VelocityX + VelocityZ * VelocityZ);
        Yaw = (float)(System.Math.Atan2(VelocityX, VelocityZ) * 180.0D / (double)((float)Math.PI));

        for (Pitch = (float)(System.Math.Atan2(VelocityY, (double)horizontalSpeed) * 180.0D / (double)((float)Math.PI)); Pitch - PrevPitch < -180.0F; PrevPitch -= 360.0F)
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
        float drag = 0.99F;
        float gravity = 0.03F;
        if (IsInWater())
        {
            for (int i = 0; i < 4; ++i)
            {
                float trailOffset = 0.25F;
                World.Broadcaster.AddParticle("bubble", X - VelocityX * (double)trailOffset, Y - VelocityY * (double)trailOffset, Z - VelocityZ * (double)trailOffset, VelocityX, VelocityY, VelocityZ);
            }

            drag = 0.8F;
        }

        VelocityX *= (double)drag;
        VelocityY *= (double)drag;
        VelocityZ *= (double)drag;
        VelocityY -= (double)gravity;
        SetPosition(X, Y, Z);
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

    public override void OnPlayerInteraction(EntityPlayer player)
    {
        if (inGround && thrower == player && shake <= 0 && player.inventory.AddItemStackToInventory(new ItemStack(Item.ARROW, 1)))
        {
            World.Broadcaster.PlaySoundAtEntity(this, "random.pop", 0.2F, ((Random.NextFloat() - Random.NextFloat()) * 0.7F + 1.0F) * 2.0F);
            player.sendPickup(this, 1);
            MarkDead();
        }

    }

    public override float GetShadowRadius()
    {
        return 0.0F;
    }
}
