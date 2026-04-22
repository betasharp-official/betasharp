using BetaSharp.Blocks.Materials;
using BetaSharp.Items;

namespace BetaSharp.Blocks;

internal class BlockSnowBlock : Block
{
    public BlockSnowBlock(int id, int textureId) : base(id, textureId, Material.SnowBlock) => setTickRandomly(true);

    public override int getDroppedItemId(int blockMeta) => Item.Snowball.id;

    public override int getDroppedItemCount() => 4;

    public override void onTick(OnTickEvent @event)
    {
        if (@event.World.Lighting.GetBrightness(LightType.Block, @event.X, @event.Y, @event.Z) <= 11) return;

        dropStacks(new OnDropEvent(@event.World, @event.X, @event.Y, @event.Z, @event.World.Reader.GetBlockMeta(@event.X, @event.Y, @event.Z)));
        @event.World.Writer.SetBlock(@event.X, @event.Y, @event.Z, 0);
    }
}
