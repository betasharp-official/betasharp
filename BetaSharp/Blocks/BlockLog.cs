using BetaSharp.Blocks.Materials;

namespace BetaSharp.Blocks;

internal class BlockLog : Block
{
    public BlockLog(int id) : base(id, Material.Wood) => TextureId = 20;

    public override int GetDroppedItemCount() => 1;

    public override int GetDroppedItemId(int blockMeta) => Log.Id;

    public override void OnAfterBreak(OnAfterBreakEvent @event) => base.OnAfterBreak(@event);

    public override void OnBreak(OnBreakEvent @event)
    {
        const sbyte searchRadius = 4;
        const int regionExtent = searchRadius + 1;
        if (!@event.World.ChunkHost.IsRegionLoaded(@event.X - regionExtent, @event.Y - regionExtent, @event.Z - regionExtent, @event.X + regionExtent, @event.Y + regionExtent, @event.Z + regionExtent))
        {
            return;
        }

        for (int offsetX = -searchRadius; offsetX <= searchRadius; ++offsetX)
        {
            for (int offsetY = -searchRadius; offsetY <= searchRadius; ++offsetY)
            {
                for (int offsetZ = -searchRadius; offsetZ <= searchRadius; ++offsetZ)
                {
                    int neighborBlockId = @event.World.Reader.GetBlockId(@event.X + offsetX, @event.Y + offsetY, @event.Z + offsetZ);
                    if (neighborBlockId != Leaves.Id)
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

    public override int GetTexture(Side side, int meta) => side == Side.Up ? 21 : side == Side.Down ? 21 : meta == 1 ? 116 : meta == 2 ? 117 : 20;

    protected override int GetDroppedItemMeta(int blockMeta) => blockMeta;
}
