using BetaSharp.Entities;
using BetaSharp.Inventorys;
using BetaSharp.Items;
using BetaSharp.Screens.Slots;

namespace BetaSharp.Screens;

public abstract class ScreenHandler
{
    protected List<ScreenHandlerListener> Listeners { get; private set; } = [];
    private short _revision;
    private HashSet<EntityPlayer> _players = new HashSet<EntityPlayer>();



    public List<ItemStack?> TrackedStacks { get; private set; } = [];
    public List<Slot> Slots { get; private set; } = [];
    public int SyncId { get; set; } = 0;


    protected void AddSlot(Slot slot)
    {
        slot.id = Slots.Count;
        Slots.Add(slot);
        TrackedStacks.Add(null);
    }

    public virtual void AddListener(ScreenHandlerListener listener)
    {
        if (Listeners.Contains(listener))
        {
            throw new ArgumentException("Listener already listening", nameof(listener));
        }
        else
        {
            Listeners.Add(listener);
            listener.onContentsUpdate(this, GetStacks());
            SendContentUpdates();
        }
    }

    public List<ItemStack> GetStacks()
    {
        List<ItemStack> stacks = new List<ItemStack>();

        for (int slotIndex = 0; slotIndex < Slots.Count; slotIndex++)
        {
            stacks.Add(Slots[slotIndex].getStack());
        }
        return stacks;
    }

    public virtual void SendContentUpdates()
    {
        for (int slotIndex = 0; slotIndex < Slots.Count; ++slotIndex)
        {
            ItemStack slotStack = Slots[slotIndex].getStack();
            ItemStack? trackedStack = TrackedStacks[slotIndex];
            if (!ItemStack.areEqual(trackedStack, slotStack))
            {
                trackedStack = slotStack is null ? null : slotStack.copy();
                TrackedStacks[slotIndex] = trackedStack;

                for (int listenerIndex = 0; listenerIndex < Listeners.Count; ++listenerIndex)
                {
                    Listeners[listenerIndex].onSlotUpdate(this, slotIndex, trackedStack);
                }
            }
        }
    }

    public Slot? GetSlot(IInventory inventory, int index)
    {
        for (int slotIndex = 0; slotIndex < Slots.Count; slotIndex++)
        {
            Slot slot = Slots[slotIndex];
            if (slot.Equals(inventory, index))
            {
                return slot;
            }
        }
        return null;
    }

    public Slot GetSlot(int index)
    {
        return Slots[index];
    }

    public virtual ItemStack? quickMove(int index)
    {
        if (index < 0 || index > Slots.Count)
            return null;

        return Slots[index].getStack();
    }

    public ItemStack? onSlotClick(int index, int button, bool shift, EntityPlayer player)
    {
        ItemStack? returnStack = null;
        if (button == 0 || button == 1)
        {
            InventoryPlayer playerInventory = player.inventory;
            if (index == -999)
            {
                if (playerInventory.getCursorStack() is not null && index == -999)
                {
                    if (button == 0)
                    {
                        if (player.DropItem(playerInventory.getCursorStack()))
                        {
                            playerInventory.setItemStack(null);
                        }
                    }

                    if (button == 1 && player.GameMode.CanDrop)
                    {
                        player.DropItem(playerInventory.getCursorStack().split(1));
                        if (playerInventory.getCursorStack().count == 0)
                        {
                            playerInventory.setItemStack(null);
                        }
                    }
                }
            }
            else
            {
                int slotItemStackSize;
                if (shift)
                {
                    ItemStack? itemStack = quickMove(index);
                    if (itemStack is not null)
                    {
                        int itemStackSize = itemStack.count;
                        returnStack = itemStack.copy();
                        Slot slot = Slots[index];
                        if (slot is not null && slot.getStack() is not null)
                        {
                            slotItemStackSize = slot.getStack().count;
                            if (slotItemStackSize < itemStackSize)
                            {
                                onSlotClick(index, button, shift, player);
                            }
                        }
                    }
                }
                else
                {
                    Slot slot = Slots[index];
                    if (slot is not null)
                    {
                        slot.markDirty();
                        ItemStack slotStack = slot.getStack();
                        ItemStack cursorStack = playerInventory.getCursorStack();
                        if (slotStack is not null)
                        {
                            returnStack = slotStack.copy();
                        }

                        if (slotStack is null)
                        {
                            if (cursorStack is not null && slot.canInsert(cursorStack))
                            {
                                slotItemStackSize = button == 0 ? cursorStack.count : 1;
                                if (slotItemStackSize > slot.getMaxItemCount())
                                {
                                    slotItemStackSize = slot.getMaxItemCount();
                                }

                                slot.setStack(cursorStack.split(slotItemStackSize));
                                if (cursorStack.count == 0)
                                {
                                    playerInventory.setItemStack(null);
                                }
                            }
                        }
                        else if (cursorStack is null)
                        {
                            slotItemStackSize = button == 0 ? slotStack.count : (slotStack.count + 1) / 2;
                            ItemStack takenStack = slot.takeStack(slotItemStackSize);
                            playerInventory.setItemStack(takenStack);
                            if (slotStack.count == 0)
                            {
                                slot.setStack(null);
                            }

                            slot.onTakeItem(playerInventory.getCursorStack());
                        }
                        else if (slot.canInsert(cursorStack))
                        {
                            if (slotStack.itemId != cursorStack.itemId || slotStack.getHasSubtypes() && slotStack.getDamage() != cursorStack.getDamage())
                            {
                                if (cursorStack.count <= slot.getMaxItemCount())
                                {
                                    slot.setStack(cursorStack);
                                    playerInventory.setItemStack(slotStack);
                                }
                            }
                            else
                            {
                                slotItemStackSize = button == 0 ? cursorStack.count : 1;
                                if (slotItemStackSize > slot.getMaxItemCount() - slotStack.count)
                                {
                                    slotItemStackSize = slot.getMaxItemCount() - slotStack.count;
                                }

                                if (slotItemStackSize > cursorStack.getMaxCount() - slotStack.count)
                                {
                                    slotItemStackSize = cursorStack.getMaxCount() - slotStack.count;
                                }

                                cursorStack.split(slotItemStackSize);
                                if (cursorStack.count == 0)
                                {
                                    playerInventory.setItemStack(null);
                                }

                                slotStack.count += slotItemStackSize;
                            }
                        }
                        else if (slotStack.itemId == cursorStack.itemId && cursorStack.getMaxCount() > 1 && (!slotStack.getHasSubtypes() || slotStack.getDamage() == cursorStack.getDamage()))
                        {
                            slotItemStackSize = slotStack.count;
                            if (slotItemStackSize > 0 && slotItemStackSize + cursorStack.count <= cursorStack.getMaxCount())
                            {
                                cursorStack.count += slotItemStackSize;
                                slotStack.split(slotItemStackSize);
                                if (slotStack.count == 0)
                                {
                                    slot.setStack(null);
                                }

                                slot.onTakeItem(playerInventory.getCursorStack());
                            }
                        }
                    }
                }
            }
        }
        return returnStack;
    }

