using BetaSharp.Blocks.Materials;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Blocks;

internal class BlockSlab : Block
{
    public static readonly string[] names = ["stone", "sand", "wood", "cobble"];
    private readonly bool doubleSlab;

    public BlockSlab(int id, bool doubleSlab) : base(id, 6, Material.Stone)
    {
        this.doubleSlab = doubleSlab;
        if (!doubleSlab)
        {
            setBoundingBox(0.0F, 0.0F, 0.0F, 1.0F, 0.5F, 1.0F);
        }

        setOpacity(255);
    }

    public override int getTexture(int side, int meta) => meta == 0 ? side <= 1 ? 6 : 5 : meta == 1 ? side == 0 ? 208 : side == 1 ? 176 : 192 : meta == 2 ? 4 : meta == 3 ? 16 : 6;

    public override int getTexture(int side) => getTexture(side, 0);

    public override bool isOpaque() => doubleSlab;

    public override void onPlaced(OnPlacedEvent etv)
    {
        if (this != Slab)
        {
            base.onPlaced(etv);
        }

        int blockBelowId = etv.World.Reader.GetBlockId(etv.X, etv.Y - 1, etv.Z);
        int slabMeta = etv.World.Reader.GetBlockMeta(etv.X, etv.Y, etv.Z);
        int blockBelowMeta = etv.World.Reader.GetBlockMeta(etv.X, etv.Y - 1, etv.Z);
        if (slabMeta == blockBelowMeta)
        {
            if (blockBelowId == Slab.id)
            {
                etv.World.Writer.SetBlock(etv.X, etv.Y, etv.Z, 0);
                etv.World.Writer.SetBlock(etv.X, etv.Y - 1, etv.Z, DoubleSlab.id, slabMeta);
            }
        }
    }

    public override int getDroppedItemId(int blockMeta) => Slab.id;

    public override int getDroppedItemCount() => doubleSlab ? 2 : 1;

    protected override int getDroppedItemMeta(int blockMeta) => blockMeta;

    public override bool isFullCube() => doubleSlab;

    public override bool isSideVisible(IBlockReader iBlockReader, int x, int y, int z, int side)
    {
        if (this != Slab)
        {
            base.isSideVisible(iBlockReader, x, y, z, side);
        }

        return side == 1 ? true : !base.isSideVisible(iBlockReader, x, y, z, side) ? false : side == 0 ? true : iBlockReader.GetBlockId(x, y, z) != id;
    }
}
