using BetaSharp.Blocks;
using BetaSharp.Entities;
using BetaSharp.Items;
using BetaSharp.Network.Packets.S2CPlay;
using BetaSharp.Worlds;

namespace BetaSharp.Server.Network;

/*
 * mining and miningX,Y,Z don't mean the block you are mining.
 * It's used in update when the player provided brake was invalid.
 *
 * Instead, failedMining is the currently pending mining operation.
 */

/// <summary>
/// Handles mining, placing, opening doors, and so on.
/// </summary>
public class ServerPlayerInteractionManager
{
    private readonly ServerWorld world;
    public EntityPlayer player;
    private int failedMiningStartTime;
    private int failedMiningX;
    private int failedMiningY;
    private int failedMiningZ;
    private int tickCounter;
    private bool mining;
    private int miningX;
    private int miningY;
    private int miningZ;
    private int startMiningTime;
    private float miningProgress = -1;

    public ServerPlayerInteractionManager(ServerWorld world)
    {
        this.world = world;
    }

    public void update()
    {
        tickCounter++;
        if (mining)
        {
            int miningTicks = tickCounter - startMiningTime;
            int blockId = world.getBlockId(miningX, miningY, miningZ);
            if (blockId != 0)
            {
                Block block = Block.Blocks[blockId];
                float breakProgress = block.getHardness(player) * (miningTicks + 1);
                if (breakProgress >= 1.0F)
                {
                    mining = false;
                    miningProgress = -1;
                    tryBreakBlock(miningX, miningY, miningZ);
                }
            }
            else
            {
                mining = false;
                miningProgress = -1;
            }
        }
    }

    public void onBlockBreakingAction(int x, int y, int z, int direction)
    {
        world.extinguishFire(null, x, y, z, direction);
        failedMiningStartTime = tickCounter;
        int blockId = world.getBlockId(x, y, z);
        if (blockId > 0)
        {
            Block.Blocks[blockId].onBlockBreakStart(world, x, y, z, player);
        }

        if (blockId > 0 && Block.Blocks[blockId].getHardness(player) >= 1.0F)
        {
            tryBreakBlock(x, y, z);
            miningProgress = -1;
        }
        else
        {
            failedMiningX = x;
            failedMiningY = y;
            failedMiningZ = z;
            // Previously this margin was in continueMining
            miningProgress = 0.3f;
        }
    }

    public void continueMining(int x, int y, int z)
    {
        if (x == failedMiningX && y == failedMiningY && z == failedMiningZ)
        {
            int ticksSinceFailedStart = tickCounter - failedMiningStartTime;
            int blockId = world.getBlockId(x, y, z);
            if (blockId != 0)
            {
                Block block = Block.Blocks[blockId];
                float breakProgress = block.getHardness(player) * (ticksSinceFailedStart + 1) + miningProgress;
                if (breakProgress >= 1F)
                {
                    tryBreakBlock(x, y, z);
                    miningProgress = -1;
                }
                else if (!mining)
                {
                    // Player submitted block brake was not accepted.
                    // As the block was not mined, we will check in update for when the block should be broken.
                    mining = true;
                    miningX = x;
                    miningY = y;
                    miningZ = z;
                    startMiningTime = failedMiningStartTime;
                }
            }
        }
    }

    public bool finishMining(int x, int y, int z)
    {
        Block block = Block.Blocks[world.getBlockId(x, y, z)];
        int blockMeta = world.getBlockMeta(x, y, z);
        bool success = world.setBlock(x, y, z, 0);
        if (block != null && success)
        {
            block.onMetadataChange(world, x, y, z, blockMeta);
        }

        return success;
    }

    public void UpdateMiningTool()
    {
        if (miningProgress is < 0F or >= 1F) return;
        int blockId = world.getBlockId(failedMiningX, failedMiningY, failedMiningZ);
        if (blockId == 0)
        {
            miningProgress = -1;
            return;
        }

        int ticksSinceFailedStart = tickCounter - failedMiningStartTime;
        failedMiningStartTime = tickCounter;
        Block block = Block.Blocks[blockId];
        miningProgress += block.getHardness(player) * ticksSinceFailedStart;
    }

    public bool tryBreakBlock(int x, int y, int z)
    {
        int blockId = world.getBlockId(x, y, z);
        int blockMeta = world.getBlockMeta(x, y, z);
        world.worldEvent(player, 2001, x, y, z, blockId + world.getBlockMeta(x, y, z) * 256);
        bool success = finishMining(x, y, z);

        if (success && player.canHarvest(Block.Blocks[blockId]))
        {
            Block.Blocks[blockId].afterBreak(world, player, x, y, z, blockMeta);
            ((ServerPlayerEntity)player).networkHandler.sendPacket(BlockUpdateS2CPacket.Get(x, y, z, world));
        }

        ItemStack itemStack = player.getHand();
        if (itemStack != null)
        {
            itemStack.postMine(blockId, x, y, z, player);
            if (itemStack.count == 0)
            {
                itemStack.onRemoved(player);
                player.clearStackInHand();
            }
        }

        return success;
    }

    public bool interactItem(EntityPlayer player, World world, ItemStack stack)
    {
        int count = stack.count;
        ItemStack itemStack = stack.use(world, player);
        if (itemStack != stack || itemStack != null && itemStack.count != count)
        {
            player.inventory.main[player.inventory.selectedSlot] = itemStack;
            if (itemStack.count == 0)
            {
                player.inventory.main[player.inventory.selectedSlot] = null;
            }

            miningProgress = -1;

            return true;
        }
        else
        {
            return false;
        }
    }

    public bool interactBlock(EntityPlayer player, World world, ItemStack? stack, int x, int y, int z, int side)
    {
        int blockId = world.getBlockId(x, y, z);
        if (blockId > 0 && Block.Blocks[blockId].onUse(world, x, y, z, player))
        {
            miningProgress = -1;
            return true;
        }

        if (stack == null) return false;
        if (stack.useOnBlock(player, world, x, y, z, side))
        {
            miningProgress = -1;
            return true;
        }

        return false;
    }
}
