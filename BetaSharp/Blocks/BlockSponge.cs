using BetaSharp.Blocks.Materials;

namespace BetaSharp.Blocks;

internal class BlockSponge : Block
{
    public BlockSponge(int id) : base(id, Material.Sponge) => textureId = 48;

    public override void onPlaced(OnPlacedEvent @event)
    {
        sbyte radius = 2;

        for (int checkX = @event.X - radius; checkX <= @event.X + radius; ++checkX)
        {
            for (int checkY = @event.Y - radius; checkY <= @event.Y + radius; ++checkY)
            {
                for (int checkZ = @event.Z - radius; checkZ <= @event.Z + radius; ++checkZ)
                {
                    if (@event.World.Reader.GetMaterial(checkX, checkY, checkZ) == Material.Water)
                    {
                    }
                }
            }
        }
    }

    public override void onBreak(OnBreakEvent @event)
    {
        sbyte radius = 2;

        for (int checkX = @event.X - radius; checkX <= @event.X + radius; ++checkX)
        {
            for (int checkY = @event.Y - radius; checkY <= @event.Y + radius; ++checkY)
            {
                for (int checkZ = @event.Z - radius; checkZ <= @event.Z + radius; ++checkZ)
                {
                    @event.World.Broadcaster.NotifyNeighbors(checkX, checkY, checkZ, @event.World.Reader.GetBlockId(checkX, checkY, checkZ));
                }
            }
        }
    }
}
