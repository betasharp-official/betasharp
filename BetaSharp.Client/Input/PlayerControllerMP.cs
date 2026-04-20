using BetaSharp.Blocks;
using BetaSharp.Client.Entities;
using BetaSharp.Client.Network;
using BetaSharp.Client.Sound;
using BetaSharp.Entities;
using BetaSharp.Items;
using BetaSharp.Network.Packets.C2SPlay;
using BetaSharp.Worlds.Core;
using BetaSharp.Worlds.Core.Systems;

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

    public PlayerControllerMP(BetaSharp var1, ClientNetworkHandler var2) : base(var1)
    {
        netClientHandler = var2;
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
        ItemStack hand = Game.Player.getHand();
        if (hand != null)
        {
            hand.postMine(blockId, x, y, z, Game.Player);
            if (hand.Count == 0)
            {
                ItemStack.onRemoved(Game.Player);
                Game.Player.clearStackInHand();
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
                Block.Blocks[blockId].OnBlockBreakStart(new OnBlockBreakStartEvent(Game.World, Game.Player, x, y, z));
            }

            if (!Game.Player.GameMode.CanBreak) return;

            if (blockId > 0 && Block.Blocks[blockId].GetHardness(Game.Player) >= Game.Player.GameMode.BrakeSpeed)
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
                    int var5 = Game.World.Reader.GetBlockId(x, y, z);
                    if (var5 == 0)
                    {
                        isHittingBlock = false;
                        return;
                    }

                    Block var6 = Block.Blocks[var5];
                    curBlockDamageMP += var6.GetHardness(Game.Player);
                    if (_mineSoundTimer % 4 == 0 && var6 != null)
                    {
                        Game.SoundManager.PlaySound(var6.SoundGroup.StepSound, (float)x + 0.5F, (float)y + 0.5F, (float)z + 0.5F, (var6.SoundGroup.Volume + 1.0F) / 8.0F, var6.SoundGroup.Pitch * 0.5F);
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

    public override void setPartialTime(float var1)
    {
        if (curBlockDamageMP <= 0.0F)
        {
            Game.WorldRenderer.DamagePartialTime = 0.0F;
        }
        else
        {
            float var2 = prevBlockDamageMP + (curBlockDamageMP - prevBlockDamageMP) * var1;
            Game.WorldRenderer.DamagePartialTime = var2;
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
        int var1 = Game.Player.inventory.SelectedSlot;
        if (var1 != currentPlayerItem)
        {
            currentPlayerItem = var1;
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
        netClientHandler.AddToSendQueue(PlayerInteractBlockC2SPacket.Get(blockX, blockY, blockZ, blockSide, player.inventory.GetItemInHand()));
        bool placed = base.sendPlaceBlock(player, world, selectedItem, blockX, blockY, blockZ, blockSide);
        return placed;
    }

    public override bool sendUseItem(EntityPlayer var1, World var2, ItemStack var3)
    {
        syncCurrentPlayItem();
        netClientHandler.AddToSendQueue(PlayerInteractBlockC2SPacket.Get(-1, -1, -1, 255, var1.inventory.GetItemInHand()));
        bool var4 = base.sendUseItem(var1, var2, var3);
        return var4;
    }

    public override EntityPlayer createPlayer(World var1)
    {
        return new EntityClientPlayerMP(Game, var1, Game.Session, netClientHandler);
    }

    public override void attackEntity(EntityPlayer var1, Entity var2)
    {
        syncCurrentPlayItem();
        netClientHandler.AddToSendQueue(PlayerInteractEntityC2SPacket.Get(var1.ID, var2.ID, 1));
        var1.attack(var2);
    }

    public override void interactWithEntity(EntityPlayer var1, Entity var2)
    {
        syncCurrentPlayItem();
        netClientHandler.AddToSendQueue(PlayerInteractEntityC2SPacket.Get(var1.ID, var2.ID, 0));
        var1.interact(var2);
    }

    public override ItemStack func_27174_a(int var1, int var2, int var3, bool var4, EntityPlayer var5)
    {
        short var6 = var5.currentScreenHandler.nextRevision(var5.inventory);
        ItemStack var7 = base.func_27174_a(var1, var2, var3, var4, var5);
        netClientHandler.AddToSendQueue(ClickSlotC2SPacket.Get(var1, var2, var3, var4, var7, var6));
        return var7;
    }

    public override void OnGuiClosed(int var1, EntityPlayer var2)
    {
        if (var1 != -9999)
        {
        }
    }
}
