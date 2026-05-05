using BetaSharp.Blocks;
using BetaSharp.Client.Entities;
using BetaSharp.Client.Network;
using BetaSharp.Client.Sound;
using BetaSharp.Entities;
using BetaSharp.Items;
using BetaSharp.Network.Packets.C2SPlay;
using BetaSharp.Worlds.Core;

namespace BetaSharp.Client.Input;

public class PlayerControllerMP : PlayerController
{

    private int currentBlockX = -1;
    private int currentBlockY = -1;
    private int currentblockZ = -1;
    private float curBlockDamageMP;
    private float prevBlockDamageMP;
    private byte _mineSoundTimer;
    private int blockHitDelay;
    private bool isHittingBlock;
    private readonly ClientNetworkHandler netClientHandler;
    private int currentPlayerItem;

    public PlayerControllerMP(BetaSharp game, ClientNetworkHandler networkHandler) : base(game)
    {
        netClientHandler = networkHandler;
    }

    public override void flipPlayer(EntityPlayer playerEntity)
    {
        playerEntity.Yaw = -180.0F;
        playerEntity.PrevYaw = -180.0F;
    }

    public override bool sendBlockRemoved(int x, int y, int z, int direction)
    {
        if (!Game.Player.GameMode.CanBreak) return false;

        int blockId = Game.World.Reader.GetBlockId(x, y, z);
        bool blockRemoved = base.sendBlockRemoved(x, y, z, direction);
        ItemStack hand = Game.Player.GetHand();
        if (hand != null)
        {
            hand.postMine(blockId, x, y, z, Game.Player);
            if (hand.Count == 0)
            {
                ItemStack.onRemoved(Game.Player);
                Game.Player.ClearStackInHand();
            }
        }

        return blockRemoved;
    }

    public override void clickBlock(int x, int y, int z, int direction)
    {
        if (!isHittingBlock || x != currentBlockX || y != currentBlockY || z != currentblockZ)
        {
            netClientHandler.AddToSendQueue(PlayerActionC2SPacket.Get(0, x, y, z, direction));
            int blockId = Game.World.Reader.GetBlockId(x, y, z);
            if (blockId > 0 && curBlockDamageMP == 0.0F && Game.Player.GameMode.CanInteract)
            {
                Block.Blocks[blockId].onBlockBreakStart(new OnBlockBreakStartEvent(Game.World, Game.Player, x, y, z));
            }

            if (!Game.Player.GameMode.CanBreak) return;

            if (blockId > 0 && Block.Blocks[blockId].getHardness(Game.Player) >= Game.Player.GameMode.BrakeSpeed)
            {
                sendBlockRemoved(x, y, z, direction);
            }
            else
            {
                isHittingBlock = true;
                currentBlockX = x;
                currentBlockY = y;
                currentblockZ = z;
                curBlockDamageMP = 0.0F;
                prevBlockDamageMP = 0.0F;
                _mineSoundTimer = 0;
            }
        }
    }

    public override void resetBlockRemoving()
    {
        curBlockDamageMP = 0.0F;
        isHittingBlock = false;
    }

    public override void sendBlockRemoving(int x, int y, int z, int direction)
    {
        if (!Game.Player.GameMode.CanBreak) return;
        if (isHittingBlock)
        {
            syncCurrentPlayItem();
            if (blockHitDelay > 0)
            {
                --blockHitDelay;
            }
            else
            {
                if (x == currentBlockX && y == currentBlockY && z == currentblockZ)
                {
                    int blockId = Game.World.Reader.GetBlockId(x, y, z);
                    if (blockId == 0)
                    {
                        isHittingBlock = false;
                        return;
                    }

                    Block block = Block.Blocks[blockId];
                    curBlockDamageMP += block.getHardness(Game.Player);
                    if (_mineSoundTimer % 4 == 0 && block != null)
                    {
                        Game.SoundManager.PlaySound(block.SoundGroup.StepSound, (float)x + 0.5F, (float)y + 0.5F, (float)z + 0.5F, (block.SoundGroup.Volume + 1.0F) / 8.0F, block.SoundGroup.Pitch * 0.5F);
                    }

                    ++_mineSoundTimer;
                    if (curBlockDamageMP >= 1.0F)
                    {
                        isHittingBlock = false;
                        netClientHandler.AddToSendQueue(PlayerActionC2SPacket.Get(2, x, y, z, direction));
                        sendBlockRemoved(x, y, z, direction);
                        curBlockDamageMP = 0.0F;
                        prevBlockDamageMP = 0.0F;
                        _mineSoundTimer = 0;
                        blockHitDelay = 5;
                    }
                }
                else
                {
                    clickBlock(x, y, z, direction);
                }

            }
        }
    }

