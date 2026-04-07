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
    private const float FlameParticleOffset = 0.52F;
    private const float DropSpread = 0.05F;

    private static readonly ILogger<BlockFurnace> s_logger = BetaSharp.Log.Instance.For<BlockFurnace>();
    private static readonly ThreadLocal<bool> s_ignoreBlockRemoval = new(() => false);

    private static readonly JavaRandom s_random = new();
    private readonly bool _lit;

    public BlockFurnace(int id, bool lit) : base(id, Material.Stone)
    {
        _lit = lit;
        TextureId = 45;
    }

    public override int getDroppedItemId(int blockMeta) => Furnace.id;

    public override void onPlaced(OnPlacedEvent @event)
    {
        base.onPlaced(@event);

        if (@event.Placer != null)
        {
            int direction = MathHelper.Floor(@event.Placer.yaw * 4.0F / 360.0F + 0.5D) & 3;

            int meta = direction switch
            {
                0 => 2,
                1 => 5,
                2 => 3,
                3 => 4,
                _ => 2
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
        if (@event.World.IsRemote) return;

        IBlockReader reader = @event.World.Reader;
        int x = @event.X, y = @event.Y, z = @event.Z;

        bool isNorthOpaque = BlocksOpaque[reader.GetBlockId(x, y, z - 1)];
        bool isSouthOpaque = BlocksOpaque[reader.GetBlockId(x, y, z + 1)];
        bool isWestOpaque = BlocksOpaque[reader.GetBlockId(x - 1, y, z)];
        bool isEastOpaque = BlocksOpaque[reader.GetBlockId(x + 1, y, z)];

        byte direction = 3;
        if (isNorthOpaque && !isSouthOpaque) direction = 3;
        else if (isSouthOpaque && !isNorthOpaque) direction = 2;


        if (isWestOpaque && !isEastOpaque) direction = 5;
        else if (isEastOpaque && !isWestOpaque) direction = 4;


        @event.World.Writer.SetBlockMeta(x, y, z, direction);
    }

    public override int GetTextureId(IBlockReader iBlockReader, int x, int y, int z, Side side)
    {
        if (side is Side.Up or Side.Down) return TextureId + 17;

        Side meta = iBlockReader.GetBlockMeta(x, y, z).ToSide();
        return side != meta ? TextureId : _lit ? TextureId + 16 : TextureId - 1;
    }


    public override void randomDisplayTick(OnTickEvent @event)
    {
        if (!_lit) return;

        Side rotation = @event.World.Reader.GetBlockMeta(@event.X, @event.Y, @event.Z).ToSide();
        float particleX = @event.X + 0.5F;
        float particleY = @event.Y + 0.0F + Random.Shared.NextSingle() * 6.0F / 16.0F;
        float particleZ = @event.Z + 0.5F;
        float randomOffset = Random.Shared.NextSingle() * 0.6F - 0.3F;

        switch (rotation)
        {
            case Side.West:
                @event.World.Broadcaster.AddParticle("smoke", particleX - FlameParticleOffset, particleY, particleZ + randomOffset, 0.0D, 0.0D, 0.0D);
                @event.World.Broadcaster.AddParticle("flame", particleX - FlameParticleOffset, particleY, particleZ + randomOffset, 0.0D, 0.0D, 0.0D);
                break;
            case Side.East:
                @event.World.Broadcaster.AddParticle("smoke", particleX + FlameParticleOffset, particleY, particleZ + randomOffset, 0.0D, 0.0D, 0.0D);
                @event.World.Broadcaster.AddParticle("flame", particleX + FlameParticleOffset, particleY, particleZ + randomOffset, 0.0D, 0.0D, 0.0D);
                break;
            case Side.North:
                @event.World.Broadcaster.AddParticle("smoke", particleX + randomOffset, particleY, particleZ - FlameParticleOffset, 0.0D, 0.0D, 0.0D);
                @event.World.Broadcaster.AddParticle("flame", particleX + randomOffset, particleY, particleZ - FlameParticleOffset, 0.0D, 0.0D, 0.0D);
                break;
            case Side.South:
                @event.World.Broadcaster.AddParticle("smoke", particleX + randomOffset, particleY, particleZ + FlameParticleOffset, 0.0D, 0.0D, 0.0D);
                @event.World.Broadcaster.AddParticle("flame", particleX + randomOffset, particleY, particleZ + FlameParticleOffset, 0.0D, 0.0D, 0.0D);
                break;
        }
    }

    public override int GetTexture(Side side) => side switch
    {
        Side.Up or Side.Down => TextureId + 17,
        Side.South => TextureId - 1,
        _ => TextureId
    };

    public override bool onUse(OnUseEvent @event)
    {
        if (@event.World.IsRemote) return true;

        BlockEntityFurnace? furnace = @event.World.Entities.GetBlockEntity<BlockEntityFurnace>(@event.X, @event.Y, @event.Z);
        if (furnace == null) return false;

        @event.Player.openFurnaceScreen(furnace);
        return true;
    }

    public static void updateLitState(bool lit, IWorldContext world, int x, int y, int z)
    {
        int meta = world.Reader.GetBlockMeta(x, y, z);
        BlockEntity? furnace = world.Entities.GetBlockEntity<BlockEntity>(x, y, z);
        s_ignoreBlockRemoval.Value = true;
        world.Writer.SetBlock(x, y, z, lit ? LitFurnace.id : Furnace.id);

        s_ignoreBlockRemoval.Value = false;
        world.Writer.SetBlockMeta(x, y, z, meta);
        furnace?.cancelRemoval();
        world.Entities.SetBlockEntity(x, y, z, furnace!);
    }

    public override BlockEntity getBlockEntity() => new BlockEntityFurnace();

    public override void onBreak(OnBreakEvent @event)
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
                if (stack == null) continue;

                float offsetX = s_random.NextFloat() * 0.8F + 0.1F;
                float offsetY = s_random.NextFloat() * 0.8F + 0.1F;
                float offsetZ = s_random.NextFloat() * 0.8F + 0.1F;

                while (stack.count > 0)
                {
                    int stackCount = s_random.NextInt(21) + 10;
                    if (stackCount > stack.count) stackCount = stack.count;

                    stack.count -= stackCount;
                    EntityItem droppedItem = new(@event.World, @event.X + offsetX, @event.Y + offsetY, @event.Z + offsetZ, new ItemStack(stack.itemId, stackCount, stack.getDamage()))
                    {
                        velocityX = (float)s_random.NextGaussian() * DropSpread,
                        velocityY = (float)s_random.NextGaussian() * DropSpread + 0.2F,
                        velocityZ = (float)s_random.NextGaussian() * DropSpread
                    };
                    @event.World.SpawnEntity(droppedItem);
                }
            }
        }

        base.onBreak(@event);
    }
}
