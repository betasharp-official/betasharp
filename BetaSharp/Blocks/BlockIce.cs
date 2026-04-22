using BetaSharp.Blocks.Materials;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Blocks;

internal class BlockIce : BlockBreakable
{
    public BlockIce(int id, int textureId) : base(id, textureId, Material.Ice, false)
    {
        Slipperiness = 0.98F;
        setTickRandomly(true);
    }

    public override int getRenderLayer() => 1;

    public override bool isSideVisible(IBlockReader iBlockReader, int x, int y, int z, Side side) => base.isSideVisible(iBlockReader, x, y, z, 1 - side);

    public override void onAfterBreak(OnAfterBreakEvent @event)
    {
        base.onAfterBreak(@event);
        Material materialBelow = @event.World.Reader.GetMaterial(@event.X, @event.Y - 1, @event.Z);
        if (materialBelow.BlocksMovement || materialBelow.IsFluid)
        {
            @event.World.Writer.SetBlock(@event.X, @event.Y, @event.Z, FlowingWater.id);
        }
    }

    public override int getDroppedItemCount() => 0;

    public override void onTick(OnTickEvent @event)
    {
        if (@event.World.Lighting.GetBrightness(LightType.Block, @event.X, @event.Y, @event.Z) <= 11 - BlockLightOpacity[id])
        {
            return;
        }

        dropStacks(new OnDropEvent(@event.World, @event.X, @event.Y, @event.Z, @event.World.Reader.GetBlockMeta(@event.X, @event.Y, @event.Z)));
        @event.World.Writer.SetBlock(@event.X, @event.Y, @event.Z, Water.id);
    }

    public override int getPistonBehavior() => 0;
}
