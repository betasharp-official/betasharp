using BetaSharp.Blocks.Entities;
using BetaSharp.Blocks.Materials;
using BetaSharp.Entities;
using BetaSharp.Items;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core.Systems;
using Microsoft.Extensions.Logging;

namespace BetaSharp.Blocks;

internal class BlockFurnace : BlockWithEntity
{
    private static readonly ILogger<BlockFurnace> s_logger = BetaSharp.Log.Instance.For<BlockFurnace>();
    private static readonly ThreadLocal<bool> s_ignoreBlockRemoval = new(() => false);
    private readonly bool _lit;

    private readonly JavaRandom _random = new();

    public BlockFurnace(int id, bool lit) : base(id, Material.Stone)
    {
        _lit = lit;
        TextureId = 45;
    }

    public override int GetDroppedItemId(int blockMeta) => Furnace.Id;

    public override void OnPlaced(OnPlacedEvent @event)
    {
        base.OnPlaced(@event);

        if (@event.Placer != null)
        {
            int direction = MathHelper.Floor(@event.Placer.yaw * 4.0F / 360.0F + 0.5D) & 3;

            int meta = direction switch
            {
                0 => (int)Side.North,
                1 => (int)Side.East,
                2 => (int)Side.South,
                3 => (int)Side.West,
                _ => (int)Side.North
            };

            @event.World.Writer.SetBlockMeta(@event.X, @event.Y, @event.Z, meta);

            if (!@event.World.IsRemote)
            {
                @event.World.Writer.SetBlockMeta(@event.X, @event.Y, @event.Z, meta);
            }
        }
        else
        {
            updateDirection(@event);
        }
    }

    private void updateDirection(OnPlacedEvent @event)
    {
        if (@event.World.IsRemote)
        {
            return;
        }

        IBlockReader reader = @event.World.Reader;
        int x = @event.X, y = @event.Y, z = @event.Z;

        bool isNorthOpaque = BlocksOpaque[reader.GetBlockId(x, y, z - 1)];
        bool isSouthOpaque = BlocksOpaque[reader.GetBlockId(x, y, z + 1)];
        bool isWestOpaque = BlocksOpaque[reader.GetBlockId(x - 1, y, z)];
        bool isEastOpaque = BlocksOpaque[reader.GetBlockId(x + 1, y, z)];

        Side direction = Side.South;
        if (isNorthOpaque && !isSouthOpaque)
        {
            direction = Side.South;
        }
        else if (isSouthOpaque && !isNorthOpaque)
        {
            direction = Side.North;
        }

        if (isWestOpaque && !isEastOpaque)
        {
            direction = Side.East;
        }
        else if (isEastOpaque && !isWestOpaque)
        {
            direction = Side.West;
        }

        @event.World.Writer.SetBlockMeta(x, y, z, direction.ToInt());
    }

    public override int GetTextureId(IBlockReader iBlockReader, int x, int y, int z, Side side)
    {
        if (side is Side.Up or Side.Down)
        {
            return TextureId + 17;
        }

        int meta = iBlockReader.GetBlockMeta(x, y, z);
        return side.ToInt() != meta ? TextureId : _lit ? TextureId + 16 : TextureId - 1;
    }

