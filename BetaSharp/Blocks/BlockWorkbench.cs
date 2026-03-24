using BetaSharp.Blocks.Materials;

namespace BetaSharp.Blocks;

internal class BlockWorkbench : Block
{
    public BlockWorkbench(int id) : base(id, Material.Wood) => textureId = 59;

    public override int getTexture(int side) => side == 1 ? textureId - 16 : side == 0 ? Planks.getTexture(0) : side != 2 && side != 4 ? textureId : textureId + 1;

    public override bool onUse(OnUseEvent ctx)
    {
        if (ctx.World.IsRemote)
        {
            return true;
        }

        ctx.Player.openCraftingScreen(ctx.X, ctx.Y, ctx.Z);
        return true;
    }
}
