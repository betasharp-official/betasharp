using BetaSharp.Blocks.Materials;
using BetaSharp.Items;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Blocks;

internal class BlockRedstoneOre : Block
{
    private readonly bool _lit;

    public BlockRedstoneOre(int id, int textureId, bool lit) : base(id, textureId, Material.Stone)
    {
        if (lit)
        {
            SetTickRandomly(true);
        }

        _lit = lit;
    }

    public override int TickRate => 30;

    public override void OnBlockBreakStart(OnBlockBreakStartEvent @event)
    {
        Light(@event.World.Writer, @event.World.Reader, @event.World.Broadcaster, @event.X, @event.Y, @event.Z);
        base.OnBlockBreakStart(@event);
    }

    public override void OnSteppedOn(OnEntityStepEvent @event)
    {
        Light(@event.World.Writer, @event.World.Reader, @event.World.Broadcaster, @event.X, @event.Y, @event.Z);
        base.OnSteppedOn(@event);
    }

    public override bool OnUse(OnUseEvent @event)
    {
        Light(@event.World.Writer, @event.World.Reader, @event.World.Broadcaster, @event.X, @event.Y, @event.Z);
        return base.OnUse(@event);
    }

    private void Light(IBlockWriter writer, IBlockReader worldRead, WorldEventBroadcaster broadcaster, int x, int y, int z)
    {
        SpawnParticles(worldRead, broadcaster, x, y, z);
        if (worldRead.GetBlockId(x, y, z) == RedstoneOre.Id)
        {
            writer.SetBlock(x, y, z, LitRedstoneOre.Id);
        }
    }

    public override void OnTick(OnTickEvent @event)
    {
        if (Id == LitRedstoneOre.Id)
        {
            @event.World.Writer.SetBlock(@event.X, @event.Y, @event.Z, RedstoneOre.Id);
        }
    }

    public override int GetDroppedItemId(int blockMeta) => Item.Redstone.id;

    public override int DroppedItemCount => 4 + Random.Shared.Next(2);

    public override void RandomDisplayTick(OnTickEvent ctx)
    {
        if (_lit)
        {
            SpawnParticles(ctx.World.Reader, ctx.World.Broadcaster, ctx.X, ctx.Y, ctx.Z);
        }
    }

    private static void SpawnParticles(IBlockReader reader, WorldEventBroadcaster broadcaster, int x, int y, int z)
    {
        const double faceOffset = 1.0D / 16.0D;
        for (int i = 0; i < 6; ++i)
        {
            Side side = (Side)i;

            double particleX = x + Random.Shared.NextSingle();
            double particleY = y + Random.Shared.NextSingle();
            double particleZ = z + Random.Shared.NextSingle();

            switch (side)
            {
                case Side.Up when !reader.IsOpaque(x, y + 1, z):
                    particleY = y + 1 + faceOffset;
                    break;
                case Side.Down when !reader.IsOpaque(x, y - 1, z):
                    particleY = y + 0 - faceOffset;
                    break;
                case Side.South when !reader.IsOpaque(x, y, z + 1):
                    particleZ = z + 1 + faceOffset;
                    break;
                case Side.North when !reader.IsOpaque(x, y, z - 1):
                    particleZ = z + 0 - faceOffset;
                    break;
                case Side.East when !reader.IsOpaque(x + 1, y, z):
                    particleX = x + 1 + faceOffset;
                    break;
                case Side.West when !reader.IsOpaque(x - 1, y, z):
                    particleX = x + 0 - faceOffset;
                    break;
            }

            if (particleX < x || particleX > x + 1 || particleY < 0.0D || particleY > y + 1 || particleZ < z || particleZ > z + 1)
            {
                broadcaster.AddParticle("reddust", particleX, particleY, particleZ, 0.0D, 0.0D, 0.0D);
            }
        }
    }
}
