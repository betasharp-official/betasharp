using BetaSharp.Blocks.Materials;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Blocks;

internal class BlockSlab : Block
{
    public static readonly string[] Names = ["stone", "sand", "wood", "cobble"];
    private readonly bool _doubleSlab;

    public BlockSlab(int id, bool doubleSlab) : base(id, BlockTextures.StoneSlabTop, Material.Stone)
    {
        _doubleSlab = doubleSlab;
        if (!doubleSlab)
        {
            SetBoundingBox(0.0F, 0.0F, 0.0F, 1.0F, 0.5F, 1.0F);
        }

        SetOpacity(255);
    }

    public override int GetTexture(Side side, int meta) => meta switch
    {
        0 => side <= Side.Up ? BlockTextures.StoneSlabTop : BlockTextures.StoneSlabSide,
        1 => side switch
        {
            Side.Down => BlockTextures.SandstoneBottom,
            Side.Up => BlockTextures.SandstoneTop,
            _ => BlockTextures.SandstoneSide
        },
        2 => BlockTextures.OakPlanks,
        3 => BlockTextures.Cobblestone,
        _ => BlockTextures.StoneSlabSide
    };

    public override int GetTexture(Side side) => GetTexture(side, 0);

    public override bool IsOpaque() => _doubleSlab;

    public override void OnPlaced(OnPlacedEvent etv)
    {
        if (this != Slab)
        {
            base.OnPlaced(etv);
        }

        int blockBelowId = etv.World.Reader.GetBlockId(etv.X, etv.Y - 1, etv.Z);
        int slabMeta = etv.World.Reader.GetBlockMeta(etv.X, etv.Y, etv.Z);
        int blockBelowMeta = etv.World.Reader.GetBlockMeta(etv.X, etv.Y - 1, etv.Z);
        if (slabMeta != blockBelowMeta)
        {
            return;
        }

        if (blockBelowId != Slab.ID)
        {
            return;
        }

        etv.World.Writer.SetBlock(etv.X, etv.Y, etv.Z, 0);
        etv.World.Writer.SetBlock(etv.X, etv.Y - 1, etv.Z, DoubleSlab.ID, slabMeta);
    }

    public override int GetDroppedItemId(int blockMeta) => Slab.ID;

    public override int GetDroppedItemCount() => _doubleSlab ? 2 : 1;

    protected override int GetDroppedItemMeta(int blockMeta) => blockMeta;

    public override bool IsFullCube() => _doubleSlab;

    public override bool IsSideVisible(IBlockReader iBlockReader, int x, int y, int z, Side side)
    {
        if (this != Slab)
        {
            base.IsSideVisible(iBlockReader, x, y, z, side);
        }

        return side == Side.Up || (base.IsSideVisible(iBlockReader, x, y, z, side) && (side == Side.Down || iBlockReader.GetBlockId(x, y, z) != ID));
    }
}
