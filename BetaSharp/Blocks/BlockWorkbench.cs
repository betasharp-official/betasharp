using BetaSharp.Blocks.Materials;

namespace BetaSharp.Blocks;

internal class BlockWorkbench : Block
{
    public BlockWorkbench(int id) : base(id, Material.Wood) => TextureId = 59;

    public override int GetTexture(Side side)
    {
        return side switch
        {
            Side.Up => TextureId - 16,
            Side.Down => Planks.GetTexture(0),
            _ => side != Side.North && side != Side.West ? TextureId : TextureId + 1
        };
    }

    public override bool onUse(OnUseEvent ctx)
    {
        if (ctx.World.IsRemote) return true;
        ctx.Player.openCraftingScreen(ctx.X, ctx.Y, ctx.Z);
        return true;
    }
}
