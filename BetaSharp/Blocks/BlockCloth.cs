using BetaSharp.Blocks.Materials;

namespace BetaSharp.Blocks;

internal class BlockCloth() : Block(35, 64, Material.Wool)
{
    public override int GetTexture(Side side, int meta)
    {
        if (meta == 0)
        {
            return TextureId;
        }

        meta = ~(meta & 15);
        return 113 + ((meta & 8) >> 3) + (meta & 7) * 16;
    }

    protected override int GetDroppedItemMeta(int blockMeta) => blockMeta;

    public static int getBlockMeta(int itemMeta) => ~itemMeta & 15;

    public static int getItemMeta(int blockMeta) => ~blockMeta & 15;
}
