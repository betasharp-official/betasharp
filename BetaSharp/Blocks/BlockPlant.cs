using BetaSharp.Blocks.Materials;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Blocks;

public class BlockPlant : Block
{
    public BlockPlant(int id, int textureId) : base(id, Material.Plant)
    {
        this.textureId = textureId;
        setTickRandomly(true);
        float halfSize = 0.2F;
        setBoundingBox(0.5F - halfSize, 0.0F, 0.5F - halfSize, 0.5F + halfSize, halfSize * 3.0F, 0.5F + halfSize);
    }

    public override bool canPlaceAt(CanPlaceAtContext context) => base.canPlaceAt(context) && canPlantOnTop(context.World.Reader.GetBlockId(context.X, context.Y - 1, context.Z));

    protected virtual bool canPlantOnTop(int id) => id == GrassBlock.id || id == Dirt.id || id == Farmland.id;

    public override void neighborUpdate(OnTickEvent @event)
    {
        base.neighborUpdate(@event);
        breakIfCannotGrow(@event.World, @event.X, @event.Y, @event.Z);
    }

    public override void onTick(OnTickEvent @event) => breakIfCannotGrow(@event.World, @event.X, @event.Y, @event.Z);

    protected void breakIfCannotGrow(IWorldContext level, int x, int y, int z)
    {
        if (!canGrow(new OnTickEvent(level, x, y, z, level.Reader.GetBlockMeta(x, y, z), level.Reader.GetBlockId(x, y, z))))
        {
            dropStacks(new OnDropEvent(level, x, y, z, level.Reader.GetBlockMeta(x, y, z)));
            level.Writer.SetBlock(x, y, z, 0);
        }
    }

    public override bool canGrow(OnTickEvent ctx) =>
        (ctx.World.Reader.GetBrightness(ctx.X, ctx.Y, ctx.Z) >= 8 || ctx.World.Lighting.HasSkyLight(ctx.X, ctx.Y, ctx.Z)) && canPlantOnTop(ctx.World.Reader.GetBlockId(ctx.X, ctx.Y - 1, ctx.Z));

    public override Box? getCollisionShape(IBlockReader world, EntityManager entities, int x, int y, int z) => null;

    public override bool isOpaque() => false;

    public override bool isFullCube() => false;

    public override BlockRendererType getRenderType() => BlockRendererType.Reed;
}
