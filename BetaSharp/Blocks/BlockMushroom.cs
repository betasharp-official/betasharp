using BetaSharp.Worlds.Chunks;

namespace BetaSharp.Blocks;

internal class BlockMushroom : BlockPlant
{
    private const float HalfSize = 0.2F;

    public BlockMushroom(int i, int j) : base(i, j)
    {
        setBoundingBox(0.5F - HalfSize, 0.0F, 0.5F - HalfSize, 0.5F + HalfSize, HalfSize * 2.0F, 0.5F + HalfSize);
        setTickRandomly(true);
    }

    public override void onTick(OnTickEvent @event)
    {
        if (Random.Shared.Next(100) != 0) return;

        int tryX = @event.X + Random.Shared.Next(3) - 1;
        int tryY = @event.Y + Random.Shared.Next(2) - Random.Shared.Next(2);
        int tryZ = @event.Z + Random.Shared.Next(3) - 1;
        if (!@event.World.Reader.IsAir(tryX, tryY, tryZ) || !canGrow(new OnTickEvent(@event.World, tryX, tryY, tryZ, @event.World.Reader.GetBlockMeta(tryX, tryY, tryZ), @event.World.Reader.GetBlockId(tryX, tryY, tryZ))))
        {
            return;
        }

        if (@event.World.Reader.IsAir(tryX, tryY, tryZ) && canGrow(new OnTickEvent(@event.World, tryX, tryY, tryZ, @event.World.Reader.GetBlockMeta(tryX, tryY, tryZ), @event.World.Reader.GetBlockId(tryX, tryY, tryZ))))
        {
            @event.World.Writer.SetBlock(tryX, tryY, tryZ, id);
        }
    }

    protected override bool canPlantOnTop(int id) => id == GrassBlock.id || id == Dirt.id || id == Stone.id || id == Gravel.id || id == Cobblestone.id;

    public override bool canGrow(OnTickEvent ctx) => ctx.Y >= 0 && ctx.Y < ChuckFormat.WorldHeight && (ctx.World.Reader.GetBrightness(ctx.X, ctx.Y, ctx.Z) < 13 && canPlantOnTop(ctx.World.Reader.GetBlockId(ctx.X, ctx.Y - 1, ctx.Z)));
}
