using BetaSharp.Entities;
using BetaSharp.Inventorys;
using BetaSharp.Items;
using BetaSharp.Screens.Slots;
using java.util;

namespace BetaSharp.Screens;

public abstract class ScreenHandler
{
    public List trackedStacks = new ArrayList();
    public List slots = new ArrayList();
    public int syncId = 0;
    private short revision;
    protected List listeners = new ArrayList();
    private Set players = new HashSet();

    protected void addSlot(Slot var1)
    {
        var1.id = slots.size();
        slots.add(var1);
        trackedStacks.add(null);
    }

    public virtual void addListener(ScreenHandlerListener listener)
    {
        if (listeners.contains(listener))
        {
            throw new ArgumentException("Listener already listening", nameof(listener));
        }
        else
        {
            listeners.add(listener);
            listener.onContentsUpdate(this, getStacks());
            sendContentUpdates();
        }
    }

    public List getStacks()
    {
        ArrayList var1 = new ArrayList();

        for (int var2 = 0; var2 < slots.size(); var2++)
        {
            var1.add(((Slot)slots.get(var2)).getStack());
        }

        return var1;
    }

    public virtual void sendContentUpdates()
    {
        for (int var1 = 0; var1 < slots.size(); ++var1)
        {
            ItemStack var2 = ((Slot)slots.get(var1)).getStack();
            ItemStack var3 = (ItemStack)trackedStacks.get(var1);
            if (!ItemStack.areEqual(var3, var2))
            {
                var3 = var2 == null ? null : var2.copy();
                trackedStacks.set(var1, var3);

                for (int var4 = 0; var4 < listeners.size(); ++var4)
                {
                    ((ScreenHandlerListener)listeners.get(var4)).onSlotUpdate(this, var1, var3);
                }
            }
        }

    }

    public Slot getSlot(IInventory inventory, int index)
    {
        for (int var3 = 0; var3 < slots.size(); var3++)
        {
            Slot var4 = (Slot)slots.get(var3);
            if (var4.Equals(inventory, index))
            {
                return var4;
            }
        }

        return null;
    }

    public Slot getSlot(int index)
    {
        return (Slot)slots.get(index);
    }

    public virtual ItemStack quickMove(int slot)
    {
        Slot var2 = (Slot)slots.get(slot);
        return var2 != null ? var2.getStack() : null;
    }

    public ItemStack onSlotClick(int index, int button, bool shift, EntityPlayer player)
    {
        ItemStack var5 = null;
        if (button == 0 || button == 1)
        {
            InventoryPlayer var6 = player.inventory;
            if (index == -999)
            {
                if (var6.getCursorStack() != null && index == -999)
                {
                    if (button == 0)
                    {
                        player.dropItem(var6.getCursorStack());
                        var6.setItemStack(null);
                    }

                    if (button == 1)
                    {
                        player.dropItem(var6.getCursorStack().split(1));
                        if (var6.getCursorStack().count == 0)
                        {
                            var6.setItemStack(null);
                        }
                    }
                }
            }
            else
            {
                int var10;
                if (shift)
                {
                    ItemStack var7 = quickMove(index);
                    if (var7 != null)
                    {
                        int var8 = var7.count;
                        var5 = var7.copy();
                        Slot var9 = (Slot)slots.get(index);
                        if (var9 != null && var9.getStack() != null)
                        {
                            var10 = var9.getStack().count;
                            if (var10 < var8)
                            {
                                onSlotClick(index, button, shift, player);
                            }
                        }
                    }
                }
                else
                {
                    Slot var12 = (Slot)slots.get(index);
                    if (var12 != null)
                    {
                        var12.MarkDirty();
                        ItemStack var13 = var12.getStack();
                        ItemStack var14 = var6.getCursorStack();
                        if (var13 != null)
                        {
                            var5 = var13.copy();
                        }

                        if (var13 == null)
                        {
                            if (var14 != null && var12.canInsert(var14))
                            {
                                var10 = button == 0 ? var14.count : 1;
                                if (var10 > var12.getMaxItemCount())
                                {
                                    var10 = var12.getMaxItemCount();
                                }

                                var12.setStack(var14.split(var10));
                                if (var14.count == 0)
                                {
                                    var6.setItemStack(null);
                                }
                            }
                        }
                        else if (var14 == null)
                        {
                            var10 = button == 0 ? var13.count : (var13.count + 1) / 2;
                            ItemStack var11 = var12.takeStack(var10);
                            var6.setItemStack(var11);
                            if (var13.count == 0)
                            {
                                var12.setStack(null);
                            }

                            var12.onTakeItem(var6.getCursorStack());
                        }
                        else if (var12.canInsert(var14))
                        {
                            if (var13.itemId != var14.itemId || var13.getHasSubtypes() && var13.getDamage() != var14.getDamage())
                            {
                                if (var14.count <= var12.getMaxItemCount())
                                {
                                    var12.setStack(var14);
                                    var6.setItemStack(var13);
                                }
                            }
                            else
                            {
                                var10 = button == 0 ? var14.count : 1;
                                if (var10 > var12.getMaxItemCount() - var13.count)
                                {
                                    var10 = var12.getMaxItemCount() - var13.count;
                                }

                                if (var10 > var14.getMaxCount() - var13.count)
                                {
                                    var10 = var14.getMaxCount() - var13.count;
                                }

                                var14.split(var10);
                                if (var14.count == 0)
                                {
                                    var6.setItemStack(null);
                                }

                                var13.count += var10;
                            }
                        }
                        else if (var13.itemId == var14.itemId && var14.getMaxCount() > 1 && (!var13.getHasSubtypes() || var13.getDamage() == var14.getDamage()))
                        {
                            var10 = var13.count;
                            if (var10 > 0 && var10 + var14.count <= var14.getMaxCount())
                            {
                                var14.count += var10;
                                var13.split(var10);
                                if (var13.count == 0)
                                {
                                    var12.setStack(null);
                                }

                                var12.onTakeItem(var6.getCursorStack());
                            }
                        }
                    }
                }
            }
        }

        return var5;
    }

