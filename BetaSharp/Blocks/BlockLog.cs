using BetaSharp.Blocks.Materials;

namespace BetaSharp.Blocks;

internal class BlockLog : Block
{
    public BlockLog(int id) : base(id, Material.Wood) => textureId = 20;

    public override int getDroppedItemCount() => 1;

    public override int getDroppedItemId(int blockMeta) => Log.id;

    public override void onAfterBreak(OnAfterBreakEvent @event) => base.onAfterBreak(@event);

    public override void onBreak(OnBreakEvent @event)
    {
        sbyte searchRadius = 4;
        int regionExtent = searchRadius + 1;
        if (@event.World.ChunkHost.IsRegionLoaded(@event.X - regionExtent, @event.Y - regionExtent, @event.Z - regionExtent, @event.X + regionExtent, @event.Y + regionExtent, @event.Z + regionExtent))
        {
            for (int offsetX = -searchRadius; offsetX <= searchRadius; ++offsetX)
            {
                for (int offsetY = -searchRadius; offsetY <= searchRadius; ++offsetY)
                {
                    for (int offsetZ = -searchRadius; offsetZ <= searchRadius; ++offsetZ)
                    {
                        int neighborBlockId = @event.World.Reader.GetBlockId(@event.X + offsetX, @event.Y + offsetY, @event.Z + offsetZ);
                        if (neighborBlockId == Leaves.id)
                        {
                            int leavesMeta = @event.World.Reader.GetBlockMeta(@event.X + offsetX, @event.Y + offsetY, @event.Z + offsetZ);
                            if ((leavesMeta & 8) == 0)
                            {
                                @event.World.Writer.SetBlockMetaWithoutNotifyingNeighbors(@event.X + offsetX, @event.Y + offsetY, @event.Z + offsetZ, leavesMeta | 8);
                            }
                        }
                    }
                }
            }
        }
    }

    public override int getTexture(int side, int meta) => side == 1 ? 21 : side == 0 ? 21 : meta == 1 ? 116 : meta == 2 ? 117 : 20;

    protected override int getDroppedItemMeta(int blockMeta) => blockMeta;
}
