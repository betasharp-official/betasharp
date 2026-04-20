using BetaSharp.Blocks.Materials;

namespace BetaSharp.Blocks;

internal class BlockSponge : Block
{
    private const sbyte AbsorbRadius = 2;
    public BlockSponge(int id) : base(id, Material.Sponge) => TextureId = BlockTextures.Sponge;

    public override void OnPlaced(OnPlacedEvent @event)
    {
        for (int checkX = @event.X - AbsorbRadius; checkX <= @event.X + AbsorbRadius; ++checkX)
        {
            for (int checkY = @event.Y - AbsorbRadius; checkY <= @event.Y + AbsorbRadius; ++checkY)
            {
                for (int checkZ = @event.Z - AbsorbRadius; checkZ <= @event.Z + AbsorbRadius; ++checkZ)
                {
                    if (@event.World.Reader.GetMaterial(checkX, checkY, checkZ) == Material.Water)
                    {
                    }
                }
            }
        }
    }

    public override void OnBreak(OnBreakEvent @event)
    {
        for (int checkX = @event.X - AbsorbRadius; checkX <= @event.X + AbsorbRadius; ++checkX)
        {
            for (int checkY = @event.Y - AbsorbRadius; checkY <= @event.Y + AbsorbRadius; ++checkY)
            {
                for (int checkZ = @event.Z - AbsorbRadius; checkZ <= @event.Z + AbsorbRadius; ++checkZ)
                {
                    @event.World.Broadcaster.NotifyNeighbors(checkX, checkY, checkZ, @event.World.Reader.GetBlockId(checkX, checkY, checkZ));
                }
            }
        }
    }
}
