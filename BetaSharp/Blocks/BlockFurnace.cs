using BetaSharp.Blocks.Entities;
using BetaSharp.Blocks.Materials;
using BetaSharp.Entities;
using BetaSharp.Items;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core;
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
        textureId = 45;
    }

    public override int getDroppedItemId(int blockMeta)
    {
        return Furnace.id;
    }

    public override void onPlaced(OnPlacedEvent @event)
    {
        if (@event.Placer != null)
        {
            int direction = MathHelper.Floor(@event.Placer.yaw * 4.0F / 360.0F + 0.5D) & 3;
            if (direction == 0)
            {
                @event.World.Writer.SetBlockMeta(@event.X, @event.Y, @event.Z, 2);
            }

            if (direction == 1)
            {
                @event.World.Writer.SetBlockMeta(@event.X, @event.Y, @event.Z, 5);
            }

            if (direction == 2)
            {
                @event.World.Writer.SetBlockMeta(@event.X, @event.Y, @event.Z, 3);
            }

            if (direction == 3)
            {
                @event.World.Writer.SetBlockMeta(@event.X, @event.Y, @event.Z, 4);
            }
        }

        base.onPlaced(@event);
        updateDirection(@event);
    }

    private void updateDirection(OnPlacedEvent @event)
    {
        if (@event.World.IsRemote)
        {
            return;
        }

        int blockNorth = @event.World.Reader.GetBlockId(@event.X, @event.Y, @event.Z - 1);
        int blockSouth = @event.World.Reader.GetBlockId(@event.X, @event.Y, @event.Z + 1);
        int westBlockId = @event.World.Reader.GetBlockId(@event.X - 1, @event.Y, @event.Z);
        int eastBlockId = @event.World.Reader.GetBlockId(@event.X + 1, @event.Y, @event.Z);
        sbyte direction = 3;
        if (BlocksOpaque[blockNorth] && !BlocksOpaque[blockSouth])
        {
            direction = 3;
        }

        if (BlocksOpaque[blockSouth] && !BlocksOpaque[blockNorth])
        {
            direction = 2;
        }

        if (BlocksOpaque[westBlockId] && !BlocksOpaque[eastBlockId])
        {
            direction = 5;
        }

        if (BlocksOpaque[eastBlockId] && !BlocksOpaque[westBlockId])
        {
            direction = 4;
        }

        @event.World.Writer.SetBlockMeta(@event.X, @event.Y, @event.Z, direction);
    }

    public override int getTextureId(IBlockReader iBlockReader, int x, int y, int z, int side)
    {
        if (side == 1)
        {
            return textureId + 17;
        }

        if (side == 0)
        {
            return textureId + 17;
        }

        int meta = iBlockReader.GetBlockMeta(x, y, z);
        return side != meta ? textureId : _lit ? textureId + 16 : textureId - 1;
    }

    public override void randomDisplayTick(OnTickEvent @event)
    {
        if (!_lit)
        {
            return;
        }

        int var6 = @event.World.Reader.GetBlockMeta(@event.X, @event.Y, @event.Z);
        float particleX = @event.X + 0.5F;
        float particleY = @event.Y + 0.0F + Random.Shared.NextSingle() * 6.0F / 16.0F;
        float particleZ = @event.Z + 0.5F;
        float flameOffset = 0.52F;
        float randomOffset = Random.Shared.NextSingle() * 0.6F - 0.3F;
        if (var6 == 4)
        {
            @event.World.Broadcaster.AddParticle("smoke", particleX - flameOffset, particleY, particleZ + randomOffset, 0.0D, 0.0D, 0.0D);
            @event.World.Broadcaster.AddParticle("flame", particleX - flameOffset, particleY, particleZ + randomOffset, 0.0D, 0.0D, 0.0D);
        }
        else if (var6 == 5)
        {
            @event.World.Broadcaster.AddParticle("smoke", particleX + flameOffset, particleY, particleZ + randomOffset, 0.0D, 0.0D, 0.0D);
            @event.World.Broadcaster.AddParticle("flame", particleX + flameOffset, particleY, particleZ + randomOffset, 0.0D, 0.0D, 0.0D);
        }
        else if (var6 == 2)
        {
            @event.World.Broadcaster.AddParticle("smoke", particleX + randomOffset, particleY, particleZ - flameOffset, 0.0D, 0.0D, 0.0D);
            @event.World.Broadcaster.AddParticle("flame", particleX + randomOffset, particleY, particleZ - flameOffset, 0.0D, 0.0D, 0.0D);
        }
        else if (var6 == 3)
        {
            @event.World.Broadcaster.AddParticle("smoke", particleX + randomOffset, particleY, particleZ + flameOffset, 0.0D, 0.0D, 0.0D);
            @event.World.Broadcaster.AddParticle("flame", particleX + randomOffset, particleY, particleZ + flameOffset, 0.0D, 0.0D, 0.0D);
        }
    }

    public override int getTexture(int side)
    {
        return side == 1 ? textureId + 17 : side == 0 ? textureId + 17 : side == 3 ? textureId - 1 : textureId;
    }

    public override bool onUse(OnUseEvent @event)
    {
        if (@event.World.IsRemote)
        {
            return true;
        }

        BlockEntityFurnace? furnace = (BlockEntityFurnace?)@event.World.Reader.GetBlockEntity(@event.X, @event.Y, @event.Z);
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
        BlockEntity? furnace = world.Reader.GetBlockEntity(x, y, z);
        s_ignoreBlockRemoval.Value = true;
        if (lit)
        {
            world.Writer.SetBlock(x, y, z, LitFurnace.id);
        }
        else
        {
            world.Writer.SetBlock(x, y, z, Furnace.id);
        }

        s_ignoreBlockRemoval.Value = false;
        world.Writer.SetBlockMeta(x, y, z, meta);
        furnace?.cancelRemoval();
        world.Entities.SetBlockEntity(x, y, z, furnace!);
    }

    protected override BlockEntity getBlockEntity()
    {
        return new BlockEntityFurnace();
    }

    public override void onBreak(OnBreakEvent @event)
    {
        if (!s_ignoreBlockRemoval.Value)
        {
            BlockEntityFurnace? furnace = (BlockEntityFurnace?)@event.World.Reader.GetBlockEntity(@event.X, @event.Y, @event.Z);
            if (furnace == null)
            {
                s_logger.LogWarning("BlockEntityFurnace not found at {X}, {Y}, {Z}", @event.X, @event.Y, @event.Z);
                return;
            }

            for (int slotIndex = 0; slotIndex < furnace.size(); ++slotIndex)
            {
                ItemStack stack = furnace.getStack(slotIndex);
                if (stack != null)
                {
                    float offsetX = _random.NextFloat() * 0.8F + 0.1F;
                    float offsetY = _random.NextFloat() * 0.8F + 0.1F;
                    float offsetZ = _random.NextFloat() * 0.8F + 0.1F;

                    while (stack.count > 0)
                    {
                        int var11 = _random.NextInt(21) + 10;
                        if (var11 > stack.count)
                        {
                            var11 = stack.count;
                        }

                        stack.count -= var11;
                        EntityItem droppedItem = new(@event.World, @event.X + offsetX, @event.Y + offsetY, @event.Z + offsetZ, new ItemStack(stack.itemId, var11, stack.getDamage()));
                        float var13 = 0.05F;
                        droppedItem.velocityX = (float)_random.NextGaussian() * var13;
                        droppedItem.velocityY = (float)_random.NextGaussian() * var13 + 0.2F;
                        droppedItem.velocityZ = (float)_random.NextGaussian() * var13;
                        @event.World.SpawnEntity(droppedItem);
                    }
                }
            }
        }

        base.onBreak(@event);
    }
}
