using BetaSharp.Blocks.Materials;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core;
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

    public override bool canPlaceAt(CanPlaceAtCtx ctx) => base.canPlaceAt(ctx) && canPlantOnTop(ctx.Level.Reader.GetBlockId(ctx.X, ctx.Y - 1, ctx.Z));

    protected virtual bool canPlantOnTop(int id) => id == GrassBlock.id || id == Dirt.id || id == Farmland.id;

    public override void neighborUpdate(OnTickEvt evt)
    {
        base.neighborUpdate(evt);
        breakIfCannotGrow(evt.Level, evt.X, evt.Y, evt.Z);
    }

    public override void onTick(OnTickEvt evt) => breakIfCannotGrow(evt.Level, evt.X, evt.Y, evt.Z);

    protected void breakIfCannotGrow(IWorldContext level, int x, int y, int z)
    {
        if (!canGrow(new OnTickEvt(level, x, y, z, level.Reader.GetBlockMeta(x, y, z), level.Reader.GetBlockId(x, y, z))))
        {
            dropStacks(new OnDropEvt(level, x, y, z, level.Reader.GetBlockMeta(x, y, z)));
            level.BlockWriter.SetBlock(x, y, z, 0);
        }
    }

    public override bool canGrow(OnTickEvt ctx) =>
        (ctx.Level.Reader.GetBrightness(ctx.X, ctx.Y, ctx.Z) >= 8 || ctx.Level.Lighting.HasSkyLight(ctx.X, ctx.Y, ctx.Z)) && canPlantOnTop(ctx.Level.Reader.GetBlockId(ctx.X, ctx.Y - 1, ctx.Z));

    public override Box? getCollisionShape(IBlockReader world, int x, int y, int z) => null;

    public override bool isOpaque() => false;

    public override bool isFullCube() => false;

    public override BlockRendererType getRenderType() => BlockRendererType.Reed;
}
