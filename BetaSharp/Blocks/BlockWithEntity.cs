using BetaSharp.Blocks.Entities;
using BetaSharp.Blocks.Materials;

namespace BetaSharp.Blocks;

/// <summary>
///     Abstract helper class for blocks that have an associated block entity. This class
///     handles the creation and removal of the block entity when the block is placed and
///     broken, and requires derived classes to implement the GetBlockEntity method to
///     provide the specific block entity instance.
/// </summary>
public abstract class BlockWithEntity : Block
{
    protected BlockWithEntity(int id, Material material) : base(id, material) => BlocksWithEntity[id] = true;

    protected BlockWithEntity(int id, int textureId, Material material) : base(id, textureId, material) => BlocksWithEntity[id] = true;

    public override void OnPlaced(OnPlacedEvent ctx)
    {
        base.OnPlaced(ctx);
        ctx.World.Entities.SetBlockEntity(ctx.X, ctx.Y, ctx.Z, GetBlockEntity());
    }

    public override void OnBreak(OnBreakEvent ctx)
    {
        base.OnBreak(ctx);
        ctx.World.Entities.RemoveBlockEntity(ctx.X, ctx.Y, ctx.Z);
    }

    public abstract BlockEntity GetBlockEntity();
}
