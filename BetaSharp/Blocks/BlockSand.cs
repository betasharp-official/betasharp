using BetaSharp.Blocks.Materials;
using BetaSharp.Entities;

namespace BetaSharp.Blocks;

internal class BlockSand(int id, int textureId) : Block(id, textureId, Material.Sand)
{
    private const sbyte CheckRadius = 32;
    private static readonly ThreadLocal<bool> s_fallInstantly = new(() => false);

    public static bool FallInstantly
    {
        get => s_fallInstantly.Value;
        set => s_fallInstantly.Value = value;
    }

    public override void OnPlaced(OnPlacedEvent ctx) => ctx.World.TickScheduler.ScheduleBlockUpdate(ctx.X, ctx.Y, ctx.Z, ID, GetTickRate());

    public override void NeighborUpdate(OnTickEvent ctx) => ctx.World.TickScheduler.ScheduleBlockUpdate(ctx.X, ctx.Y, ctx.Z, ID, GetTickRate());

    public override void OnTick(OnTickEvent @event) => ProcessFall(@event);

    private void ProcessFall(OnTickEvent @event)
    {
        (int x, int y, int z) = (@event.X, @event.Y, @event.Z);
        if (y <= 0 || !CanFallThrough(new OnTickEvent(@event.World, x, y - 1, z, 0, @event.BlockId)))
        {
            return;
        }

        if (!FallInstantly && @event.World.ChunkHost.IsRegionLoaded(x - CheckRadius, y - CheckRadius, z - CheckRadius, x + CheckRadius, y + CheckRadius, z + CheckRadius))
        {
            EntityFallingSand fallingSand = new(@event.World, x + 0.5F, y + 0.5F, z + 0.5F, ID);
            @event.World.Entities.SpawnEntity(fallingSand);
        }
        else
        {
            @event.World.Writer.SetBlock(x, y, z, 0);

            while (CanFallThrough(new OnTickEvent(@event.World, x, y - 1, z, 0, @event.BlockId)) && y > 0)
            {
                --y;
            }

            if (y > 0)
            {
                @event.World.Writer.SetBlock(x, y, z, ID);
            }
        }
    }

    public override int GetTickRate() => 3;

    public static bool CanFallThrough(OnTickEvent ctx)
    {
        int blockId = ctx.World.Reader.GetBlockId(ctx.X, ctx.Y, ctx.Z);
        if (blockId == 0)
        {
            return true;
        }

        if (blockId == Fire.ID)
        {
            return true;
        }

        Material material = Blocks[blockId].Material;
        return material == Material.Water || material == Material.Lava;
    }
}
