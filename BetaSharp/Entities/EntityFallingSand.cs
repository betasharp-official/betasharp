using BetaSharp.Blocks;
using BetaSharp.NBT;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Entities;

public class EntityFallingSand : Entity
{
    public override EntityType Type => EntityRegistry.FallingSand;
    public int blockId;
    public int fallTime;

    public EntityFallingSand(IWorldContext world) : base(world)
    {
    }

    public EntityFallingSand(IWorldContext world, double x, double y, double z, int blockId) : base(world)
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
            if (world.Reader.GetBlockId(floorX, floorY, floorZ) == blockId)
            {
                world.Writer.SetBlock(floorX, floorY, floorZ, 0);
            }

            if (onGround)
            {
                velocityX *= (double)0.7F;
                velocityZ *= (double)0.7F;
                velocityY *= -0.5D;
                markDead();
                if ((!Block.Blocks[blockId].canPlaceAt(new CanPlaceAtContext(world, 0, floorX, floorY, floorZ)) || BlockSand.canFallThrough(new OnTickEvent(world, floorX, floorY - 1, floorZ, 0, blockId)) || !world.Writer.SetBlock(floorX, floorY, floorZ, blockId)) && !world.IsRemote)
                {
                    dropItem(blockId, 1);
                }
            }
            else if (fallTime > 100 && !world.IsRemote)
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

    public IWorldContext getWorld()
    {
        return world;
    }
}
