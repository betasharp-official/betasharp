namespace BetaSharp.Blocks;

public class BlockMushroom : BlockPlant
{
    public BlockMushroom(int i, int j) : base(i, j)
    {
        const float halfSize = 0.2F;
        SetBoundingBox(0.5F - halfSize, 0.0F, 0.5F - halfSize, 0.5F + halfSize, halfSize * 2.0F, 0.5F + halfSize);
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
            @event.World.Writer.SetBlock(tryX, tryY, tryZ, Id);
        }
    }

    protected override bool canPlantOnTop(int id) => id == GrassBlock.Id || id == Dirt.Id || id == Stone.Id || id == Gravel.Id || id == Cobblestone.Id;

    public override bool CanGrow(OnTickEvent ctx) => ctx.Y is >= 0 and < 128 &&
                                                     ctx.World.Reader.GetBrightness(ctx.X, ctx.Y, ctx.Z) < 13 &&
                                                     canPlantOnTop(ctx.World.Reader.GetBlockId(ctx.X, ctx.Y - 1, ctx.Z));
}
