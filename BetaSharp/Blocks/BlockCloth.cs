using BetaSharp.Blocks.Materials;

namespace BetaSharp.Blocks;

internal class BlockCloth() : Block(35, 64, Material.Wool)
{
    public override int GetTexture(Side side, int meta)
    {
        if (meta == 0)
        {
            return 64;
        }

        meta = ~(meta & 15);
        return BlockTextures.WoolColoredPaletteBase + ((meta & 8) >> 3) + (meta & 7) * 16;
    }

    protected override int getDroppedItemMeta(int blockMeta) => blockMeta;

    public static int getBlockMeta(int itemMeta) => ~itemMeta & 15;

    public static int getItemMeta(int blockMeta) => ~blockMeta & 15;

    public override IReadOnlyList<string> GetBlockAlias => [
        "blackWool:15",
        "redWool:14",
        "greenWool:13",
        "brownWool:12",
        "blueWool:11",
        "purpleWool:10",
        "cyanWool:9",
        "silverWool:8",
        "grayWool:7",
        "pinkWool:6",
        "limeWool:5",
        "yellowWool:4",
        "lightBlueWool:3",
        "magentaWool:2",
        "orangeWool:1",
        "whiteWool:0"
    ];
}
