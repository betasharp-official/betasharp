using BetaSharp.Blocks;
using BetaSharp.NBT;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds;

namespace BetaSharp.Entities;

public class EntityFallingSand : Entity
{
    public int blockId;
    public int fallTime;

    public EntityFallingSand(World world) : base(world)
    {
    }

    public EntityFallingSand(World world, double x, double y, double z, int blockId) : base(world)
    {
        this.blockId = blockId;
        preventEntitySpawning = true;
        setBoundingBoxSpacing(0.98F, 0.98F);
        standingEyeHeight = height / 2.0F;
        setPosition(x, y, z);
        velocityX = 0.0D;
        velocityY = 0.0D;
        velocityZ = 0.0D;
        prevX = x;
        prevY = y;
        prevZ = z;
    }

    protected override bool bypassesSteppingEffects()
    {
        return false;
    }


    public override bool isCollidable()
    {
        return !dead;
    }

    public override void tick()
    {
        if (blockId == 0)
        {
            markDead();
        }
        else
        {
            prevX = x;
            prevY = y;
            prevZ = z;
            ++fallTime;
            velocityY -= (double)0.04F;
            move(velocityX, velocityY, velocityZ);
            velocityX *= (double)0.98F;
            velocityY *= (double)0.98F;
            velocityZ *= (double)0.98F;
            int floorX = MathHelper.Floor(x);
            int floorY = MathHelper.Floor(y);
            int floorZ = MathHelper.Floor(z);
            if (world.getBlockId(floorX, floorY, floorZ) == blockId)
            {
                world.setBlock(floorX, floorY, floorZ, 0);
            }

            if (onGround)
            {
                velocityX *= (double)0.7F;
                velocityZ *= (double)0.7F;
                velocityY *= -0.5D;
                markDead();
                if ((!world.canPlace(blockId, floorX, floorY, floorZ, true, 1) || BlockSand.canFallThrough(world, floorX, floorY - 1, floorZ) || !world.setBlock(floorX, floorY, floorZ, blockId)) && !world.isRemote)
                {
                    dropItem(blockId, 1);
                }
            }
            else if (fallTime > 100 && !world.isRemote)
            {
                dropItem(blockId, 1);
                markDead();
            }

        }
    }

    public override void writeNbt(NBTTagCompound nbt)
    {
        nbt.SetByte("Tile", (sbyte)blockId);
    }

    public override void readNbt(NBTTagCompound nbt)
    {
        blockId = nbt.GetByte("Tile") & 255;
    }

    public override float getShadowRadius()
    {
        return 0.0F;
    }

    public World getWorld()
    {
        return world;
    }
}
