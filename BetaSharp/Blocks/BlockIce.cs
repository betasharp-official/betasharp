using BetaSharp.Blocks.Materials;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Blocks;

internal class BlockIce : BlockBreakable
{
    public BlockIce(int id, int textureId) : base(id, textureId, Material.Ice, false)
    {
        Slipperiness = 0.98F;
        SetTickRandomly(true);
    }

    public override int GetRenderLayer() => 1;

    public override bool IsSideVisible(IBlockReader iBlockReader, int x, int y, int z, Side side) => base.IsSideVisible(iBlockReader, x, y, z, 1 - side);

    public override void OnAfterBreak(OnAfterBreakEvent @event)
    {
        base.OnAfterBreak(@event);
        Material materialBelow = @event.World.Reader.GetMaterial(@event.X, @event.Y - 1, @event.Z);
        if (materialBelow.BlocksMovement || materialBelow.IsFluid)
        {
            @event.World.Writer.SetBlock(@event.X, @event.Y, @event.Z, FlowingWater.ID);
        }
    }

    public override int GetDroppedItemCount() => 0;

    public override void OnTick(OnTickEvent @event)
    {
        if (@event.World.Lighting.GetBrightness(LightType.Block, @event.X, @event.Y, @event.Z) <= 11 - BlockLightOpacity[ID])
        {
            return;
        }

        DropStacks(new OnDropEvent(@event.World, @event.X, @event.Y, @event.Z, @event.World.Reader.GetBlockMeta(@event.X, @event.Y, @event.Z)));
        @event.World.Writer.SetBlock(@event.X, @event.Y, @event.Z, Water.ID);
    }

    public override PistonBehavior GetPistonBehavior() => PistonBehavior.Normal;
}
