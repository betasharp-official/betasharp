using BetaSharp.Blocks.Materials;
using BetaSharp.Items;

namespace BetaSharp.Blocks;

internal class BlockSnowBlock : Block
{
    public BlockSnowBlock(int id, int textureId) : base(id, textureId, Material.SnowBlock) => SetTickRandomly(true);

    public override int GetDroppedItemId(int blockMeta) => Item.Snowball.id;

    public override int GetDroppedItemCount() => 4;

    public override void OnTick(OnTickEvent @event)
    {
        if (@event.World.Lighting.GetBrightness(LightType.Block, @event.X, @event.Y, @event.Z) <= 11)
        {
            return;
        }

        DropStacks(new OnDropEvent(@event.World, @event.X, @event.Y, @event.Z, @event.World.Reader.GetBlockMeta(@event.X, @event.Y, @event.Z)));
        @event.World.Writer.SetBlock(@event.X, @event.Y, @event.Z, 0);
    }
}
