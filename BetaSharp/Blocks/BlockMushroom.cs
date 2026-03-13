namespace BetaSharp.Blocks;

internal class BlockMushroom : BlockPlant
{
    public BlockMushroom(int i, int j) : base(i, j)
    {
        float halfSize = 0.2F;
        setBoundingBox(0.5F - halfSize, 0.0F, 0.5F - halfSize, 0.5F + halfSize, halfSize * 2.0F, 0.5F + halfSize);
        setTickRandomly(true);
    }

    public override void onTick(OnTickEvt evt)
    {
        if (Random.Shared.Next(100) == 0)
        {
            int tryX = evt.X + Random.Shared.Next(3) - 1;
            int tryY = evt.Y + Random.Shared.Next(2) - Random.Shared.Next(2);
            int tryZ = evt.Z + Random.Shared.Next(3) - 1;
            if (evt.Level.Reader.IsAir(tryX, tryY, tryZ) && canGrow(new OnTickEvt(evt.Level, tryX, tryY, tryZ, evt.Level.Reader.GetBlockMeta(tryX, tryY, tryZ), evt.Level.Reader.GetBlockId(tryX, tryY, tryZ))))
            {
                if (evt.Level.Reader.IsAir(tryX, tryY, tryZ) && canGrow(new OnTickEvt(evt.Level, tryX, tryY, tryZ, evt.Level.Reader.GetBlockMeta(tryX, tryY, tryZ), evt.Level.Reader.GetBlockId(tryX, tryY, tryZ))))
                {
                    evt.Level.BlockWriter.SetBlock(tryX, tryY, tryZ, id);
                }
            }
        }
    }

    protected override bool canPlantOnTop(int id) => BlocksOpaque[id];

    public override bool canGrow(OnTickEvt ctx) => ctx.Y >= 0 && ctx.Y < 128 ? ctx.Level.Reader.GetBrightness(ctx.X, ctx.Y, ctx.Z) < 13 && canPlantOnTop(ctx.Level.Reader.GetBlockId(ctx.X, ctx.Y - 1, ctx.Z)) : false;
}
