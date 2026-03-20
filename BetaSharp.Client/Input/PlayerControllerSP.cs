using BetaSharp.Blocks;
using BetaSharp.Client.Sound;
using BetaSharp.Entities;
using BetaSharp.Items;
using BetaSharp.Worlds;

namespace BetaSharp.Client.Input;

public class PlayerControllerSP : PlayerController
{
    private int field_1074_c = -1;
    private int field_1073_d = -1;
    private int field_1072_e = -1;
    private float curBlockDamage;
    private float prevBlockDamage;
    private float field_1069_h;
    private int blockHitWait;

    public PlayerControllerSP(BetaSharp game) : base(game)
    {
    }

    public override void flipPlayer(EntityPlayer player)
    {
        player.yaw = -180.0F;
    }

    public override bool sendBlockRemoved(int x, int y, int z, int face)
    {
        int blockId = Game.world.getBlockId(x, y, z);
        int blockMeta = Game.world.getBlockMeta(x, y, z);
        bool success = base.sendBlockRemoved(x, y, z, face);
        ItemStack heldItem = Game.player.getHand();
        bool canHarvest = Game.player.canHarvest(Block.Blocks[blockId]);
        if (heldItem != null)
        {
            heldItem.postMine(blockId, x, y, z, Game.player);
            if (heldItem.count == 0)
            {
                heldItem.onRemoved(Game.player);
                Game.player.clearStackInHand();
            }
        }

        if (success && canHarvest)
        {
            Block.Blocks[blockId].afterBreak(Game.world, Game.player, x, y, z, blockMeta);
        }

        return success;
    }

    public override void clickBlock(int x, int y, int z, int face)
    {
        Game.world.extinguishFire(Game.player, x, y, z, face);
        int blockId = Game.world.getBlockId(x, y, z);
        if (blockId > 0 && curBlockDamage == 0.0F)
        {
            Block.Blocks[blockId].onBlockBreakStart(Game.world, x, y, z, Game.player);
        }

        if (blockId > 0 && Block.Blocks[blockId].getHardness(Game.player) >= 1.0F)
        {
            sendBlockRemoved(x, y, z, face);
        }

    }

    public override void resetBlockRemoving()
    {
        curBlockDamage = 0.0F;
        blockHitWait = 0;
    }

    public override void sendBlockRemoving(int x, int y, int z, int face)
    {
        if (blockHitWait > 0)
        {
            --blockHitWait;
        }
        else
        {
            if (x == field_1074_c && y == field_1073_d && z == field_1072_e)
            {
                int blockId = Game.world.getBlockId(x, y, z);
                if (blockId == 0)
                {
                    return;
                }

                Block block = Block.Blocks[blockId];
                curBlockDamage += block.getHardness(Game.player);
                if (field_1069_h % 4.0F == 0.0F && block != null)
                {
                    Game.sndManager.PlaySound(block.soundGroup.StepSound, (float)x + 0.5F, (float)y + 0.5F, (float)z + 0.5F, (block.soundGroup.Volume + 1.0F) / 8.0F, block.soundGroup.Pitch * 0.5F);
                }

                ++field_1069_h;
                if (curBlockDamage >= 1.0F)
                {
                    sendBlockRemoved(x, y, z, face);
                    curBlockDamage = 0.0F;
                    prevBlockDamage = 0.0F;
                    field_1069_h = 0.0F;
                    blockHitWait = 5;
                }
            }
            else
            {
                curBlockDamage = 0.0F;
                prevBlockDamage = 0.0F;
                field_1069_h = 0.0F;
                field_1074_c = x;
                field_1073_d = y;
                field_1072_e = z;
            }

        }
    }

    public override void setPartialTime(float partialTicks)
    {
        if (curBlockDamage <= 0.0F)
        {
            Game.ingameGUI._damageGuiPartialTime = 0.0F;
            Game.terrainRenderer.damagePartialTime = 0.0F;
        }
        else
        {
            float interpolatedDamage = prevBlockDamage + (curBlockDamage - prevBlockDamage) * partialTicks;
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
        prevBlockDamage = curBlockDamage;
        Game.sndManager.PlayRandomMusicIfReady(DefaultMusicCategories.Game);
    }
}
