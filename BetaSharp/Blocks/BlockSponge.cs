using BetaSharp.Blocks.Materials;

namespace BetaSharp.Blocks;

internal class BlockSponge : Block
{
    public BlockSponge(int id) : base(id, Material.Sponge) => textureId = 48;

    public override void onPlaced(OnPlacedEvt ctx)
    {
        sbyte radius = 2;

        for (int checkX = ctx.X - radius; checkX <= ctx.X + radius; ++checkX)
        {
            for (int checkY = ctx.Y - radius; checkY <= ctx.Y + radius; ++checkY)
            {
                for (int checkZ = ctx.Z - radius; checkZ <= ctx.Z + radius; ++checkZ)
                {
                    if (ctx.WorldRead.GetMaterial(checkX, checkY, checkZ) == Material.Water)
                    {
                    }
                }
            }
        }
    }

    public override void onBreak(OnBreakEvt ctx)
    {
        sbyte radius = 2;

        for (int checkX = ctx.X - radius; checkX <= ctx.X + radius; ++checkX)
        {
            for (int checkY = ctx.Y - radius; checkY <= ctx.Y + radius; ++checkY)
            {
                for (int checkZ = ctx.Z - radius; checkZ <= ctx.Z + radius; ++checkZ)
                {
                    // TODO: Implement this
                    // ctx.World.notifyNeighbors(checkX, checkY, checkZ, ctx.World.getBlockId(checkX, checkY, checkZ));
                }
            }
        }
    }
}
