using BetaSharp.Blocks.Materials;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Blocks;

internal class BlockSlab : Block
{
    public static readonly string[] Names = ["stone", "sand", "wood", "cobble"];
    private readonly bool _doubleSlab;

    public BlockSlab(int id, bool doubleSlab) : base(id, 6, Material.Stone)
    {
        _doubleSlab = doubleSlab;
        if (!doubleSlab)
        {
            setBoundingBox(0.0F, 0.0F, 0.0F, 1.0F, 0.5F, 1.0F);
        }

        setOpacity(255);
    }

    public override int GetTexture(Side side, int meta) => meta switch
    {
        0 => side <= Side.Up ? 6 : 5,
        1 => side switch
        {
            Side.Down => 208,
            Side.Up => 176,
            _ => 192
        },
        2 => 4,
        3 => 16,
        _ => 6
    };

    public override int GetTexture(Side side) => GetTexture(side, 0);

    public override bool isOpaque() => _doubleSlab;

    public override void onPlaced(OnPlacedEvent etv)
    {
        if (this != Slab) base.onPlaced(etv);

        int blockBelowId = etv.World.Reader.GetBlockId(etv.X, etv.Y - 1, etv.Z);
        int slabMeta = etv.World.Reader.GetBlockMeta(etv.X, etv.Y, etv.Z);
        int blockBelowMeta = etv.World.Reader.GetBlockMeta(etv.X, etv.Y - 1, etv.Z);
        if (slabMeta != blockBelowMeta) return;
        if (blockBelowId != Slab.id) return;
        etv.World.Writer.SetBlock(etv.X, etv.Y, etv.Z, 0);
        etv.World.Writer.SetBlock(etv.X, etv.Y - 1, etv.Z, DoubleSlab.id, slabMeta);
    }

    public override int getDroppedItemId(int blockMeta) => Slab.id;

    public override int getDroppedItemCount() => _doubleSlab ? 2 : 1;

    protected override int getDroppedItemMeta(int blockMeta) => blockMeta;

    public override bool isFullCube() => _doubleSlab;

    public override bool isSideVisible(IBlockReader iBlockReader, int x, int y, int z, Side side)
    {
        if (this != Slab) base.isSideVisible(iBlockReader, x, y, z, side);

        return side == Side.Up || base.isSideVisible(iBlockReader, x, y, z, side) && (side == Side.Down || iBlockReader.GetBlockId(x, y, z) != id);
    }
}
