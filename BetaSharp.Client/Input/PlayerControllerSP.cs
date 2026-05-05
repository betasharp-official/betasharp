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

    public PlayerControllerSP(BetaSharp game) : base(game)
    {
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
        ItemStack itemStackInHand = Game.Player.GetHand();
        bool canHarvest = Game.Player.CanHarvest(Block.Blocks[blockId]);
        if (itemStackInHand != null)
        {
            itemStackInHand.postMine(blockId, x, y, z, Game.Player);
            if (itemStackInHand.Count == 0)
            {
                ItemStack.onRemoved(Game.Player);
                Game.Player.ClearStackInHand();
            }
        }

        if (blockRemoved && canHarvest)
        {
            Block.Blocks[blockId].onBreak(new OnBreakEvent(Game.World, Game.Player, x, y, z));
        }

        return blockRemoved;
    }

    public override void clickBlock(int x, int y, int z, int direction)
    {
        if (Game.Player.GameMode.CanExhaustFire)
        {
            Game.World.ExtinguishFire(Game.Player, x, y, z, direction);
        }

        int blockId = Game.World.Reader.GetBlockId(x, y, z);
        if (blockId > 0 && curBlockDamage == 0.0F && Game.Player.GameMode.CanInteract)
        {
            Block.Blocks[blockId].onBlockBreakStart(new OnBlockBreakStartEvent(Game.World, Game.Player, x, y, z));
        }

        if (blockId > 0 && Game.Player.GameMode.CanBreak && Block.Blocks[blockId].getHardness(Game.Player) >= Game.Player.GameMode.BrakeSpeed)
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
        if (!Game.Player.GameMode.CanBreak) return;
        if (blockHitWait > 0)
        {
            --blockHitWait;
        }
        else
        {
            if (x == _mineX && y == _mineY && z == _mineZ)
            {
                int blockId = Game.World.Reader.GetBlockId(x, y, z);
                if (blockId == 0)
                {
                    return;
                }

                Block block = Block.Blocks[blockId];
                curBlockDamage += block.getHardness(Game.Player);
                if (_mineSoundTimer % 4 == 0 && block != null)
                {
                    Game.SoundManager.PlaySound(block.SoundGroup.StepSound, x + 0.5F, y + 0.5F, z + 0.5F, (block.SoundGroup.Volume + 1.0F) / 8.0F, block.SoundGroup.Pitch * 0.5F);
                }

                ++_mineSoundTimer;
                if (curBlockDamage >= Game.Player.GameMode.BrakeSpeed)
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

    public override void setPartialTime(float tickDelta)
    {
        if (curBlockDamage <= 0.0F)
        {
            Game.WorldRenderer.DamagePartialTime = 0.0F;
        }
        else
        {
            float partialDamage = prevBlockDamage + (curBlockDamage - prevBlockDamage) * tickDelta;
            Game.WorldRenderer.DamagePartialTime = partialDamage;
        }

    }

    public override float getBlockReachDistance()
    {
        return 4.0F;
    }

    public override void updateController()
    {
        prevBlockDamage = curBlockDamage;
        Game.SoundManager.PlayRandomMusicIfReady(DefaultMusicCategories.Game);
    }
}
