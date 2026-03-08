using BetaSharp.Blocks.Entities;
using BetaSharp.Blocks.Materials;
using BetaSharp.Entities;
using BetaSharp.Items;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core;
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

    public override int getDroppedItemId(int blockMeta) => Furnace.id;

    public override void onPlaced(OnPlacedEvt ctx)
    {
        int direction = MathHelper.Floor(ctx.Placer.yaw * 4.0F / 360.0F + 0.5D) & 3;
        if (direction == 0)
        {
            ctx.WorldWrite.SetBlockMeta(ctx.X, ctx.Y, ctx.Z, 2);
        }

        if (direction == 1)
        {
            ctx.WorldWrite.SetBlockMeta(ctx.X, ctx.Y, ctx.Z, 5);
        }

        if (direction == 2)
        {
            ctx.WorldWrite.SetBlockMeta(ctx.X, ctx.Y, ctx.Z, 3);
        }

        if (direction == 3)
        {
            ctx.WorldWrite.SetBlockMeta(ctx.X, ctx.Y, ctx.Z, 4);
        }

        base.onPlaced(ctx);
        updateDirection(ctx);
    }

    private void updateDirection(OnPlacedEvt ctx)
    {
        if (!ctx.IsRemote)
        {
            int blockNorth = ctx.WorldRead.GetBlockId(ctx.X, ctx.Y, ctx.Z - 1);
            int blockSouth = ctx.WorldRead.GetBlockId(ctx.X, ctx.Y, ctx.Z + 1);
            int westBlockId = ctx.WorldRead.GetBlockId(ctx.X - 1, ctx.Y, ctx.Z);
            int eastBlockId = ctx.WorldRead.GetBlockId(ctx.X + 1, ctx.Y, ctx.Z);
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

            ctx.WorldWrite.SetBlockMeta(ctx.X, ctx.Y, ctx.Z, direction);
        }
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

    public override void randomDisplayTick(OnTickEvt ctx)
    {
        if (_lit)
        {
            int var6 = ctx.WorldRead.GetBlockMeta(ctx.X, ctx.Y, ctx.Z);
            float particleX = ctx.X + 0.5F;
            float particleY = ctx.Y + 0.0F + ctx.Random.NextFloat() * 6.0F / 16.0F;
            float particleZ = ctx.Z + 0.5F;
            float flameOffset = 0.52F;
            float randomOffset = ctx.Random.NextFloat() * 0.6F - 0.3F;
            if (var6 == 4)
            {
                ctx.Broadcaster.AddParticle("smoke", particleX - flameOffset, particleY, particleZ + randomOffset, 0.0D, 0.0D, 0.0D);
                ctx.Broadcaster.AddParticle("flame", particleX - flameOffset, particleY, particleZ + randomOffset, 0.0D, 0.0D, 0.0D);
            }
            else if (var6 == 5)
            {
                ctx.Broadcaster.AddParticle("smoke", particleX + flameOffset, particleY, particleZ + randomOffset, 0.0D, 0.0D, 0.0D);
                ctx.Broadcaster.AddParticle("flame", particleX + flameOffset, particleY, particleZ + randomOffset, 0.0D, 0.0D, 0.0D);
            }
            else if (var6 == 2)
            {
                ctx.Broadcaster.AddParticle("smoke", particleX + randomOffset, particleY, particleZ - flameOffset, 0.0D, 0.0D, 0.0D);
                ctx.Broadcaster.AddParticle("flame", particleX + randomOffset, particleY, particleZ - flameOffset, 0.0D, 0.0D, 0.0D);
            }
            else if (var6 == 3)
            {
                ctx.Broadcaster.AddParticle("smoke", particleX + randomOffset, particleY, particleZ + flameOffset, 0.0D, 0.0D, 0.0D);
                ctx.Broadcaster.AddParticle("flame", particleX + randomOffset, particleY, particleZ + flameOffset, 0.0D, 0.0D, 0.0D);
            }
        }
    }

    public override int getTexture(int side) => side == 1 ? textureId + 17 : side == 0 ? textureId + 17 : side == 3 ? textureId - 1 : textureId;

    public override bool onUse(OnUseEvt ctx)
    {
        if (ctx.IsRemote)
        {
            return true;
        }

        BlockEntityFurnace? furnace = (BlockEntityFurnace?)ctx.WorldRead.GetBlockEntity(ctx.X, ctx.Y, ctx.Z);
        if (furnace == null)
        {
            return false;
        }

        ctx.Player.openFurnaceScreen(furnace);
        return true;
    }

    public static void updateLitState(bool lit, World world, int x, int y, int z)
    {
        int meta = world.getBlockMeta(x, y, z);
        BlockEntity furnace = world.getBlockEntity(x, y, z);
        s_ignoreBlockRemoval.Value = true;
        if (lit)
        {
            world.setBlock(x, y, z, LitFurnace.id);
        }
        else
        {
            world.setBlock(x, y, z, Furnace.id);
        }

        s_ignoreBlockRemoval.Value = false;
        world.setBlockMeta(x, y, z, meta);
        furnace.cancelRemoval();
        world.Entities.SetBlockEntity(x, y, z, furnace);
    }

    protected override BlockEntity getBlockEntity() => new BlockEntityFurnace();

    public override void onBreak(OnBreakEvt ctx)
    {
        if (!s_ignoreBlockRemoval.Value)
        {
            BlockEntityFurnace? furnace = (BlockEntityFurnace?)ctx.WorldRead.GetBlockEntity(ctx.X, ctx.Y, ctx.Z);
            if (furnace == null)
            {
                s_logger.LogWarning("BlockEntityFurnace not found at {X}, {Y}, {Z}", ctx.X, ctx.Y, ctx.Z);
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
                        // EntityItem droppedItem = new(world, x + offsetX, y + offsetY, z + offsetZ, new ItemStack(stack.itemId, var11, stack.getDamage()));
                        // float var13 = 0.05F;
                        // droppedItem.velocityX = (float)_random.NextGaussian() * var13;
                        // droppedItem.velocityY = (float)_random.NextGaussian() * var13 + 0.2F;
                        // droppedItem.velocityZ = (float)_random.NextGaussian() * var13;
                        // world.SpawnEntity(droppedItem);
                    }
                }
            }
        }

        base.onBreak(ctx);
    }
}