    public virtual void onClosed(EntityPlayer player)
    {
        InventoryPlayer var2 = player.inventory;
        if (var2.getCursorStack() != null)
        {
            player.dropItem(var2.getCursorStack());
            var2.setItemStack(null);
        }

    }

    public virtual void onSlotUpdate(IInventory inventory)
    {
        sendContentUpdates();
    }

    public void setStackInSlot(int index, ItemStack stack)
    {
        getSlot(index).setStack(stack);
    }

    public void updateSlotStacks(ItemStack[] stacks)
    {
        for (int var2 = 0; var2 < stacks.Length; ++var2)
        {
            getSlot(var2).setStack(stacks[var2]);
        }

    }

    public virtual void setProperty(int id, int value)
    {
    }

    public short nextRevision(InventoryPlayer inventory)
    {
        ++revision;
        return revision;
    }

    public void onAcknowledgementAccepted(short actionType)
    {
    }

    public void onAcknowledgementDenied(short actionType)
    {
    }

    public bool canOpen(EntityPlayer player)
    {
        return !players.contains(player);
    }

    public void updatePlayerList(EntityPlayer player, bool remove)
    {
        if (remove)
        {
            players.remove(player);
        }
        else
        {
            players.add(player);
        }
    }

    public abstract bool canUse(EntityPlayer player);

    protected void insertItem(ItemStack stack, int start, int end, bool fromLast)
    {
        int var5 = start;
        if (fromLast)
        {
            var5 = end - 1;
        }

        Slot var6;
        ItemStack var7;
        if (stack.isStackable())
        {
            while (stack.count > 0 && (!fromLast && var5 < end || fromLast && var5 >= start))
            {
                var6 = (Slot)slots.get(var5);
                var7 = var6.getStack();
                if (var7 != null && var7.itemId == stack.itemId && (!stack.getHasSubtypes() || stack.getDamage() == var7.getDamage()))
                {
                    int var8 = var7.count + stack.count;
                    if (var8 <= stack.getMaxCount())
                    {
                        stack.count = 0;
                        var7.count = var8;
                        var6.MarkDirty();
                    }
                    else if (var7.count < stack.getMaxCount())
                    {
                        stack.count -= stack.getMaxCount() - var7.count;
                        var7.count = stack.getMaxCount();
                        var6.MarkDirty();
                    }
                }

                if (fromLast)
                {
                    --var5;
                }
                else
                {
                    ++var5;
                }
            }
        }

        if (stack.count > 0)
        {
            if (fromLast)
            {
                var5 = end - 1;
            }
            else
            {
                var5 = start;
            }

            while (!fromLast && var5 < end || fromLast && var5 >= start)
            {
                var6 = (Slot)slots.get(var5);
                var7 = var6.getStack();
                if (var7 == null)
                {
                    var6.setStack(stack.copy());
                    var6.MarkDirty();
                    stack.count = 0;
                    break;
                }

                if (fromLast)
                {
                    --var5;
                }
                else
                {
                    ++var5;
                }
            }
        }

    }
}
