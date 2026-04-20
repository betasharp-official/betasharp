using BetaSharp.Blocks.Materials;

namespace BetaSharp.Blocks;

internal class BlockWorkbench : Block
{
    public BlockWorkbench(int id) : base(id, Material.Wood) => TextureId = BlockTextures.CraftingTableSide;

    public override int GetTexture(Side side) =>
        side switch
        {
            Side.Up => BlockTextures.CraftingTableTop,
            Side.Down => BlockTextures.OakPlanks,
            _ => side != Side.North && side != Side.West ? BlockTextures.CraftingTableSide : BlockTextures.CraftingTableFront
        };

    public override bool OnUse(OnUseEvent ctx)
    {
        if (ctx.World.IsRemote)
        {
            return true;
        }

        ctx.Player.openCraftingScreen(ctx.X, ctx.Y, ctx.Z);
        return true;
    }
}
