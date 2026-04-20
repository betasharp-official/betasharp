using BetaSharp.Blocks.Materials;

namespace BetaSharp.Blocks;

internal class BlockLog : Block
{
    private const sbyte SearchRadius = 4;
    private const int RegionExtent = SearchRadius + 1;
    public BlockLog(int id) : base(id, Material.Wood) => TextureId = BlockTextures.LogOakSide;

    public override int GetDroppedItemCount() => 1;

    public override int GetDroppedItemId(int blockMeta) => Log.ID;

    public override void OnAfterBreak(OnAfterBreakEvent @event) => base.OnAfterBreak(@event);

    public override void OnBreak(OnBreakEvent @event)
    {
        if (!@event.World.ChunkHost.IsRegionLoaded(@event.X - RegionExtent, @event.Y - RegionExtent, @event.Z - RegionExtent, @event.X + RegionExtent, @event.Y + RegionExtent, @event.Z + RegionExtent))
        {
            return;
        }

        for (int offsetX = -SearchRadius; offsetX <= SearchRadius; ++offsetX)
        {
            for (int offsetY = -SearchRadius; offsetY <= SearchRadius; ++offsetY)
            {
                for (int offsetZ = -SearchRadius; offsetZ <= SearchRadius; ++offsetZ)
                {
                    int neighborBlockId = @event.World.Reader.GetBlockId(@event.X + offsetX, @event.Y + offsetY, @event.Z + offsetZ);
                    if (neighborBlockId != Leaves.ID)
                    {
                        continue;
                    }

                    int leavesMeta = @event.World.Reader.GetBlockMeta(@event.X + offsetX, @event.Y + offsetY, @event.Z + offsetZ);
                    if ((leavesMeta & 8) == 0)
                    {
                        @event.World.Writer.SetBlockMetaWithoutNotifyingNeighbors(@event.X + offsetX, @event.Y + offsetY, @event.Z + offsetZ, leavesMeta | 8);
                    }
                }
            }
        }
    }

    public override int GetTexture(Side side, int meta) => side switch
    {
        Side.Up or Side.Down => BlockTextures.LogTop,
        _ => meta == 1 ? BlockTextures.LogPineSide : meta == 2 ? BlockTextures.LogBirchSide : BlockTextures.LogOakSide
    };


    protected override int GetDroppedItemMeta(int blockMeta) => blockMeta;
}
