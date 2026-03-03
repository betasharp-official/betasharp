using BetaSharp.Blocks.Materials;
using BetaSharp.Entities;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds;

namespace BetaSharp.Blocks;

internal class BlockCake : Block
{

    public BlockCake(int id, int textureId) : base(id, textureId, Material.Cake)
    {
        setTickRandomly(true);
    }

    public override void updateBoundingBox(IBlockAccess iBlockAccess, int x, int y, int z)
    {
        int slicesEaten = iBlockAccess.getBlockMeta(x, y, z);
        float edgeInset = 1.0F / 16.0F;
        float minX = (float)(1 + slicesEaten * 2) / 16.0F;
        float height = 0.5F;
        setBoundingBox(minX, 0.0F, edgeInset, 1.0F - edgeInset, height, 1.0F - edgeInset);
    }

    public override void setupRenderBoundingBox()
    {
        float edgeInset = 1.0F / 16.0F;
        float height = 0.5F;
        setBoundingBox(edgeInset, 0.0F, edgeInset, 1.0F - edgeInset, height, 1.0F - edgeInset);
    }

    public override Box? getCollisionShape(World world, int x, int y, int z)
    {
        int slicesEaten = world.getBlockMeta(x, y, z);
        float edgeInset = 1.0F / 16.0F;
        float minX = (float)(1 + slicesEaten * 2) / 16.0F;
        float height = 0.5F;
        return new Box((double)((float)x + minX), (double)y, (double)((float)z + edgeInset), (double)((float)(x + 1) - edgeInset), (double)((float)y + height - edgeInset), (double)((float)(z + 1) - edgeInset));
    }

    public override Box getBoundingBox(World world, int x, int y, int z)
    {
        int slicesEaten = world.getBlockMeta(x, y, z);
        float edgeInset = 1.0F / 16.0F;
        float minX = (float)(1 + slicesEaten * 2) / 16.0F;
        float height = 0.5F;
        return new Box((double)((float)x + minX), (double)y, (double)((float)z + edgeInset), (double)((float)(x + 1) - edgeInset), (double)((float)y + height), (double)((float)(z + 1) - edgeInset));
    }

    public override int getTexture(int side, int meta)
    {
        return side == 1 ? textureId : (side == 0 ? textureId + 3 : (meta > 0 && side == 4 ? textureId + 2 : textureId + 1));
    }

    public override int getTexture(int side)
    {
        return side == 1 ? textureId : (side == 0 ? textureId + 3 : textureId + 1);
    }

    public override bool isFullCube()
    {
        return false;
    }

    public override bool isOpaque()
    {
        return false;
    }

    public override bool onUse(World world, int x, int y, int z, EntityPlayer player)
    {
        tryEat(world, x, y, z, player);
        return true;
    }

    public override void onBlockBreakStart(World world, int x, int y, int z, EntityPlayer player)
    {
        tryEat(world, x, y, z, player);
    }

    private void tryEat(World world, int x, int y, int z, EntityPlayer player)
    {
        if (player.health < 20)
        {
            player.heal(3);
            int var6 = world.getBlockMeta(x, y, z) + 1;
            if (var6 >= 6)
            {
                world.setBlock(x, y, z, 0);
            }
            else
            {
                world.setBlockMeta(x, y, z, var6);
                world.setBlocksDirty(x, y, z);
            }
        }

    }

    public override bool canPlaceAt(World world, int x, int y, int z)
    {
        return !base.canPlaceAt(world, x, y, z) ? false : canGrow(world, x, y, z);
    }

    public override void neighborUpdate(World world, int x, int y, int z, int id)
    {
        if (!canGrow(world, x, y, z))
        {
            dropStacks(world, x, y, z, world.getBlockMeta(x, y, z));
            world.setBlock(x, y, z, 0);
        }

    }

    public override bool canGrow(World world, int x, int y, int z)
    {
        return world.getMaterial(x, y - 1, z).IsSolid;
    }

    public override int getDroppedItemCount(JavaRandom random)
    {
        return 0;
    }

    public override int getDroppedItemId(int blockMeta, JavaRandom random)
    {
        return 0;
    }
}
