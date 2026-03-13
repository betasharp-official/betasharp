using BetaSharp.Blocks.Materials;
using BetaSharp.Worlds.Core;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Blocks;

internal class BlockIce : BlockBreakable
{
    public BlockIce(int id, int textureId) : base(id, textureId, Material.Ice, false)
    {
        slipperiness = 0.98F;
        setTickRandomly(true);
    }

    public override int getRenderLayer() => 1;

    public override bool isSideVisible(IBlockReader iBlockReader, int x, int y, int z, int side) => base.isSideVisible(iBlockReader, x, y, z, 1 - side);

    public override void onAfterBreak(OnAfterBreakEvt evt)
    {
        base.onAfterBreak(evt);
        Material materialBelow = evt.Level.Reader.GetMaterial(evt.X, evt.Y - 1, evt.Z);
        if (materialBelow.BlocksMovement || materialBelow.IsFluid)
        {
            evt.Level.BlockWriter.SetBlock(evt.X, evt.Y, evt.Z, FlowingWater.id);
        }
    }

    public override int getDroppedItemCount() => 0;

    public override void onTick(OnTickEvt evt)
    {
        if (evt.Level.Lighting.GetBrightness(LightType.Block, evt.X, evt.Y, evt.Z) > 11 - BlockLightOpacity[id])
        {
            dropStacks(new OnDropEvt(evt.Level, evt.X, evt.Y, evt.Z, evt.Level.Reader.GetBlockMeta(evt.X, evt.Y, evt.Z)));
            evt.Level.BlockWriter.SetBlock(evt.X, evt.Y, evt.Z, Water.id);
        }
    }

    public override int getPistonBehavior() => 0;
}
