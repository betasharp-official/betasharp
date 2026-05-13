using BetaSharp.Blocks;
using BetaSharp.Client.Entities;
using BetaSharp.Entities;
using BetaSharp.Items;
using BetaSharp.Worlds.Core;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Client.Input;

public class PlayerController
{
    protected readonly BetaSharp Game;
    public bool IsTestPlayer = false;

    public PlayerController(BetaSharp game)
    {
        Game = game;
    }

    public virtual void ChangeWorld(World world) { }

    public virtual void ClickBlock(int x, int y, int z, int direction)
    {
        Game.World.ExtinguishFire(Game.Player, x, y, z, direction);
        SendBlockRemoved(x, y, z, direction);
    }

    public virtual bool SendBlockRemoved(int x, int y, int z, int direction)
    {
        World world = Game.World;
        Block block = Block.Blocks[world.Reader.GetBlockId(x, y, z)];
        world.Broadcaster.NotifyNeighbors(x, y, z, world.Reader.GetBlockId(x, y, z));
        int blockMeta = world.Reader.GetBlockMeta(x, y, z);
        bool success = world.Writer.SetBlock(x, y, z, 0);
        if (block != null && success)
        {
            block.onMetadataChange(new OnMetadataChangeEvent(world, x, y, z, blockMeta));
        }

        return success;
    }

    public virtual void SendBlockRemoving(int x, int y, int z, int direction) { }

    public virtual void ResetBlockRemoving() { }

    public virtual void SetPartialTime(float tickDelta) { }

    public virtual float GetBlockReachDistance() => 5.0F;

    public virtual bool SendUseItem(EntityPlayer player, World world, ItemStack stack)
    {
        int originalCount = stack.Count;
        ItemStack resultStack = stack.use(world, player);
        if (resultStack != stack || resultStack != null && resultStack.Count != originalCount)
        {
            player.Inventory.Main[player.Inventory.SelectedSlot] = resultStack;
            if (resultStack.Count == 0)
            {
                player.Inventory.Main[player.Inventory.SelectedSlot] = null;
            }

            return true;
        }
        else
        {
            return false;
        }
    }

    public virtual void FlipPlayer(EntityPlayer playerEntity) { }

    public virtual void UpdateController() { }

    public virtual bool ShouldDrawHUD() => true;

    public virtual void FillHotbar(EntityPlayer player) { }

    public virtual bool SendPlaceBlock(
        ClientPlayerEntity player,
        IWorldContext world,
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
            bool used = Block.Blocks[targetId].onUse(new OnUseEvent(world, player, blockX, blockY, blockZ));
            if (used) return true;
        }

        if (selectedItem == null || !player.GameMode.CanPlace) return false;

        return selectedItem.useOnBlock(player, world, blockX, blockY, blockZ, blockSide);
    }

    public virtual EntityPlayer CreatePlayer(World world) =>
        new ClientPlayerEntity(Game, world, Game.Session, world.Dimension.Id);

    public virtual void InteractWithEntity(EntityPlayer player, Entity target) =>
        player.Interact(target);

    public virtual void AttackEntity(EntityPlayer player, Entity target) =>
        player.Attack(target);

    public virtual ItemStack OnSlotClick(int windowId, int slotIndex, int mouseButton, bool shiftClick, EntityPlayer player)
    {
        return player.CurrentScreenHandler.onSlotClick(slotIndex, mouseButton, shiftClick, player);
    }

    public virtual void OnGuiClosed(int windowId, EntityPlayer player)
    {
        player.CurrentScreenHandler.onClosed(player);
        player.CurrentScreenHandler = player.PlayerScreenHandler;
    }
}
