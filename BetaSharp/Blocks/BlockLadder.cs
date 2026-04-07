using BetaSharp.Blocks.Materials;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Blocks;

internal class BlockLadder(int id, int textureId) : Block(id, textureId, Material.PistonBreakable)
{
    private const float thickness = 2.0F / 16.0F;

    public override Box? getCollisionShape(IBlockReader world, EntityManager entities, int x, int y, int z)
    {
        Side rotation = world.GetBlockMeta(x, y, z).ToSide();
        switch (rotation)
        {
            case Side.North:
                setBoundingBox(0.0F, 0.0F, 1.0F - thickness, 1.0F, 1.0F, 1.0F);
                break;
            case Side.South:
                setBoundingBox(0.0F, 0.0F, 0.0F, 1.0F, 1.0F, thickness);
                break;
            case Side.West:
                setBoundingBox(1.0F - thickness, 0.0F, 0.0F, 1.0F, 1.0F, 1.0F);
                break;
            case Side.East:
                setBoundingBox(0.0F, 0.0F, 0.0F, thickness, 1.0F, 1.0F);
                break;
        }

        return base.getCollisionShape(world, entities, x, y, z);
    }

    public override Box getBoundingBox(IBlockReader world, EntityManager entities, int x, int y, int z)
    {
        Side rotation = world.GetBlockMeta(x, y, z).ToSide();
        switch (rotation)
        {
            case Side.North:
                setBoundingBox(0.0F, 0.0F, 1.0F - thickness, 1.0F, 1.0F, 1.0F);
                break;
            case Side.South:
                setBoundingBox(0.0F, 0.0F, 0.0F, 1.0F, 1.0F, thickness);
                break;
            case Side.West:
                setBoundingBox(1.0F - thickness, 0.0F, 0.0F, 1.0F, 1.0F, 1.0F);
                break;
            case Side.East:
                setBoundingBox(0.0F, 0.0F, 0.0F, thickness, 1.0F, 1.0F);
                break;
        }

        return base.getBoundingBox(world, entities, x, y, z);
    }

    public override bool isOpaque() => false;

    public override bool isFullCube() => false;

    public override BlockRendererType getRenderType() => BlockRendererType.Ladder;

    public override bool canPlaceAt(CanPlaceAtContext context) =>
        context.World.Reader.ShouldSuffocate(context.X - 1, context.Y, context.Z) ? true :
        context.World.Reader.ShouldSuffocate(context.X + 1, context.Y, context.Z) ? true :
        context.World.Reader.ShouldSuffocate(context.X, context.Y, context.Z - 1) ? true : context.World.Reader.ShouldSuffocate(context.X, context.Y, context.Z + 1);

    public override void onPlaced(OnPlacedEvent ctx)
    {
        Side rotation = ctx.World.Reader.GetBlockMeta(ctx.X, ctx.Y, ctx.Z).ToSide();
        if ((rotation == 0 || ctx.Direction == Side.North) && ctx.World.Reader.ShouldSuffocate(ctx.X, ctx.Y, ctx.Z + 1)) rotation = Side.North;
        if ((rotation == 0 || ctx.Direction == Side.South) && ctx.World.Reader.ShouldSuffocate(ctx.X, ctx.Y, ctx.Z - 1)) rotation = Side.South;
        if ((rotation == 0 || ctx.Direction == Side.West) && ctx.World.Reader.ShouldSuffocate(ctx.X + 1, ctx.Y, ctx.Z)) rotation = Side.West;
        if ((rotation == 0 || ctx.Direction == Side.East) && ctx.World.Reader.ShouldSuffocate(ctx.X - 1, ctx.Y, ctx.Z)) rotation = Side.East;
        ctx.World.Writer.SetBlockMeta(ctx.X, ctx.Y, ctx.Z, rotation.ToInt());
    }

    public override void neighborUpdate(OnTickEvent ctx)
    {
        Side rotation = ctx.World.Reader.GetBlockMeta(ctx.X, ctx.Y, ctx.Z).ToSide();
        bool hasSupport = rotation == Side.North && ctx.World.Reader.ShouldSuffocate(ctx.X, ctx.Y, ctx.Z + 1) ||
                          rotation == Side.South && ctx.World.Reader.ShouldSuffocate(ctx.X, ctx.Y, ctx.Z - 1) ||
                          rotation == Side.West && ctx.World.Reader.ShouldSuffocate(ctx.X + 1, ctx.Y, ctx.Z) ||
                          rotation == Side.East && ctx.World.Reader.ShouldSuffocate(ctx.X - 1, ctx.Y, ctx.Z);

        if (!hasSupport)
        {
            dropStacks(new OnDropEvent(ctx.World, ctx.X, ctx.Y, ctx.Z, rotation.ToInt()));
            ctx.World.Writer.SetBlock(ctx.X, ctx.Y, ctx.Z, 0);
        }

        base.neighborUpdate(ctx);
    }

    public override int getDroppedItemCount() => 1;
}
