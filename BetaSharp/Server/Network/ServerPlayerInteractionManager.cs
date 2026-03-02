using BetaSharp.Blocks;
using BetaSharp.Entities;
using BetaSharp.Items;
using BetaSharp.Network.Packets.S2CPlay;
using BetaSharp.Worlds;

namespace BetaSharp.Server.Network;

public class ServerPlayerInteractionManager
{
    private readonly ServerWorld world;
    public EntityPlayer Player;
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

    public ServerPlayerInteractionManager(ServerWorld world)
    {
        this.world = world;
    }

    public void Update()
    {
        tickCounter++;
        if (mining)
        {
            int miningTicks = tickCounter - startMiningTime;
            int blockId = world.getBlockId(miningX, miningY, miningZ);
            if (blockId != 0)
            {
                Block block = Block.Blocks[blockId];
                float breakProgress = block.getHardness(Player) * (miningTicks + 1);
                if (breakProgress >= 1.0F)
                {
                    mining = false;
                    TryBreakBlock(miningX, miningY, miningZ);
                }
            }
            else
            {
                mining = false;
            }
        }
    }

    public void OnBlockBreakingAction(int x, int y, int z, int direction)
    {
        world.extinguishFire(null, x, y, z, direction);
        failedMiningStartTime = tickCounter;
        int blockId = world.getBlockId(x, y, z);
        if (blockId > 0)
        {
            Block.Blocks[blockId].onBlockBreakStart(world, x, y, z, Player);
        }

        if (blockId > 0 && Block.Blocks[blockId].getHardness(Player) >= 1.0F)
        {
            TryBreakBlock(x, y, z);
        }
        else
        {
            failedMiningX = x;
            failedMiningY = y;
            failedMiningZ = z;
        }
    }

    public void ContinueMining(int x, int y, int z)
    {
        if (x == failedMiningX && y == failedMiningY && z == failedMiningZ)
        {
            int ticksSinceFailedStart = tickCounter - failedMiningStartTime;
            int blockId = world.getBlockId(x, y, z);
            if (blockId != 0)
            {
                Block block = Block.Blocks[blockId];
                float breakProgress = block.getHardness(Player) * (ticksSinceFailedStart + 1);
                if (breakProgress >= 0.7F)
                {
                    TryBreakBlock(x, y, z);
                }
                else if (!mining)
                {
                    mining = true;
                    miningX = x;
                    miningY = y;
                    miningZ = z;
                    startMiningTime = failedMiningStartTime;
                }
            }
        }
    }

    public bool FinishMining(int x, int y, int z)
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

    public bool TryBreakBlock(int x, int y, int z)
    {
        int blockId = world.getBlockId(x, y, z);
        int blockMeta = world.getBlockMeta(x, y, z);
        world.worldEvent(Player, 2001, x, y, z, blockId + world.getBlockMeta(x, y, z) * 256);
        bool success = FinishMining(x, y, z);

        if (success && Player.canHarvest(Block.Blocks[blockId]))
        {
            Block.Blocks[blockId].afterBreak(world, Player, x, y, z, blockMeta);
            ((ServerPlayerEntity)Player).networkHandler.SendPacket(new BlockUpdateS2CPacket(x, y, z, world));
        }

        ItemStack itemStack = Player.getHand();
        if (itemStack != null)
        {
            itemStack.postMine(blockId, x, y, z, Player);
            if (itemStack.count == 0)
            {
                itemStack.onRemoved(Player);
                Player.clearStackInHand();
            }
        }

        return success;
    }

    public bool InteractItem(EntityPlayer player, World world, ItemStack stack)
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

            return true;
        }
        else
        {
            return false;
        }
    }

    public bool InteractBlock(EntityPlayer player, World world, ItemStack stack, int x, int y, int z, int side)
    {
        int blockId = world.getBlockId(x, y, z);
        if (blockId > 0 && Block.Blocks[blockId].onUse(world, x, y, z, player))
        {
            return true;
        }
        else
        {
            return stack == null ? false : stack.useOnBlock(player, world, x, y, z, side);
        }
    }
}
