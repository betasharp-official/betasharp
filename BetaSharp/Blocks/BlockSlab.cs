using BetaSharp.Blocks.Materials;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds;

namespace BetaSharp.Blocks;

internal class BlockSlab : Block
{
    public static readonly string[] names = ["stone", "sand", "wood", "cobble"];
    private bool doubleSlab;

    public BlockSlab(int id, bool doubleSlab) : base(id, 6, Material.Stone)
    {
        this.doubleSlab = doubleSlab;
        if (!doubleSlab)
        {
            setBoundingBox(0.0F, 0.0F, 0.0F, 1.0F, 0.5F, 1.0F);
        }

        setOpacity(255);
    }

    public override int getTexture(int side, int meta)
    {
        return meta == 0 ? (side <= 1 ? 6 : 5) : (meta == 1 ? (side == 0 ? 208 : (side == 1 ? 176 : 192)) : (meta == 2 ? 4 : (meta == 3 ? 16 : 6)));
    }

    public override int getTexture(int side)
    {
        return getTexture(side, 0);
    }

    public override bool isOpaque()
    {
        return doubleSlab;
    }

    public override void onPlaced(World world, int x, int y, int z)
    {
        if (this != Block.Slab)
        {
            base.onPlaced(world, x, y, z);
        }

        int blockBelowId = world.getBlockId(x, y - 1, z);
        int slabMeta = world.getBlockMeta(x, y, z);
        int blockBelowMeta = world.getBlockMeta(x, y - 1, z);
        if (slabMeta == blockBelowMeta)
        {
            if (blockBelowId == Slab.id)
            {
                world.setBlock(x, y, z, 0);
                world.setBlock(x, y - 1, z, Block.DoubleSlab.id, slabMeta);
            }

        }
    }

    public override int getDroppedItemId(int blockMeta, JavaRandom random)
    {
        return Block.Slab.id;
    }

    public override int getDroppedItemCount(JavaRandom random)
    {
        return doubleSlab ? 2 : 1;
    }

    protected override int getDroppedItemMeta(int blockMeta)
    {
        return blockMeta;
    }

    public override bool isFullCube()
    {
        return doubleSlab;
    }

    public override bool isSideVisible(IBlockAccess iBlockAccess, int x, int y, int z, int side)
    {
        if (this != Block.Slab)
        {
            base.isSideVisible(iBlockAccess, x, y, z, side);
        }

        return side == 1 ? true : (!base.isSideVisible(iBlockAccess, x, y, z, side) ? false : (side == 0 ? true : iBlockAccess.getBlockId(x, y, z) != id));
    }
}