    public override void setPartialTime(float tickDelta)
    {
        if (curBlockDamageMP <= 0.0F)
        {
            Game.WorldRenderer.DamagePartialTime = 0.0F;
        }
        else
        {
            float partialDamage = prevBlockDamageMP + (curBlockDamageMP - prevBlockDamageMP) * tickDelta;
            Game.WorldRenderer.DamagePartialTime = partialDamage;
        }

    }

    public override float getBlockReachDistance()
    {
        return 4.0F;
    }

    public override void updateController()
    {
        syncCurrentPlayItem();
        prevBlockDamageMP = curBlockDamageMP;
        Game.SoundManager.PlayRandomMusicIfReady(DefaultMusicCategories.Game);
    }

    private void syncCurrentPlayItem()
    {
        int selectedSlot = Game.Player.Inventory.SelectedSlot;
        if (selectedSlot != currentPlayerItem)
        {
            currentPlayerItem = selectedSlot;
            netClientHandler.AddToSendQueue(UpdateSelectedSlotC2SPacket.Get(currentPlayerItem));
        }

    }

    public override bool sendPlaceBlock(
        ClientPlayerEntity player,
        World world,
        ItemStack selectedItem,
        int blockX,
        int blockY,
        int blockZ,
        int blockSide
    )
    {
        syncCurrentPlayItem();
        netClientHandler.AddToSendQueue(PlayerInteractBlockC2SPacket.Get(blockX, blockY, blockZ, blockSide, player.Inventory.ItemInHand));
        bool placed = base.sendPlaceBlock(player, world, selectedItem, blockX, blockY, blockZ, blockSide);
        return placed;
    }

    public override bool sendUseItem(EntityPlayer player, World world, ItemStack stack)
    {
        syncCurrentPlayItem();
        netClientHandler.AddToSendQueue(PlayerInteractBlockC2SPacket.Get(-1, -1, -1, 255, player.Inventory.ItemInHand));
        bool usedItem = base.sendUseItem(player, world, stack);
        return usedItem;
    }

    public override EntityPlayer createPlayer(World world)
    {
        return new EntityClientPlayerMP(Game, world, Game.Session, netClientHandler);
    }

    public override void attackEntity(EntityPlayer player, Entity target)
    {
        syncCurrentPlayItem();
        netClientHandler.AddToSendQueue(PlayerInteractEntityC2SPacket.Get(player.ID, target.ID, 1));
        player.Attack(target);
    }

    public override void interactWithEntity(EntityPlayer player, Entity target)
    {
        syncCurrentPlayItem();
        netClientHandler.AddToSendQueue(PlayerInteractEntityC2SPacket.Get(player.ID, target.ID, 0));
        player.Interact(target);
    }

    public override ItemStack func_27174_a(int windowId, int slotIndex, int mouseButton, bool shiftClick, EntityPlayer player)
    {
        short revision = player.CurrentScreenHandler.nextRevision(player.Inventory);
        ItemStack resultStack = base.func_27174_a(windowId, slotIndex, mouseButton, shiftClick, player);
        netClientHandler.AddToSendQueue(ClickSlotC2SPacket.Get(windowId, slotIndex, mouseButton, shiftClick, resultStack, revision));
        return resultStack;
    }

    public override void OnGuiClosed(int windowId, EntityPlayer player)
    {
        if (windowId != -9999)
        {
        }
    }
}
