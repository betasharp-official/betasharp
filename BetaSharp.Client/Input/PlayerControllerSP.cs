using BetaSharp.Blocks;
using BetaSharp.Client.Sound;
using BetaSharp.Entities;
using BetaSharp.Items;

namespace BetaSharp.Client.Input;

public class PlayerControllerSP : PlayerController
{
    private int _mineX = -1;
    private int _mineY = -1;
    private int _mineZ = -1;
    private float curBlockDamage;
    private float prevBlockDamage;
    private byte _mineSoundTimer;
    private int blockHitWait;

    public PlayerControllerSP(BetaSharp var1) : base(var1)
    {
    }

    public override void flipPlayer(EntityPlayer playerEntity)
    {
        playerEntity.yaw = -180.0F;
        playerEntity.prevYaw = -180.0F;
    }

    public override bool sendBlockRemoved(int x, int y, int z, int direction)
    {
        if (!Game.player.GameMode.CanBreak) return false;

        int blockId = Game.world.Reader.GetBlockId(x, y, z);
        bool blockRemoved = base.sendBlockRemoved(x, y, z, direction);
        ItemStack itemStackInHand = Game.player.getHand();
        bool canHarvest = Game.player.canHarvest(Block.Blocks[blockId]);
        if (itemStackInHand != null)
        {
            itemStackInHand.postMine(blockId, x, y, z, Game.player);
            if (itemStackInHand.count == 0)
            {
                itemStackInHand.onRemoved(Game.player);
                Game.player.clearStackInHand();
            }
        }

        if (blockRemoved && canHarvest)
        {
            Block.Blocks[blockId].onBreak(new OnBreakEvent(Game.world, Game.player, x, y, z));
        }

        return blockRemoved;
    }

    public override void clickBlock(int x, int y, int z, int direction)
    {
        if (Game.player.GameMode.CanExhaustFire)
        {
            Game.world.ExtinguishFire(Game.player, x, y, z, direction);
        }

        int blockId = Game.world.Reader.GetBlockId(x, y, z);
        if (blockId > 0 && curBlockDamage == 0.0F && Game.player.GameMode.CanInteract)
        {
            Block.Blocks[blockId].onBlockBreakStart(new OnBlockBreakStartEvent(Game.world, Game.player, x, y, z));
        }

        if (blockId > 0 && Game.player.GameMode.CanBreak && Block.Blocks[blockId].getHardness(Game.player) >= Game.player.GameMode.BrakeSpeed)
        {
            sendBlockRemoved(x, y, z, direction);
        }

    }

    public override void resetBlockRemoving()
    {
        curBlockDamage = 0.0F;
        blockHitWait = 0;
    }

    public override void sendBlockRemoving(int x, int y, int z, int direction)
    {
        if (!Game.player.GameMode.CanBreak) return;
        if (blockHitWait > 0)
        {
            --blockHitWait;
        }
        else
        {
            if (x == _mineX && y == _mineY && z == _mineZ)
            {
                int blockId = Game.world.Reader.GetBlockId(x, y, z);
                if (blockId == 0)
                {
                    return;
                }

                Block block = Block.Blocks[blockId];
                curBlockDamage += block.getHardness(Game.player);
                if (_mineSoundTimer % 4 == 0 && block != null)
                {
                    Game.sndManager.PlaySound(block.soundGroup.StepSound, x + 0.5F, y + 0.5F, z + 0.5F, (block.soundGroup.Volume + 1.0F) / 8.0F, block.soundGroup.Pitch * 0.5F);
                }

                ++_mineSoundTimer;
                if (curBlockDamage >= Game.player.GameMode.BrakeSpeed)
                {
                    sendBlockRemoved(x, y, z, direction);
                    curBlockDamage = 0.0F;
                    prevBlockDamage = 0.0F;
                    _mineSoundTimer = 0;
                    blockHitWait = 5;
                }
            }
            else
            {
                curBlockDamage = 0.0F;
                prevBlockDamage = 0.0F;
                _mineSoundTimer = 0;
                _mineX = x;
                _mineY = y;
                _mineZ = z;
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

    public override void updateController()
    {
        prevBlockDamage = curBlockDamage;
        Game.sndManager.PlayRandomMusicIfReady(DefaultMusicCategories.Game);
    }
}