    public virtual void onClosed(EntityPlayer player)
    {
        InventoryPlayer playerInventory = player.inventory;
        if (playerInventory.getCursorStack() is not null)
        {
            if (player.GameMode.CanDrop)
            {
                if (player.DropItem(playerInventory.getCursorStack()))
                {
                    playerInventory.setItemStack(null);
                }
            }
            else
            {
                player.inventory.AddItemStackToInventoryOrDrop(playerInventory.getCursorStack());
            }
        }

    }

    public virtual void onSlotUpdate(IInventory inventory)
    {
        SendContentUpdates();
    }

    public void setStackInSlot(int index, ItemStack stack)
    {
        GetSlot(index).setStack(stack);
    }

    public void updateSlotStacks(ItemStack[] stacks)
    {
        for (int index = 0; index < stacks.Length; ++index)
        {
            GetSlot(index).setStack(stacks[index]);
        }

    }

    public virtual void setProperty(int id, int value)
    {
    }

    public short nextRevision(InventoryPlayer inventory)
    {
        ++_revision;
        return _revision;
    }

    public void onAcknowledgementAccepted(short actionType)
    {
    }

    public void onAcknowledgementDenied(short actionType)
    {
    }

    public bool canOpen(EntityPlayer player)
    {
        return !_players.Contains(player);
    }

    public void updatePlayerList(EntityPlayer player, bool remove)
    {
        if (remove)
        {
            _players.Remove(player);
        }
        else
        {
            _players.Add(player);
        }
    }

    public abstract bool canUse(EntityPlayer player);

    protected void insertItem(ItemStack stack, int start, int end, bool fromLast)
    {
        int slotIndex = start;
        if (fromLast)
        {
            slotIndex = end - 1;
        }

        Slot slotToInsertStack;
        ItemStack itemStackToInsert;
        if (stack.isStackable())
        {
            while (stack.count > 0 && (!fromLast && slotIndex < end || fromLast && slotIndex >= start))
            {
                slotToInsertStack = Slots[slotIndex];
                itemStackToInsert = slotToInsertStack.getStack();
                if (itemStackToInsert is not null && itemStackToInsert.itemId == stack.itemId && (!stack.getHasSubtypes() || stack.getDamage() == itemStackToInsert.getDamage()))
                {
                    int newitemStackSize = itemStackToInsert.count + stack.count;
                    if (newitemStackSize <= stack.getMaxCount())
                    {
                        stack.count = 0;
                        itemStackToInsert.count = newitemStackSize;
                        slotToInsertStack.markDirty();
                    }
                    else if (itemStackToInsert.count < stack.getMaxCount())
                    {
                        stack.count -= stack.getMaxCount() - itemStackToInsert.count;
                        itemStackToInsert.count = stack.getMaxCount();
                        slotToInsertStack.markDirty();
                    }
                }

                if (fromLast)
                {
                    --slotIndex;
                }
                else
                {
                    ++slotIndex;
                }
            }
        }

        if (stack.count > 0)
        {
            if (fromLast)
            {
                slotIndex = end - 1;
            }
            else
            {
                slotIndex = start;
            }

            while (!fromLast && slotIndex < end || fromLast && slotIndex >= start)
            {
                slotToInsertStack = Slots[slotIndex];
                itemStackToInsert = slotToInsertStack.getStack();
                if (itemStackToInsert is null)
                {
                    slotToInsertStack.setStack(stack.copy());
                    slotToInsertStack.markDirty();
                    stack.count = 0;
                    break;
                }

                if (fromLast)
                {
                    --slotIndex;
                }
                else
                {
                    ++slotIndex;
                }
            }
        }
    }
}
