using BetaSharp.Blocks;
using BetaSharp.Client.Entities;
using BetaSharp.Entities;
using BetaSharp.Items;
using BetaSharp.Worlds.Core;

namespace BetaSharp.Client.Input;

public class PlayerController
{
    protected readonly BetaSharp Game;
    public bool IsTestPlayer = false;

    public PlayerController(BetaSharp var1)
    {
        Game = var1;
    }

    public virtual void ChangeWorld(World world) { }

    public virtual void clickBlock(int x, int y, int z, int direction)
    {
        Game.World.ExtinguishFire(Game.Player, x, y, z, direction);
        sendBlockRemoved(x, y, z, direction);
    }

    public virtual bool sendBlockRemoved(int x, int y, int z, int direction)
    {
        World world = Game.World;
        Block block = Block.Blocks[world.Reader.GetBlockId(x, y, z)];
        world.Broadcaster.NotifyNeighbors(x, y, z, world.Reader.GetBlockId(x, y, z));
        int blockMeta = world.Reader.GetBlockMeta(x, y, z);
        bool success = world.Writer.SetBlock(x, y, z, 0);
        if (block != null && success)
        {
            block.OnMetadataChange(new OnMetadataChangeEvent(world, x, y, z, blockMeta));
        }

        return success;
    }

    public virtual void sendBlockRemoving(int x, int y, int z, int direction)
    {
    }

    public virtual void resetBlockRemoving()
    {
    }

    public virtual void setPartialTime(float var1)
    {
    }

    public virtual float getBlockReachDistance()
    {
        return 5.0F;
    }

    public virtual bool sendUseItem(EntityPlayer var1, World var2, ItemStack var3)
    {
        int var4 = var3.Count;
        ItemStack var5 = var3.use(var2, var1);
        if (var5 != var3 || var5 != null && var5.Count != var4)
        {
            var1.inventory.Main[var1.inventory.SelectedSlot] = var5;
            if (var5.Count == 0)
            {
                var1.inventory.Main[var1.inventory.SelectedSlot] = null;
            }

            return true;
        }
        else
        {
            return false;
        }
    }

    public virtual void flipPlayer(EntityPlayer playerEntity)
    {
    }

    public virtual void updateController()
    {
    }

    public virtual bool shouldDrawHUD()
    {
        return true;
    }

    public virtual void fillHotbar(EntityPlayer var1)
    {
    }

    public virtual bool sendPlaceBlock(
        ClientPlayerEntity player,
        World world,
        ItemStack selectedItem,
        int blockX,
        int blockY,
        int blockZ,
        int blockSide
    )
    {
        int targetId = world.Reader.GetBlockId(blockX, blockY, blockZ);

        if (targetId > 0 && !player.IsSneaking())
        {
            if (!player.GameMode.CanInteract) return false;
            bool used = Block.Blocks[targetId].OnUse(new OnUseEvent(world, player, blockX, blockY, blockZ));
            if (used) return true;
        }

        if (selectedItem == null || !player.GameMode.CanPlace) return false;

        return selectedItem.useOnBlock(player, world, blockX, blockY, blockZ, blockSide);
    }

    public virtual EntityPlayer createPlayer(World var1)
    {
        return new ClientPlayerEntity(Game, var1, Game.Session, var1.Dimension.Id);
    }

    public virtual void interactWithEntity(EntityPlayer var1, Entity var2)
    {
        var1.interact(var2);
    }

    public virtual void attackEntity(EntityPlayer var1, Entity var2)
    {
        var1.attack(var2);
    }

    public virtual ItemStack func_27174_a(int var1, int var2, int var3, bool var4, EntityPlayer var5)
    {
        return var5.currentScreenHandler.onSlotClick(var2, var3, var4, var5);
    }

    public virtual void OnGuiClosed(int var1, EntityPlayer var2)
    {
        var2.currentScreenHandler.onClosed(var2);
        var2.currentScreenHandler = var2.playerScreenHandler;
    }
}