    public override void RandomDisplayTick(OnTickEvent @event)
    {
        if (!_lit)
        {
            return;
        }

        const float flameOffset = 0.52F;

        Side side = @event.World.Reader.GetBlockMeta(@event.X, @event.Y, @event.Z).ToSide();
        float particleX = @event.X + 0.5F;
        float particleY = @event.Y + 0.0F + Random.Shared.NextSingle() * 6.0F / 16.0F;
        float particleZ = @event.Z + 0.5F;
        float randomOffset = Random.Shared.NextSingle() * 0.6F - 0.3F;

        switch (side)
        {
            case Side.West:
                @event.World.Broadcaster.AddParticle("smoke", particleX - flameOffset, particleY, particleZ + randomOffset, 0.0D, 0.0D, 0.0D);
                @event.World.Broadcaster.AddParticle("flame", particleX - flameOffset, particleY, particleZ + randomOffset, 0.0D, 0.0D, 0.0D);
                break;
            case Side.East:
                @event.World.Broadcaster.AddParticle("smoke", particleX + flameOffset, particleY, particleZ + randomOffset, 0.0D, 0.0D, 0.0D);
                @event.World.Broadcaster.AddParticle("flame", particleX + flameOffset, particleY, particleZ + randomOffset, 0.0D, 0.0D, 0.0D);
                break;
            case Side.North:
                @event.World.Broadcaster.AddParticle("smoke", particleX + randomOffset, particleY, particleZ - flameOffset, 0.0D, 0.0D, 0.0D);
                @event.World.Broadcaster.AddParticle("flame", particleX + randomOffset, particleY, particleZ - flameOffset, 0.0D, 0.0D, 0.0D);
                break;
            case Side.South:
                @event.World.Broadcaster.AddParticle("smoke", particleX + randomOffset, particleY, particleZ + flameOffset, 0.0D, 0.0D, 0.0D);
                @event.World.Broadcaster.AddParticle("flame", particleX + randomOffset, particleY, particleZ + flameOffset, 0.0D, 0.0D, 0.0D);
                break;
        }
    }

    public override int GetTexture(Side side) => side switch
    {
        Side.Up or Side.Down => TextureId + 17,
        Side.South => TextureId - 1,
        _ => TextureId
    };

    public override bool OnUse(OnUseEvent @event)
    {
        if (@event.World.IsRemote)
        {
            return true;
        }

        BlockEntityFurnace? furnace = @event.World.Entities.GetBlockEntity<BlockEntityFurnace>(@event.X, @event.Y, @event.Z);
        if (furnace == null)
        {
            return false;
        }

        @event.Player.openFurnaceScreen(furnace);
        return true;
    }

    public static void updateLitState(bool lit, IWorldContext world, int x, int y, int z)
    {
        int meta = world.Reader.GetBlockMeta(x, y, z);
        BlockEntity? furnace = world.Entities.GetBlockEntity<BlockEntity>(x, y, z);
        s_ignoreBlockRemoval.Value = true;
        world.Writer.SetBlock(x, y, z, lit ? LitFurnace.Id : Furnace.Id);
        s_ignoreBlockRemoval.Value = false;
        world.Writer.SetBlockMeta(x, y, z, meta);
        furnace?.cancelRemoval();
        world.Entities.SetBlockEntity(x, y, z, furnace!);
    }

    public override BlockEntity? getBlockEntity() => new BlockEntityFurnace();

    public override void OnBreak(OnBreakEvent @event)
    {
        if (!s_ignoreBlockRemoval.Value)
        {
            BlockEntityFurnace? furnace = @event.World.Entities.GetBlockEntity<BlockEntityFurnace>(@event.X, @event.Y, @event.Z);

            if (furnace == null)
            {
                s_logger.LogWarning("BlockEntityFurnace not found at {X}, {Y}, {Z}", @event.X, @event.Y, @event.Z);
                return;
            }

            for (int slotIndex = 0; slotIndex < furnace.size(); ++slotIndex)
            {
                ItemStack? stack = furnace.getStack(slotIndex);
                if (stack == null)
                {
                    continue;
                }

                float offsetX = _random.NextFloat() * 0.8F + 0.1F;
                float offsetY = _random.NextFloat() * 0.8F + 0.1F;
                float offsetZ = _random.NextFloat() * 0.8F + 0.1F;

                while (stack.count > 0)
                {
                    int count = _random.NextInt(21) + 10;
                    if (count > stack.count)
                    {
                        count = stack.count;
                    }

                    stack.count -= count;
                    const float spread = 0.05F;
                    EntityItem droppedItem = new(@event.World, @event.X + offsetX, @event.Y + offsetY, @event.Z + offsetZ, new ItemStack(stack.itemId, count, stack.getDamage()))
                    {
                        velocityX = (float)_random.NextGaussian() * spread,
                        velocityY = (float)_random.NextGaussian() * spread + 0.2F,
                        velocityZ = (float)_random.NextGaussian() * spread
                    };
                    @event.World.SpawnEntity(droppedItem);
                }
            }
        }

        base.OnBreak(@event);
    }
}
