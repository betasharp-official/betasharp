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
            InventoryPlayer playerInventory = player.Inventory;
            if (index == -999)
            {
                if (playerInventory.GetCursorStack() is not null && index == -999)
                {
                    if (button == 0)
                    {
                        if (player.DropItem(playerInventory.GetCursorStack()))
                        {
                            playerInventory.SetCursorStack(null);
                        }
                    }

                    if (button == 1 && player.GameMode.CanDrop)
                    {
                        player.DropItem(playerInventory.GetCursorStack().Split(1));
                        if (playerInventory.GetCursorStack().Count == 0)
                        {
                            playerInventory.SetCursorStack(null);
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
                        int itemStackSize = itemStack.Count;
                        returnStack = itemStack.copy();
                        Slot slot = Slots[index];
                        if (slot is not null && slot.getStack() is not null)
                        {
                            slotItemStackSize = slot.getStack().Count;
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
                        ItemStack cursorStack = playerInventory.GetCursorStack();
                        if (slotStack is not null)
                        {
                            returnStack = slotStack.copy();
                        }

                        if (slotStack is null)
                        {
                            if (cursorStack is not null && slot.canInsert(cursorStack))
                            {
                                slotItemStackSize = button == 0 ? cursorStack.Count : 1;
                                if (slotItemStackSize > slot.getMaxItemCount())
                                {
                                    slotItemStackSize = slot.getMaxItemCount();
                                }

                                slot.setStack(cursorStack.Split(slotItemStackSize));
                                if (cursorStack.Count == 0)
                                {
                                    playerInventory.SetCursorStack(null);
                                }
                            }
                        }
                        else if (cursorStack is null)
                        {
                            slotItemStackSize = button == 0 ? slotStack.Count : (slotStack.Count + 1) / 2;
                            ItemStack takenStack = slot.takeStack(slotItemStackSize);
                            playerInventory.SetCursorStack(takenStack);
                            if (slotStack.Count == 0)
                            {
                                slot.setStack(null);
                            }

                            slot.onTakeItem(playerInventory.GetCursorStack());
                        }
                        else if (slot.canInsert(cursorStack))
                        {
                            if (slotStack.ItemId != cursorStack.ItemId || slotStack.getHasSubtypes() && slotStack.getDamage() != cursorStack.getDamage())
                            {
                                if (cursorStack.Count <= slot.getMaxItemCount())
                                {
                                    slot.setStack(cursorStack);
                                    playerInventory.SetCursorStack(slotStack);
                                }
                            }
                            else
                            {
                                slotItemStackSize = button == 0 ? cursorStack.Count : 1;
                                if (slotItemStackSize > slot.getMaxItemCount() - slotStack.Count)
                                {
                                    slotItemStackSize = slot.getMaxItemCount() - slotStack.Count;
                                }

                                if (slotItemStackSize > cursorStack.getMaxCount() - slotStack.Count)
                                {
                                    slotItemStackSize = cursorStack.getMaxCount() - slotStack.Count;
                                }

                                cursorStack.Split(slotItemStackSize);
                                if (cursorStack.Count == 0)
                                {
                                    playerInventory.SetCursorStack(null);
                                }

                                slotStack.Count += slotItemStackSize;
                            }
                        }
                        else if (slotStack.ItemId == cursorStack.ItemId && cursorStack.getMaxCount() > 1 && (!slotStack.getHasSubtypes() || slotStack.getDamage() == cursorStack.getDamage()))
                        {
                            slotItemStackSize = slotStack.Count;
                            if (slotItemStackSize > 0 && slotItemStackSize + cursorStack.Count <= cursorStack.getMaxCount())
                            {
                                cursorStack.Count += slotItemStackSize;
                                slotStack.Split(slotItemStackSize);
                                if (slotStack.Count == 0)
                                {
                                    slot.setStack(null);
                                }

                                slot.onTakeItem(playerInventory.GetCursorStack());
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
        InventoryPlayer playerInventory = player.Inventory;
        if (playerInventory.GetCursorStack() is not null)
        {
            if (player.GameMode.CanDrop)
            {
                if (player.DropItem(playerInventory.GetCursorStack()))
                {
                    playerInventory.SetCursorStack(null);
                }
            }
            else
            {
                player.Inventory.AddItemStackToInventoryOrDrop(playerInventory.GetCursorStack());
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

    public static void onAcknowledgementAccepted(short actionType)
    {
    }

    public static void onAcknowledgementDenied(short actionType)
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
            while (stack.Count > 0 && (!fromLast && slotIndex < end || fromLast && slotIndex >= start))
            {
                slotToInsertStack = Slots[slotIndex];
                itemStackToInsert = slotToInsertStack.getStack();
                if (itemStackToInsert is not null && itemStackToInsert.ItemId == stack.ItemId && (!stack.getHasSubtypes() || stack.getDamage() == itemStackToInsert.getDamage()))
                {
                    int newitemStackSize = itemStackToInsert.Count + stack.Count;
                    if (newitemStackSize <= stack.getMaxCount())
                    {
                        stack.Count = 0;
                        itemStackToInsert.Count = newitemStackSize;
                        slotToInsertStack.markDirty();
                    }
                    else if (itemStackToInsert.Count < stack.getMaxCount())
                    {
                        stack.Count -= stack.getMaxCount() - itemStackToInsert.Count;
                        itemStackToInsert.Count = stack.getMaxCount();
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

        if (stack.Count > 0)
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
                    stack.Count = 0;
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
