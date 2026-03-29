using System.ComponentModel;
using BetaSharp.Blocks;
using BetaSharp.Util.Hit;

namespace BetaSharp.Client.Debug.Components;

[DisplayName("Targeted Block")]
[Description("Shows info about the current targeted block.")]
public class DebugTargetedBlock : DebugComponent
{
    public DebugTargetedBlock() { }

    private static readonly string[] s_blockSides = ["Down", "Up", "North", "South", "West", "East"];

    public override IEnumerable<DebugRowData> GetRows(DebugContext ctx)
    {
        BetaSharp g = ctx.Game;

        if (g.objectMouseOver.Type != HitResultType.TILE || g.world == null) yield break;

        int blockX = g.objectMouseOver.BlockX;
        int blockY = g.objectMouseOver.BlockY;
        int blockZ = g.objectMouseOver.BlockZ;
        int blockId = g.world.Reader.GetBlockId(blockX, blockY, blockZ);
        int blockMeta = g.world.Reader.GetBlockMeta(blockX, blockY, blockZ);
        string sideName = GetTargetedSideName(g.objectMouseOver.Side);

        string blockName = "Unknown";
        if (blockId == 0)
        {
            blockName = "Air";
        }
        else if (blockId > 0 && blockId < Block.Blocks.Length && Block.Blocks[blockId] != null)
        {
            Block block = Block.Blocks[blockId];
            string translatedName = block.translateBlockName();
            if (!string.IsNullOrWhiteSpace(translatedName))
            {
                blockName = translatedName;
            }
            else if (!string.IsNullOrWhiteSpace(block.getBlockName()))
            {
                blockName = block.getBlockName();
            }
        }

        yield return new DebugRowData("Targeted block:");
        yield return new DebugRowData($"{blockName} ({blockId}:{blockMeta})");
        yield return new DebugRowData($"XYZ: {blockX} / {blockY} / {blockZ}");
        yield return new DebugRowData($"Face: {sideName}");
    }

    public override DebugComponent Duplicate()
    {
        return new DebugTargetedBlock()
        {
            Right = Right
        };
    }

    private static string GetTargetedSideName(int side)
    {
        return side >= 0 && side < s_blockSides.Length
            ? s_blockSides[side]
            : side.ToString();
    }
}
