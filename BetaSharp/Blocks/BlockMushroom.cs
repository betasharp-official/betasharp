
namespace BetaSharp.Blocks;

internal class BlockMushroom : BlockPlant
{
    public BlockMushroom(int i, int j) : base(i, j)
    {
        float halfSize = 0.2F;
        setBoundingBox(0.5F - halfSize, 0.0F, 0.5F - halfSize, 0.5F + halfSize, halfSize * 2.0F, 0.5F + halfSize);
        setTickRandomly(true);
    }

    public override void onTick(OnTickEvt ctx)
    {
        if (Random.Shared.Next(100) == 0)
        {
            int tryX = ctx.X + Random.Shared.Next(3) - 1;
            int tryY = ctx.Y + Random.Shared.Next(2) - Random.Shared.Next(2);
            int tryZ = ctx.Z + Random.Shared.Next(3) - 1;
            if (ctx.Level.BlocksReader.IsAir(tryX, tryY, tryZ) && canGrow(new OnTickEvt(ctx.Level, tryX, tryY, tryZ, ctx.Level.BlocksReader.GetBlockMeta(tryX, tryY, tryZ), ctx.Level.BlocksReader.GetBlockId(tryX, tryY, tryZ))))
            {
                if (ctx.Level.BlocksReader.IsAir(tryX, tryY, tryZ) && canGrow(new OnTickEvt(ctx.Level, tryX, tryY, tryZ, ctx.Level.BlocksReader.GetBlockMeta(tryX, tryY, tryZ), ctx.Level.BlocksReader.GetBlockId(tryX, tryY, tryZ))))
                {
                    ctx.Level.BlockWriter.SetBlock(tryX, tryY, tryZ, id);
                }
            }
        }
    }

    protected override bool canPlantOnTop(int id) => BlocksOpaque[id];

    public override bool canGrow(OnTickEvt ctx) => ctx.Y >= 0 && ctx.Y < 128 ? ctx.Level.BlocksReader.GetBrightness(ctx.X, ctx.Y, ctx.Z) < 13 && canPlantOnTop(ctx.Level.BlocksReader.GetBlockId(ctx.X, ctx.Y - 1, ctx.Z)) : false;
}
