using BetaSharp.Blocks;
using BetaSharp.Client.Entities;
using BetaSharp.Client.Network;
using BetaSharp.Client.Sound;
using BetaSharp.Entities;
using BetaSharp.Items;
using BetaSharp.Network.Packets.C2SPlay;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Client.Input;

public class PlayerControllerMP : PlayerController
{
    private Vec3i _targetBlockPos = new();
    private float _curBlockDamageMp;
    private float _prevBlockDamageMp;
    private byte _mineSoundTimer;
    private int _blockHitDelay;
    private bool _isHittingBlock;
    private readonly ClientNetworkHandler _netClientHandler;
    private int _currentPlayerItem;

    public PlayerControllerMP(BetaSharp game, ClientNetworkHandler networkHandler) : base(game)
    {
        _netClientHandler = networkHandler;
    }

    public override void FlipPlayer(EntityPlayer playerEntity)
    {
        playerEntity.Yaw = -180.0F;
        playerEntity.PrevYaw = -180.0F;
    }

    public override bool SendBlockRemoved(int x, int y, int z, int direction)
    {
        if (!Game.Player.GameMode.CanBreak) return false;

        int blockId = Game.World.Reader.GetBlockId(x, y, z);
        bool blockRemoved = base.SendBlockRemoved(x, y, z, direction);
        ItemStack? hand = Game.Player.GetHand();
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

    public override void ClickBlock(int x, int y, int z, int direction)
    {
        if (!_isHittingBlock || x != _targetBlockPos.X || y != _targetBlockPos.Y || z != _targetBlockPos.Z)
        {
            _netClientHandler.AddToSendQueue(PlayerActionC2SPacket.Get(0, x, y, z, direction));
            int blockId = Game.World.Reader.GetBlockId(x, y, z);
            if (blockId > 0 && _curBlockDamageMp == 0.0F && Game.Player.GameMode.CanInteract)
            {
                Block.Blocks[blockId].onBlockBreakStart(new OnBlockBreakStartEvent(Game.World, Game.Player, x, y, z));
            }

            if (!Game.Player.GameMode.CanBreak) return;

            if (blockId > 0 && Block.Blocks[blockId].getHardness(Game.Player) >= Game.Player.GameMode.BrakeSpeed)
            {
                SendBlockRemoved(x, y, z, direction);
            }
            else
            {
                _isHittingBlock = true;
                _targetBlockPos = new Vec3i(x, y, z);
                _curBlockDamageMp = 0.0F;
                _prevBlockDamageMp = 0.0F;
                _mineSoundTimer = 0;
            }
        }
    }

    public override void ResetBlockRemoving()
    {
        _curBlockDamageMp = 0.0F;
        _isHittingBlock = false;
    }

    public override void SendBlockRemoving(int x, int y, int z, int direction)
    {
        if (_isHittingBlock)
        {
            SyncCurrentPlayItem();
            if (_blockHitDelay > 0)
            {
                --_blockHitDelay;
            }
            else
            {
                if (x == _targetBlockPos.X && y == _targetBlockPos.Y && z == _targetBlockPos.Z)
                {
                    if (!Game.Player.GameMode.CanBreak) return;

                    int blockId = Game.World.Reader.GetBlockId(x, y, z);
                    if (blockId == 0)
                    {
                        _isHittingBlock = false;
                        return;
                    }

                    Block? block = Block.Blocks[blockId];

                    // If it's an unknown block id, break behavior will be handled on server.
                    if (block == null)
                    {
                        if (_mineSoundTimer++ % 4 == 0)
                        {
                            Game.SoundManager.PlayStepSound(Block.Bedrock.SoundGroup, x, y, z);
                        }

                        return;
                    }

                    _curBlockDamageMp += block.getHardness(Game.Player);
                    if (_mineSoundTimer++ % 4 == 0)
                    {
                        Game.SoundManager.PlayStepSound(block.SoundGroup, x, y, z);
                    }

                    if (_curBlockDamageMp >= 1.0F)
                    {
                        _isHittingBlock = false;
                        _netClientHandler.AddToSendQueue(PlayerActionC2SPacket.Get(2, x, y, z, direction));
                        if (SendBlockRemoved(x, y, z, direction))
                        {
                            Game.WorldRenderer.WorldEventBreak(blockId, Game.World.Reader.GetBlockMeta(x, y, z), x, y, z);
                        }

                        _curBlockDamageMp = 0.0F;
                        _prevBlockDamageMp = 0.0F;
                        _mineSoundTimer = 0;
                        _blockHitDelay = 5;
                    }
                }
                else
                {
                    ClickBlock(x, y, z, direction);
                }
            }
        }
    }

    public override void SetPartialTime(float tickDelta)
    {
        if (_curBlockDamageMp <= 0.0F)
        {
            Game.WorldRenderer.DamagePartialTime = 0.0F;
        }
        else
        {
            float partialDamage = _prevBlockDamageMp + (_curBlockDamageMp - _prevBlockDamageMp) * tickDelta;
            Game.WorldRenderer.DamagePartialTime = partialDamage;
        }
    }

    public override float GetBlockReachDistance() => 4.0F;

    public override void UpdateController()
    {
        SyncCurrentPlayItem();
        _prevBlockDamageMp = _curBlockDamageMp;
        Game.SoundManager.PlayRandomMusicIfReady(DefaultMusicCategories.Game);
    }

    private void SyncCurrentPlayItem()
    {
        int selectedSlot = Game.Player.Inventory.SelectedSlot;
        if (selectedSlot != _currentPlayerItem)
        {
            _currentPlayerItem = selectedSlot;
            _netClientHandler.AddToSendQueue(UpdateSelectedSlotC2SPacket.Get(_currentPlayerItem));
        }
    }

    public override bool SendPlaceBlock(
        ClientPlayerEntity player,
        IWorldContext world,
        ItemStack selectedItem,
        int blockX,
        int blockY,
        int blockZ,
        int blockSide
    )
    {
        SyncCurrentPlayItem();
        _netClientHandler.AddToSendQueue(PlayerInteractBlockC2SPacket.Get(blockX, blockY, blockZ, blockSide, player.Inventory.ItemInHand));
        bool placed = base.SendPlaceBlock(player, world, selectedItem, blockX, blockY, blockZ, blockSide);
        return placed;
    }

    public override bool SendUseItem(EntityPlayer player, World world, ItemStack stack)
    {
        SyncCurrentPlayItem();
        _netClientHandler.AddToSendQueue(PlayerInteractBlockC2SPacket.Get(-1, -1, -1, 255, player.Inventory.ItemInHand));
        bool usedItem = base.SendUseItem(player, world, stack);
        return usedItem;
    }

    public override EntityPlayer CreatePlayer(World world) =>
        new EntityClientPlayerMP(Game, world, Game.Session, _netClientHandler);

    public override void AttackEntity(EntityPlayer player, Entity target)
    {
        SyncCurrentPlayItem();
        _netClientHandler.AddToSendQueue(PlayerInteractEntityC2SPacket.Get(player.ID, target.ID, 1));
        player.Attack(target);
    }

    public override void InteractWithEntity(EntityPlayer player, Entity target)
    {
        SyncCurrentPlayItem();
        _netClientHandler.AddToSendQueue(PlayerInteractEntityC2SPacket.Get(player.ID, target.ID, 0));
        player.Interact(target);
    }

    public override ItemStack OnSlotClick(int windowId, int slotIndex, int mouseButton, bool shiftClick, EntityPlayer player)
    {
        short revision = player.CurrentScreenHandler.nextRevision(player.Inventory);
        ItemStack resultStack = base.OnSlotClick(windowId, slotIndex, mouseButton, shiftClick, player);
        _netClientHandler.AddToSendQueue(ClickSlotC2SPacket.Get(windowId, slotIndex, mouseButton, shiftClick, resultStack, revision));
        return resultStack;
    }

    public override void OnGuiClosed(int windowId, EntityPlayer player)
    {
        if (windowId != -9999) { }
    }
}
