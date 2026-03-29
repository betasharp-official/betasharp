using BetaSharp.Items;
using BetaSharp.NBT;
using BetaSharp.Util;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core.Systems;
using BetaSharp.Worlds.Chunks;

namespace BetaSharp.Entities;

public class EntitySlime : EntityLiving, Monster
{
    public override EntityType Type => EntityRegistry.Slime;
    public readonly SyncedProperty<byte> SlimeSize;
    public float squishAmount;
    public float prevSquishAmount;
    private int slimeJumpDelay;

    public EntitySlime(IWorldContext world) : base(world)
    {
        texture = "/mob/slime.png";
        SlimeSize = DataSynchronizer.MakeProperty<byte>(16, 1);
        int size = 1 << random.NextInt(3);
        standingEyeHeight = 0.0F;
        slimeJumpDelay = random.NextInt(20) + 10;
        setSlimeSize(size);
    }

    public void setSlimeSize(int size)
    {
        SlimeSize.Value = (byte)size;
        setBoundingBoxSpacing(0.6F * (float)size, 0.6F * (float)size);
        health = size * size;
        setPosition(x, y, z);
    }

    public int getSlimeSize()
    {
        return SlimeSize.Value;
    }

    public override void writeNbt(NBTTagCompound nbt)
    {
        base.writeNbt(nbt);
        nbt.SetInteger("Size", getSlimeSize() - 1);
    }

    public override void readNbt(NBTTagCompound nbt)
    {
        base.readNbt(nbt);
        setSlimeSize(nbt.GetInteger("Size") + 1);
    }

    public override void tick()
    {
        prevSquishAmount = squishAmount;
        bool wasOnGround = onGround;
        base.tick();
        if (onGround && !wasOnGround)
        {
            int size = getSlimeSize();

            for (int _ = 0; _ < size * 8; ++_)
            {
                float angle = random.NextFloat() * (float)Math.PI * 2.0F;
                float spread = random.NextFloat() * 0.5F + 0.5F;
                float offsetX = MathHelper.Sin(angle) * (float)size * 0.5F * spread;
                float offsetY = MathHelper.Cos(angle) * (float)size * 0.5F * spread;
                world.Broadcaster.AddParticle("slime", base.x + (double)offsetX, boundingBox.MinY, z + (double)offsetY, 0.0D, 0.0D, 0.0D);
            }

            if (size > 2)
            {
                world.Broadcaster.PlaySoundAtEntity(this, "mob.slime", getSoundVolume(), ((random.NextFloat() - random.NextFloat()) * 0.2F + 1.0F) / 0.8F);
            }

            squishAmount = -0.5F;
        }

        squishAmount *= 0.6F;
    }

    public override void tickLiving()
    {
        func_27021_X();
        EntityPlayer player = world.Entities.GetClosestPlayerTarget(this.x, this.y, this.z, 16.0D);
        if (player != null)
        {
            faceEntity(player, 10.0F, 20.0F);
        }

        if (onGround && slimeJumpDelay-- <= 0)
        {
            slimeJumpDelay = random.NextInt(20) + 10;
            if (player != null)
            {
                slimeJumpDelay /= 3;
            }

            jumping = true;
            if (getSlimeSize() > 1)
            {
                world.Broadcaster.PlaySoundAtEntity(this, "mob.slime", getSoundVolume(), ((random.NextFloat() - random.NextFloat()) * 0.2F + 1.0F) * 0.8F);
            }

            squishAmount = 1.0F;
            sidewaysSpeed = 1.0F - random.NextFloat() * 2.0F;
            forwardSpeed = (float)(1 * getSlimeSize());
        }
        else
        {
            jumping = false;
            if (onGround)
            {
                sidewaysSpeed = forwardSpeed = 0.0F;
            }
        }

    }

    public override void markDead()
    {
        int size = getSlimeSize();
        if (!world.IsRemote && size > 1 && health == 0)
        {
            for (int i = 0; i < 4; ++i)
            {
                float offsetX = ((float)(i % 2) - 0.5F) * (float)size / 4.0F;
                float offsetY = ((float)(i / 2) - 0.5F) * (float)size / 4.0F;
                EntitySlime slime = new EntitySlime(world);
                slime.setSlimeSize(size / 2);
                slime.setPositionAndAnglesKeepPrevAngles(x + (double)offsetX, y + 0.5D, z + (double)offsetY, random.NextFloat() * 360.0F, 0.0F);
                world.SpawnEntity(slime);
            }
        }

        base.markDead();
    }

    public override void onPlayerInteraction(EntityPlayer player)
    {
        int size = getSlimeSize();
        if (size > 1 && canSee(player) && (double)getDistance(player) < 0.6D * (double)size && player.damage(this, size))
        {
            world.Broadcaster.PlaySoundAtEntity(this, "mob.slimeattack", 1.0F, (random.NextFloat() - random.NextFloat()) * 0.2F + 1.0F);
        }

    }

    protected override String getHurtSound()
    {
        return "mob.slime";
    }

    protected override String getDeathSound()
    {
        return "mob.slime";
    }

    protected override int getDropItemId()
    {
        return getSlimeSize() == 1 ? Item.Slimeball.id : 0;
    }

    public override bool canSpawn()
    {
        Chunk chunk = world.ChunkHost.GetChunkFromPos(MathHelper.Floor(x), MathHelper.Floor(z));
        return (getSlimeSize() == 1 || world.Difficulty > 0) && random.NextInt(10) == 0 && chunk.GetSlimeRandom(987234911L).NextInt(10) == 0 && y < 16.0D;
    }

    protected override float getSoundVolume()
    {
        return 0.6F;
    }
}
