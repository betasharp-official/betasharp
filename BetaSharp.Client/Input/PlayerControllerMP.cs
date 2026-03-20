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

    public PlayerControllerMP(BetaSharp game, ClientNetworkHandler clientHandler) : base(game)
    {
        netClientHandler = clientHandler;
    }

    public override void flipPlayer(EntityPlayer player)
    {
        player.yaw = -180.0F;
    }

    public override bool sendBlockRemoved(int x, int y, int z, int face)
    {
        int blockId = Game.world.getBlockId(x, y, z);
        bool success = base.sendBlockRemoved(x, y, z, face);
        ItemStack heldItem = Game.player.getHand();
        if (heldItem != null)
        {
            heldItem.postMine(blockId, x, y, z, Game.player);
            if (heldItem.count == 0)
            {
                heldItem.onRemoved(Game.player);
                Game.player.clearStackInHand();
            }
        }

        return success;
    }

    public override void clickBlock(int x, int y, int z, int face)
    {
        if (!isHittingBlock || x != currentBlockX || y != currentBlockY || z != currentblockZ)
        {
            netClientHandler.addToSendQueue(PlayerActionC2SPacket.Get(0, x, y, z, face));
            int blockId = Game.world.getBlockId(x, y, z);
            if (blockId > 0 && curBlockDamageMP == 0.0F)
            {
                Block.Blocks[blockId].onBlockBreakStart(Game.world, x, y, z, Game.player);
            }

            if (blockId > 0 && Block.Blocks[blockId].getHardness(Game.player) >= 1.0F)
            {
                sendBlockRemoved(x, y, z, face);
            }
            else
            {
                isHittingBlock = true;
                currentBlockX = x;
                currentBlockY = y;
                currentblockZ = z;
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

    public override void sendBlockRemoving(int x, int y, int z, int face)
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
                if (x == currentBlockX && y == currentBlockY && z == currentblockZ)
                {
                    int blockId = Game.world.getBlockId(x, y, z);
                    if (blockId == 0)
                    {
                        isHittingBlock = false;
                        return;
                    }

                    Block block = Block.Blocks[blockId];
                    curBlockDamageMP += block.getHardness(Game.player);
                    if (field_9441_h % 4.0F == 0.0F && block != null)
                    {
                        Game.sndManager.PlaySound(block.soundGroup.StepSound, (float)x + 0.5F, (float)y + 0.5F, (float)z + 0.5F, (block.soundGroup.Volume + 1.0F) / 8.0F, block.soundGroup.Pitch * 0.5F);
                    }

                    ++field_9441_h;
                    if (curBlockDamageMP >= 1.0F)
                    {
                        isHittingBlock = false;
                        netClientHandler.addToSendQueue(PlayerActionC2SPacket.Get(2, x, y, z, face));
                        sendBlockRemoved(x, y, z, face);
                        curBlockDamageMP = 0.0F;
                        prevBlockDamageMP = 0.0F;
                        field_9441_h = 0.0F;
                        blockHitDelay = 5;
                    }
                }
                else
                {
                    clickBlock(x, y, z, face);
                }

            }
        }
    }

    public override void setPartialTime(float partialTicks)
    {
        if (curBlockDamageMP <= 0.0F)
        {
            Game.ingameGUI._damageGuiPartialTime = 0.0F;
            Game.terrainRenderer.damagePartialTime = 0.0F;
        }
        else
        {
            float interpolatedDamage = prevBlockDamageMP + (curBlockDamageMP - prevBlockDamageMP) * partialTicks;
            Game.ingameGUI._damageGuiPartialTime = interpolatedDamage;
            Game.terrainRenderer.damagePartialTime = interpolatedDamage;
        }

    }

    public override float getBlockReachDistance()
    {
        return 4.0F;
    }

    public override void func_717_a(World world)
    {
        base.func_717_a(world);
    }

    public override void updateController()
    {
        syncCurrentPlayItem();
        prevBlockDamageMP = curBlockDamageMP;
        Game.sndManager.PlayRandomMusicIfReady(DefaultMusicCategories.Game);
    }

    private void syncCurrentPlayItem()
    {
        int selectedSlot = Game.player.inventory.selectedSlot;
        if (selectedSlot != currentPlayerItem)
        {
            currentPlayerItem = selectedSlot;
            netClientHandler.addToSendQueue(UpdateSelectedSlotC2SPacket.Get(currentPlayerItem));
        }

    }

    public override bool sendPlaceBlock(EntityPlayer player, World world, ItemStack stack, int x, int y, int z, int face)
    {
        syncCurrentPlayItem();
        netClientHandler.addToSendQueue(PlayerInteractBlockC2SPacket.Get(x, y, z, face, player.inventory.getSelectedItem()));
        bool result = base.sendPlaceBlock(player, world, stack, x, y, z, face);
        return result;
    }

    public override bool sendUseItem(EntityPlayer player, World world, ItemStack stack)
    {
        syncCurrentPlayItem();
        netClientHandler.addToSendQueue(PlayerInteractBlockC2SPacket.Get(-1, -1, -1, 255, player.inventory.getSelectedItem()));
        bool result = base.sendUseItem(player, world, stack);
        return result;
    }

    public override EntityPlayer createPlayer(World world)
    {
        return new EntityClientPlayerMP(Game, world, Game.session, netClientHandler);
    }

    public override void attackEntity(EntityPlayer player, Entity target)
    {
        syncCurrentPlayItem();
        netClientHandler.addToSendQueue(PlayerInteractEntityC2SPacket.Get(player.id, target.id, 1));
        player.attack(target);
    }

    public override void interactWithEntity(EntityPlayer player, Entity entity)
    {
        syncCurrentPlayItem();
        netClientHandler.addToSendQueue(PlayerInteractEntityC2SPacket.Get(player.id, entity.id, 0));
        player.interact(entity);
    }

    public override ItemStack func_27174_a(int slotId, int button, int mode, bool isShift, EntityPlayer player)
    {
        short revision = player.currentScreenHandler.nextRevision(player.inventory);
        ItemStack result = base.func_27174_a(slotId, button, mode, isShift, player);
        netClientHandler.addToSendQueue(ClickSlotC2SPacket.Get(slotId, button, mode, isShift, result, revision));
        return result;
    }

    public override void func_20086_a(int slotId, EntityPlayer player)
    {
        if (slotId != -9999)
        {
        }
    }
}
