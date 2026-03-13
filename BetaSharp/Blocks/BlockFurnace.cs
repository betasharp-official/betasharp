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

    public override void onPlaced(OnPlacedEvt evt)
    {
        if (evt.Placer != null)
        {
            int direction = MathHelper.Floor(evt.Placer.yaw * 4.0F / 360.0F + 0.5D) & 3;
            if (direction == 0)
            {
                evt.Level.BlockWriter.SetBlockMeta(evt.X, evt.Y, evt.Z, 2);
            }

            if (direction == 1)
            {
                evt.Level.BlockWriter.SetBlockMeta(evt.X, evt.Y, evt.Z, 5);
            }

            if (direction == 2)
            {
                evt.Level.BlockWriter.SetBlockMeta(evt.X, evt.Y, evt.Z, 3);
            }

            if (direction == 3)
            {
                evt.Level.BlockWriter.SetBlockMeta(evt.X, evt.Y, evt.Z, 4);
            }
        }

        base.onPlaced(evt);
        updateDirection(evt);
    }

    private void updateDirection(OnPlacedEvt evt)
    {
        if (evt.Level.IsRemote)
        {
            return;
        }

        int blockNorth = evt.Level.Reader.GetBlockId(evt.X, evt.Y, evt.Z - 1);
        int blockSouth = evt.Level.Reader.GetBlockId(evt.X, evt.Y, evt.Z + 1);
        int westBlockId = evt.Level.Reader.GetBlockId(evt.X - 1, evt.Y, evt.Z);
        int eastBlockId = evt.Level.Reader.GetBlockId(evt.X + 1, evt.Y, evt.Z);
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

        evt.Level.BlockWriter.SetBlockMeta(evt.X, evt.Y, evt.Z, direction);
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

    public override void randomDisplayTick(OnTickEvt evt)
    {
        if (!_lit)
        {
            return;
        }

        int var6 = evt.Level.Reader.GetBlockMeta(evt.X, evt.Y, evt.Z);
        float particleX = evt.X + 0.5F;
        float particleY = evt.Y + 0.0F + Random.Shared.NextSingle() * 6.0F / 16.0F;
        float particleZ = evt.Z + 0.5F;
        float flameOffset = 0.52F;
        float randomOffset = Random.Shared.NextSingle() * 0.6F - 0.3F;
        if (var6 == 4)
        {
            evt.Level.Broadcaster.AddParticle("smoke", particleX - flameOffset, particleY, particleZ + randomOffset, 0.0D, 0.0D, 0.0D);
            evt.Level.Broadcaster.AddParticle("flame", particleX - flameOffset, particleY, particleZ + randomOffset, 0.0D, 0.0D, 0.0D);
        }
        else if (var6 == 5)
        {
            evt.Level.Broadcaster.AddParticle("smoke", particleX + flameOffset, particleY, particleZ + randomOffset, 0.0D, 0.0D, 0.0D);
            evt.Level.Broadcaster.AddParticle("flame", particleX + flameOffset, particleY, particleZ + randomOffset, 0.0D, 0.0D, 0.0D);
        }
        else if (var6 == 2)
        {
            evt.Level.Broadcaster.AddParticle("smoke", particleX + randomOffset, particleY, particleZ - flameOffset, 0.0D, 0.0D, 0.0D);
            evt.Level.Broadcaster.AddParticle("flame", particleX + randomOffset, particleY, particleZ - flameOffset, 0.0D, 0.0D, 0.0D);
        }
        else if (var6 == 3)
        {
            evt.Level.Broadcaster.AddParticle("smoke", particleX + randomOffset, particleY, particleZ + flameOffset, 0.0D, 0.0D, 0.0D);
            evt.Level.Broadcaster.AddParticle("flame", particleX + randomOffset, particleY, particleZ + flameOffset, 0.0D, 0.0D, 0.0D);
        }
    }

    public override int getTexture(int side)
    {
        return side == 1 ? textureId + 17 : side == 0 ? textureId + 17 : side == 3 ? textureId - 1 : textureId;
    }

    public override bool onUse(OnUseEvt evt)
    {
        if (evt.Level.IsRemote)
        {
            return true;
        }

        BlockEntityFurnace? furnace = (BlockEntityFurnace?)evt.Level.Reader.GetBlockEntity(evt.X, evt.Y, evt.Z);
        if (furnace == null)
        {
            return false;
        }

        evt.Player.openFurnaceScreen(furnace);
        return true;
    }

    public static void updateLitState(bool lit, IWorldContext world, int x, int y, int z)
    {
        int meta = world.Reader.GetBlockMeta(x, y, z);
        BlockEntity? furnace = world.Reader.GetBlockEntity(x, y, z);
        s_ignoreBlockRemoval.Value = true;
        if (lit)
        {
            world.BlockWriter.SetBlock(x, y, z, LitFurnace.id);
        }
        else
        {
            world.BlockWriter.SetBlock(x, y, z, Furnace.id);
        }

        s_ignoreBlockRemoval.Value = false;
        world.BlockWriter.SetBlockMeta(x, y, z, meta);
        furnace?.cancelRemoval();
        world.Entities.SetBlockEntity(x, y, z, furnace!);
    }

    protected override BlockEntity getBlockEntity()
    {
        return new BlockEntityFurnace();
    }

    public override void onBreak(OnBreakEvt evt)
    {
        if (!s_ignoreBlockRemoval.Value)
        {
            BlockEntityFurnace? furnace = (BlockEntityFurnace?)evt.Level.Reader.GetBlockEntity(evt.X, evt.Y, evt.Z);
            if (furnace == null)
            {
                s_logger.LogWarning("BlockEntityFurnace not found at {X}, {Y}, {Z}", evt.X, evt.Y, evt.Z);
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
                        EntityItem droppedItem = new(evt.Level, evt.X + offsetX, evt.Y + offsetY, evt.Z + offsetZ, new ItemStack(stack.itemId, var11, stack.getDamage()));
                        float var13 = 0.05F;
                        droppedItem.velocityX = (float)_random.NextGaussian() * var13;
                        droppedItem.velocityY = (float)_random.NextGaussian() * var13 + 0.2F;
                        droppedItem.velocityZ = (float)_random.NextGaussian() * var13;
                        evt.Level.SpawnEntity(droppedItem);
                    }
                }
            }
        }

        base.onBreak(evt);
    }
}
