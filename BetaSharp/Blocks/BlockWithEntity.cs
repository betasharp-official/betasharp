using BetaSharp.Blocks.Entities;
using BetaSharp.Blocks.Materials;

namespace BetaSharp.Blocks;

public abstract class BlockWithEntity : Block
{
    protected BlockWithEntity(int id, Material material) : base(id, material) => BlocksWithEntity[id] = true;

    protected BlockWithEntity(int id, int textureId, Material material) : base(id, textureId, material) => BlocksWithEntity[id] = true;

    public override void onPlaced(OnPlacedEvt ctx)
    {
        base.onPlaced(ctx);
        ctx.WorldWrite.SetBlockEntity(ctx.X, ctx.Y, ctx.Z, getBlockEntity());
    }

    public override void onBreak(OnBreakEvt ctx)
    {
        base.onBreak(ctx);
        ctx.WorldWrite.RemoveBlockEntity(ctx.X, ctx.Y, ctx.Z);
    }

    protected abstract BlockEntity getBlockEntity();
}
