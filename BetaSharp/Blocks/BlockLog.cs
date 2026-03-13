using BetaSharp.Blocks.Materials;

namespace BetaSharp.Blocks;

internal class BlockLog : Block
{
    public BlockLog(int id) : base(id, Material.Wood) => textureId = 20;

    public override int getDroppedItemCount() => 1;

    public override int getDroppedItemId(int blockMeta) => Log.id;

    public override void onAfterBreak(OnAfterBreakEvt evt) => base.onAfterBreak(evt);

    public override void onBreak(OnBreakEvt evt)
    {
        sbyte searchRadius = 4;
        int regionExtent = searchRadius + 1;
        if (evt.Level.BlockHost.IsRegionLoaded(evt.X - regionExtent, evt.Y - regionExtent, evt.Z - regionExtent, evt.X + regionExtent, evt.Y + regionExtent, evt.Z + regionExtent))
        {
            for (int offsetX = -searchRadius; offsetX <= searchRadius; ++offsetX)
            {
                for (int offsetY = -searchRadius; offsetY <= searchRadius; ++offsetY)
                {
                    for (int offsetZ = -searchRadius; offsetZ <= searchRadius; ++offsetZ)
                    {
                        int neighborBlockId = evt.Level.Reader.GetBlockId(evt.X + offsetX, evt.Y + offsetY, evt.Z + offsetZ);
                        if (neighborBlockId == Leaves.id)
                        {
                            int leavesMeta = evt.Level.Reader.GetBlockMeta(evt.X + offsetX, evt.Y + offsetY, evt.Z + offsetZ);
                            if ((leavesMeta & 8) == 0)
                            {
                                evt.Level.BlockWriter.SetBlockMetaWithoutNotifyingNeighbors(evt.X + offsetX, evt.Y + offsetY, evt.Z + offsetZ, leavesMeta | 8);
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
