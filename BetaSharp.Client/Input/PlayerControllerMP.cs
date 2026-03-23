using BetaSharp.Blocks;
using BetaSharp.Client.Network;
using BetaSharp.Client.Sound;
using BetaSharp.Entities;
using BetaSharp.Items;
using BetaSharp.Network.Packets.C2SPlay;
using BetaSharp.Worlds;

namespace BetaSharp.Client.Input;

public class PlayerControllerMP : PlayerController
{

    private int currentBlockX = -1;
    private int currentBlockY = -1;
    private int currentblockZ = -1;
    private float curBlockDamageMP;
    private float prevBlockDamageMP;
    private float field_9441_h;
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
        playerEntity.yaw = -180.0F;
        playerEntity.prevYaw = -180.0F;
    }

    public override bool sendBlockRemoved(int x, int y, int z, int var4)
    {
        int blockId = Game.world.getBlockId(x, y, z);
        bool var6 = base.sendBlockRemoved(x, y, z, var4);
        ItemStack var7 = Game.player.getHand();
        if (var7 != null)
        {
            var7.postMine(blockId, x, y, z, Game.player);
            if (var7.count == 0)
            {
                var7.onRemoved(Game.player);
                Game.player.clearStackInHand();
            }
        }

        return var6;
    }

    public override void clickBlock(int var1, int var2, int var3, int var4)
    {
        if (!isHittingBlock || var1 != currentBlockX || var2 != currentBlockY || var3 != currentblockZ)
        {
            netClientHandler.addToSendQueue(PlayerActionC2SPacket.Get(0, var1, var2, var3, var4));
            int var5 = Game.world.getBlockId(var1, var2, var3);
            if (var5 > 0 && curBlockDamageMP == 0.0F)
            {
                Block.Blocks[var5].onBlockBreakStart(Game.world, var1, var2, var3, Game.player);
            }

            if (var5 > 0 && Block.Blocks[var5].getHardness(Game.player) >= 1.0F)
            {
                sendBlockRemoved(var1, var2, var3, var4);
            }
            else
            {
                isHittingBlock = true;
                currentBlockX = var1;
                currentBlockY = var2;
                currentblockZ = var3;
                curBlockDamageMP = 0.0F;
                prevBlockDamageMP = 0.0F;
                field_9441_h = 0.0F;
            }
        }

    }

    public override void resetBlockRemoving()
    {
        curBlockDamageMP = 0.0F;
        isHittingBlock = false;
    }

    public override void sendBlockRemoving(int var1, int var2, int var3, int var4)
    {
        if (isHittingBlock)
        {
            syncCurrentPlayItem();
            if (blockHitDelay > 0)
            {
                --blockHitDelay;
            }
            else
            {
                if (var1 == currentBlockX && var2 == currentBlockY && var3 == currentblockZ)
                {
                    int var5 = Game.world.getBlockId(var1, var2, var3);
                    if (var5 == 0)
                    {
                        isHittingBlock = false;
                        return;
                    }

                    Block var6 = Block.Blocks[var5];
                    curBlockDamageMP += var6.getHardness(Game.player);
                    if (field_9441_h % 4.0F == 0.0F && var6 != null)
                    {
                        Game.sndManager.PlaySound(var6.soundGroup.StepSound, (float)var1 + 0.5F, (float)var2 + 0.5F, (float)var3 + 0.5F, (var6.soundGroup.Volume + 1.0F) / 8.0F, var6.soundGroup.Pitch * 0.5F);
                    }

                    ++field_9441_h;
                    if (curBlockDamageMP >= 1.0F)
                    {
                        isHittingBlock = false;
                        netClientHandler.addToSendQueue(PlayerActionC2SPacket.Get(2, var1, var2, var3, var4));
                        sendBlockRemoved(var1, var2, var3, var4);
                        curBlockDamageMP = 0.0F;
                        prevBlockDamageMP = 0.0F;
                        field_9441_h = 0.0F;
                        blockHitDelay = 5;
                    }
                }
                else
                {
                    clickBlock(var1, var2, var3, var4);
                }

            }
        }
    }

    public override void setPartialTime(float var1)
    {
        if (curBlockDamageMP <= 0.0F)
        {
            Game.ingameGUI._damageGuiPartialTime = 0.0F;
            Game.terrainRenderer.damagePartialTime = 0.0F;
        }
        else
        {
            float var2 = prevBlockDamageMP + (curBlockDamageMP - prevBlockDamageMP) * var1;
            Game.ingameGUI._damageGuiPartialTime = var2;
            Game.terrainRenderer.damagePartialTime = var2;
        }

    }

    public override float getBlockReachDistance()
    {
        return 4.0F;
    }

    public override void func_717_a(World var1)
    {
        base.func_717_a(var1);
    }

    public override void updateController()
    {
        syncCurrentPlayItem();
        prevBlockDamageMP = curBlockDamageMP;
        Game.sndManager.PlayRandomMusicIfReady(DefaultMusicCategories.Game);
    }

    private void syncCurrentPlayItem()
    {
        int var1 = Game.player.inventory.selectedSlot;
        if (var1 != currentPlayerItem)
        {
            currentPlayerItem = var1;
            netClientHandler.addToSendQueue(UpdateSelectedSlotC2SPacket.Get(currentPlayerItem));
        }

    }

    public override bool sendPlaceBlock(EntityPlayer var1, World var2, ItemStack var3, int var4, int var5, int var6, int var7)
    {
        syncCurrentPlayItem();
        netClientHandler.addToSendQueue(PlayerInteractBlockC2SPacket.Get(var4, var5, var6, var7, var1.inventory.getSelectedItem()));
        bool var8 = base.sendPlaceBlock(var1, var2, var3, var4, var5, var6, var7);
        return var8;
    }

    public override bool sendUseItem(EntityPlayer var1, World var2, ItemStack var3)
    {
        syncCurrentPlayItem();
        netClientHandler.addToSendQueue(PlayerInteractBlockC2SPacket.Get(-1, -1, -1, 255, var1.inventory.getSelectedItem()));
        bool var4 = base.sendUseItem(var1, var2, var3);
        return var4;
    }

    public override EntityPlayer createPlayer(World var1)
    {
        return new EntityClientPlayerMP(Game, var1, Game.session, netClientHandler);
    }

    public override void attackEntity(EntityPlayer var1, Entity var2)
    {
        syncCurrentPlayItem();
        netClientHandler.addToSendQueue(PlayerInteractEntityC2SPacket.Get(var1.id, var2.id, 1));
        var1.attack(var2);
    }

    public override void interactWithEntity(EntityPlayer var1, Entity var2)
    {
        syncCurrentPlayItem();
        netClientHandler.addToSendQueue(PlayerInteractEntityC2SPacket.Get(var1.id, var2.id, 0));
        var1.interact(var2);
    }

    public override ItemStack func_27174_a(int var1, int var2, int var3, bool var4, EntityPlayer var5)
    {
        short var6 = var5.currentScreenHandler.nextRevision(var5.inventory);
        ItemStack var7 = base.func_27174_a(var1, var2, var3, var4, var5);
        netClientHandler.addToSendQueue(ClickSlotC2SPacket.Get(var1, var2, var3, var4, var7, var6));
        return var7;
    }

    public override void func_20086_a(int var1, EntityPlayer var2)
    {
        if (var1 != -9999)
        {
        }
    }
}
