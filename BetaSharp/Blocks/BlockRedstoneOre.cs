using BetaSharp.Blocks.Materials;
using BetaSharp.Items;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Blocks;

internal class BlockRedstoneOre : Block
{
    private readonly bool lit;

    public BlockRedstoneOre(int id, int textureId, bool lit) : base(id, textureId, Material.Stone)
    {
        if (lit)
        {
            setTickRandomly(true);
        }

        this.lit = lit;
    }

    public override int getTickRate() => 30;

    public override void onBlockBreakStart(OnBlockBreakStartEvent @event)
    {
        light(@event.World.Writer, @event.World.Reader, @event.World.Broadcaster, @event.X, @event.Y, @event.Z);
        base.onBlockBreakStart(@event);
    }

    public override void onSteppedOn(OnEntityStepEvent @event)
    {
        light(@event.World.Writer, @event.World.Reader, @event.World.Broadcaster, @event.X, @event.Y, @event.Z);
        base.onSteppedOn(@event);
    }

    public override bool onUse(OnUseEvent @event)
    {
        light(@event.World.Writer, @event.World.Reader, @event.World.Broadcaster, @event.X, @event.Y, @event.Z);
        return base.onUse(@event);
    }

    private void light(IBlockWrite worldWrite, IBlockReader worldRead, WorldEventBroadcaster broadcaster, int x, int y, int z)
    {
        spawnParticles(worldRead, broadcaster, x, y, z);
        if (worldRead.GetBlockId(x, y, z) == RedstoneOre.id)
        {
            worldWrite.SetBlock(x, y, z, LitRedstoneOre.id);
        }
    }

    public override void onTick(OnTickEvent @event)
    {
        if (id == LitRedstoneOre.id)
        {
            @event.World.Writer.SetBlock(@event.X, @event.Y, @event.Z, RedstoneOre.id);
        }
    }

    public override int getDroppedItemId(int blockMeta) => Item.Redstone.id;

    public override int getDroppedItemCount() => 4 + Random.Shared.Next(2);

    public override void randomDisplayTick(OnTickEvent ctx)
    {
        if (lit)
        {
            spawnParticles(ctx.World.Reader, ctx.World.Broadcaster, ctx.X, ctx.Y, ctx.Z);
        }
    }

    private void spawnParticles(IBlockReader reader, WorldEventBroadcaster broadcaster, int x, int y, int z)
    {
        double faceOffset = 1.0D / 16.0D;
        for (int direction = 0; direction < 6; ++direction)
        {
            double particleX = x + Random.Shared.NextSingle();
            double particleY = y + Random.Shared.NextSingle();
            double particleZ = z + Random.Shared.NextSingle();
            if (direction == 0 && !reader.IsOpaque(x, y + 1, z))
            {
                particleY = y + 1 + faceOffset;
            }

            if (direction == 1 && !reader.IsOpaque(x, y - 1, z))
            {
                particleY = y + 0 - faceOffset;
            }

            if (direction == 2 && !reader.IsOpaque(x, y, z + 1))
            {
                particleZ = z + 1 + faceOffset;
            }

            if (direction == 3 && !reader.IsOpaque(x, y, z - 1))
            {
                particleZ = z + 0 - faceOffset;
            }

            if (direction == 4 && !reader.IsOpaque(x + 1, y, z))
            {
                particleX = x + 1 + faceOffset;
            }

            if (direction == 5 && !reader.IsOpaque(x - 1, y, z))
            {
                particleX = x + 0 - faceOffset;
            }

            if (particleX < x || particleX > x + 1 || particleY < 0.0D || particleY > y + 1 || particleZ < z || particleZ > z + 1)
            {
                broadcaster.AddParticle("reddust", particleX, particleY, particleZ, 0.0D, 0.0D, 0.0D);
            }
        }
    }
}
