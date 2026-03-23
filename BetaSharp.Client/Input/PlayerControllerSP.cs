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

    public PlayerControllerSP(BetaSharp var1) : base(var1)
    {
    }

    public override void flipPlayer(EntityPlayer playerEntity)
    {
        playerEntity.yaw = -180.0F;
        playerEntity.prevYaw = -180.0F;
    }

    public override bool sendBlockRemoved(int x, int y, int z, int var4)
    {
        int blockId = Game.world.getBlockId(x, y, z);
        int var6 = Game.world.getBlockMeta(x, y, z);
        bool var7 = base.sendBlockRemoved(x, y, z, var4);
        ItemStack var8 = Game.player.getHand();
        bool var9 = Game.player.canHarvest(Block.Blocks[blockId]);
        if (var8 != null)
        {
            var8.postMine(blockId, x, y, z, Game.player);
            if (var8.count == 0)
            {
                var8.onRemoved(Game.player);
                Game.player.clearStackInHand();
            }
        }

        if (var7 && var9)
        {
            Block.Blocks[blockId].afterBreak(Game.world, Game.player, x, y, z, var6);
        }

        return var7;
    }

    public override void clickBlock(int var1, int var2, int var3, int var4)
    {
        Game.world.extinguishFire(Game.player, var1, var2, var3, var4);
        int var5 = Game.world.getBlockId(var1, var2, var3);
        if (var5 > 0 && curBlockDamage == 0.0F)
        {
            Block.Blocks[var5].onBlockBreakStart(Game.world, var1, var2, var3, Game.player);
        }

        if (var5 > 0 && Block.Blocks[var5].getHardness(Game.player) >= 1.0F)
        {
            sendBlockRemoved(var1, var2, var3, var4);
        }

    }

    public override void resetBlockRemoving()
    {
        curBlockDamage = 0.0F;
        blockHitWait = 0;
    }

    public override void sendBlockRemoving(int var1, int var2, int var3, int var4)
    {
        if (blockHitWait > 0)
        {
            --blockHitWait;
        }
        else
        {
            if (var1 == field_1074_c && var2 == field_1073_d && var3 == field_1072_e)
            {
                int var5 = Game.world.getBlockId(var1, var2, var3);
                if (var5 == 0)
                {
                    return;
                }

                Block var6 = Block.Blocks[var5];
                curBlockDamage += var6.getHardness(Game.player);
                if (field_1069_h % 4.0F == 0.0F && var6 != null)
                {
                    Game.sndManager.PlaySound(var6.soundGroup.StepSound, (float)var1 + 0.5F, (float)var2 + 0.5F, (float)var3 + 0.5F, (var6.soundGroup.Volume + 1.0F) / 8.0F, var6.soundGroup.Pitch * 0.5F);
                }

                ++field_1069_h;
                if (curBlockDamage >= 1.0F)
                {
                    sendBlockRemoved(var1, var2, var3, var4);
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
                field_1074_c = var1;
                field_1073_d = var2;
                field_1072_e = var3;
            }

        }
    }

    public override void setPartialTime(float var1)
    {
        if (curBlockDamage <= 0.0F)
        {
            Game.ingameGUI._damageGuiPartialTime = 0.0F;
            Game.terrainRenderer.damagePartialTime = 0.0F;
        }
        else
        {
            float var2 = prevBlockDamage + (curBlockDamage - prevBlockDamage) * var1;
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
        prevBlockDamage = curBlockDamage;
        Game.sndManager.PlayRandomMusicIfReady(DefaultMusicCategories.Game);
    }
}
