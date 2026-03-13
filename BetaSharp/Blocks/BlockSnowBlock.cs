using BetaSharp.Blocks.Materials;
using BetaSharp.Items;

namespace BetaSharp.Blocks;

internal class BlockSnowBlock : Block
{
    public BlockSnowBlock(int id, int textureId) : base(id, textureId, Material.SnowBlock) => setTickRandomly(true);

    public override int getDroppedItemId(int blockMeta) => Item.Snowball.id;

    public override int getDroppedItemCount() => 4;

    public override void onTick(OnTickEvt evt)
    {
        if (evt.Level.Lighting.GetBrightness(LightType.Block, evt.X, evt.Y, evt.Z) > 11)
        {
            dropStacks(new OnDropEvt(evt.Level, evt.X, evt.Y, evt.Z, evt.Level.Reader.GetBlockMeta(evt.X, evt.Y, evt.Z)));
            evt.Level.BlockWriter.SetBlock(evt.X, evt.Y, evt.Z, 0);
        }
    }
}
