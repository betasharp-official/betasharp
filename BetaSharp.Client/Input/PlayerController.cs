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

    public PlayerController(BetaSharp var1)
    {
        Game = var1;
    }

    public virtual void func_717_a(World var1)
    {
    }

    public virtual void clickBlock(int var1, int var2, int var3, int var4)
    {
        Game.world.extinguishFire(Game.player, var1, var2, var3, var4);
        sendBlockRemoved(var1, var2, var3, var4);
    }

    public virtual bool sendBlockRemoved(int var1, int var2, int var3, int var4)
    {
        World var5 = Game.world;
        Block var6 = Block.Blocks[var5.getBlockId(var1, var2, var3)];
        var5.worldEvent(2001, var1, var2, var3, var6.id + var5.getBlockMeta(var1, var2, var3) * 256);
        int var7 = var5.getBlockMeta(var1, var2, var3);
        bool var8 = var5.setBlock(var1, var2, var3, 0);
        if (var6 != null && var8)
        {
            var6.onMetadataChange(var5, var1, var2, var3, var7);
        }

        return var8;
    }

    public virtual void sendBlockRemoving(int var1, int var2, int var3, int var4)
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
        int var4 = var3.count;
        ItemStack var5 = var3.use(var2, var1);
        if (var5 != var3 || var5 != null && var5.count != var4)
        {
            var1.inventory.main[var1.inventory.selectedSlot] = var5;
            if (var5.count == 0)
            {
                var1.inventory.main[var1.inventory.selectedSlot] = null;
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

    public virtual bool sendPlaceBlock(EntityPlayer var1, World var2, ItemStack var3, int var4, int var5, int var6, int var7)
    {
        int var8 = var2.getBlockId(var4, var5, var6);
        return var8 > 0 && Block.Blocks[var8].onUse(var2, var4, var5, var6, var1) ? true : (var3 == null ? false : var3.useOnBlock(var1, var2, var4, var5, var6, var7));
    }

    public virtual EntityPlayer createPlayer(World var1)
    {
        return new ClientPlayerEntity(Game, var1, Game.session, var1.dimension.Id);
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

    public virtual void func_20086_a(int var1, EntityPlayer var2)
    {
        var2.currentScreenHandler.onClosed(var2);
        var2.currentScreenHandler = var2.playerScreenHandler;
    }
}
