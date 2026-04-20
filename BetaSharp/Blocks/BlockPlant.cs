using BetaSharp.Blocks.Materials;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Blocks;

public class BlockPlant : Block
{
    private const float HalfSize = 0.2F;

    public BlockPlant(int id, int textureId) : base(id, Material.Plant)
    {
        TextureId = textureId;
        SetTickRandomly(true);
        SetBoundingBox(0.5F - HalfSize, 0.0F, 0.5F - HalfSize, 0.5F + HalfSize, HalfSize * 3.0F, 0.5F + HalfSize);
    }

    public override bool CanPlaceAt(CanPlaceAtContext context) => base.CanPlaceAt(context) && CanPlantOnTop(context.World.Reader.GetBlockId(context.X, context.Y - 1, context.Z));

    protected virtual bool CanPlantOnTop(int id) => id == GrassBlock.ID || id == Dirt.ID || id == Farmland.ID;

    public override void NeighborUpdate(OnTickEvent @event)
    {
        base.NeighborUpdate(@event);
        breakIfCannotGrow(@event.World, @event.X, @event.Y, @event.Z);
    }

    public override void OnTick(OnTickEvent @event) => breakIfCannotGrow(@event.World, @event.X, @event.Y, @event.Z);

    protected void breakIfCannotGrow(IWorldContext level, int x, int y, int z)
    {
        if (CanGrow(new OnTickEvent(level, x, y, z, level.Reader.GetBlockMeta(x, y, z), level.Reader.GetBlockId(x, y, z))))
        {
            return;
        }

        DropStacks(new OnDropEvent(level, x, y, z, level.Reader.GetBlockMeta(x, y, z)));
        level.Writer.SetBlock(x, y, z, 0);
    }

    public override bool CanGrow(OnTickEvent ctx) =>
        (ctx.World.Reader.GetBrightness(ctx.X, ctx.Y, ctx.Z) >= 8 || ctx.World.Lighting.HasSkyLight(ctx.X, ctx.Y, ctx.Z)) && CanPlantOnTop(ctx.World.Reader.GetBlockId(ctx.X, ctx.Y - 1, ctx.Z));

    public override Box? GetCollisionShape(IBlockReader world, EntityManager entities, int x, int y, int z) => null;

    public override bool IsOpaque() => false;

    public override bool IsFullCube() => false;

    public override BlockRendererType GetRenderType() => BlockRendererType.Reed;
}
