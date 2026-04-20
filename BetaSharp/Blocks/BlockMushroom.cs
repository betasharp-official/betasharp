using BetaSharp.Worlds.Chunks;

namespace BetaSharp.Blocks;

internal class BlockMushroom : BlockPlant
{
    private const float HalfSize = 0.2F;

    public BlockMushroom(int i, int j) : base(i, j)
    {
        SetBoundingBox(0.5F - HalfSize, 0.0F, 0.5F - HalfSize, 0.5F + HalfSize, HalfSize * 2.0F, 0.5F + HalfSize);
        SetTickRandomly(true);
    }

    public override void OnTick(OnTickEvent @event)
    {
        if (Random.Shared.Next(100) != 0)
        {
            return;
        }

        int tryX = @event.X + Random.Shared.Next(3) - 1;
        int tryY = @event.Y + Random.Shared.Next(2) - Random.Shared.Next(2);
        int tryZ = @event.Z + Random.Shared.Next(3) - 1;
        if (!@event.World.Reader.IsAir(tryX, tryY, tryZ) || !CanGrow(new OnTickEvent(@event.World, tryX, tryY, tryZ, @event.World.Reader.GetBlockMeta(tryX, tryY, tryZ), @event.World.Reader.GetBlockId(tryX, tryY, tryZ))))
        {
            return;
        }

        if (@event.World.Reader.IsAir(tryX, tryY, tryZ) && CanGrow(new OnTickEvent(@event.World, tryX, tryY, tryZ, @event.World.Reader.GetBlockMeta(tryX, tryY, tryZ), @event.World.Reader.GetBlockId(tryX, tryY, tryZ))))
        {
            @event.World.Writer.SetBlock(tryX, tryY, tryZ, ID);
        }
    }

    protected override bool CanPlantOnTop(int id) => id == GrassBlock.ID || id == Dirt.ID || id == Stone.ID || id == Gravel.ID || id == Cobblestone.ID;

    public override bool CanGrow(OnTickEvent ctx) => ctx.Y >= 0 && ctx.Y < ChuckFormat.WorldHeight && (ctx.World.Reader.GetBrightness(ctx.X, ctx.Y, ctx.Z) < 13 && CanPlantOnTop(ctx.World.Reader.GetBlockId(ctx.X, ctx.Y - 1, ctx.Z)));
}
