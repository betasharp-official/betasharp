using BetaSharp.Blocks;
using BetaSharp.Client.Entities;
using BetaSharp.Entities;
using BetaSharp.Items;
using BetaSharp.Worlds;

namespace BetaSharp.Client.Input;

public class PlayerController
{
    protected readonly BetaSharp Game;
    public bool IsTestPlayer = false;

    public PlayerController(BetaSharp game)
    {
        Game = game;
    }

    public virtual void func_717_a(World world)
    {
    }

    public virtual void clickBlock(int x, int y, int z, int side)
    {
        Game.world.extinguishFire(Game.player, x, y, z, side);
        sendBlockRemoved(x, y, z, side);
    }

    public virtual bool sendBlockRemoved(int x, int y, int z, int side)
    {
        World world = Game.world;
        Block block = Block.Blocks[world.getBlockId(x, y, z)];
        world.worldEvent(2001, x, y, z, block.id + world.getBlockMeta(x, y, z) * 256);
        int blockMetadata = world.getBlockMeta(x, y, z);
        bool blockRemovalSuccess = world.setBlock(x, y, z, 0);
        if (block != null && blockRemovalSuccess)
        {
            block.onMetadataChange(world, x, y, z, blockMetadata);
        }

        return blockRemovalSuccess;
    }

    public virtual void sendBlockRemoving(int x, int y, int z, int side)
    {
    }

    public virtual void resetBlockRemoving()
    {
    }

    public virtual void setPartialTime(float partialTime)
    {
    }

    public virtual float getBlockReachDistance()
    {
        return 5.0F;
    }

    public virtual bool sendUseItem(EntityPlayer player, World world, ItemStack itemStack)
    {
        int itemStackCount = itemStack.count;
        ItemStack resultItemStack = itemStack.use(world, player);
        if (resultItemStack != itemStack || resultItemStack != null && resultItemStack.count != itemStackCount)
        {
            player.inventory.main[player.inventory.selectedSlot] = resultItemStack;
            if (resultItemStack.count == 0)
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

    public virtual void flipPlayer(EntityPlayer player)
    {
    }

    public virtual void updateController()
    {
    }

    public virtual bool shouldDrawHUD()
    {
        return true;
    }

    public virtual void fillHotbar(EntityPlayer player)
    {
    }

    public virtual bool sendPlaceBlock(EntityPlayer player, World world, ItemStack itemStack, int x, int y, int z, int side)
    {
        int blockId = world.getBlockId(x, y, z);
        return blockId > 0 && Block.Blocks[blockId].onUse(world, x, y, z, player) ? true : (itemStack == null ? false : itemStack.useOnBlock(player, world, x, y, z, side));
    }

    public virtual EntityPlayer createPlayer(World world)
    {
        return new ClientPlayerEntity(Game, world, Game.session, world.dimension.Id);
    }

    public virtual void interactWithEntity(EntityPlayer player, Entity entity)
    {
        player.interact(entity);
    }

    public virtual void attackEntity(EntityPlayer player, Entity entity)
    {
        player.attack(entity);
    }

    public virtual ItemStack func_27174_a(int windowId, int slotId, int mouseButton, bool shiftClick, EntityPlayer player)
    {
        return player.currentScreenHandler.onSlotClick(slotId, mouseButton, shiftClick, player);
    }

    public virtual void func_20086_a(int windowId, EntityPlayer player)
    {
        player.currentScreenHandler.onClosed(player);
        player.currentScreenHandler = player.playerScreenHandler;
    }
}
