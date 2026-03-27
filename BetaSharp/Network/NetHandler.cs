using BetaSharp.Network.Packets;
using BetaSharp.Network.Packets.C2SPlay;
using BetaSharp.Network.Packets.Play;
using BetaSharp.Network.Packets.S2CPlay;

namespace BetaSharp.Network;

public abstract class NetHandler
{
    public abstract bool isServerSide();

    public virtual void handleChunkData(ChunkDataS2CPacket var1)
    {
    }

    public virtual void handle(Packet var1)
    {
    }

    public virtual void onDisconnected(string var1, object[]? var2)
    {
    }

    public virtual void onDisconnect(DisconnectPacket var1)
    {
        handle(var1);
    }

    public virtual void onHello(LoginHelloPacket var1)
    {
        handle(var1);
    }

    public virtual void onPlayerMove(PlayerMovePacket var1)
    {
        handle(var1);
    }

    public virtual void onChunkDeltaUpdate(ChunkDeltaUpdateS2CPacket var1)
    {
        handle(var1);
    }

    public virtual void handlePlayerAction(PlayerActionC2SPacket var1)
    {
        handle(var1);
    }

    public virtual void onBlockUpdate(BlockUpdateS2CPacket var1)
    {
        handle(var1);
    }

    public virtual void onChunkStatusUpdate(ChunkStatusUpdateS2CPacket var1)
    {
        handle(var1);
    }

    public virtual void onPlayerSpawn(PlayerSpawnS2CPacket var1)
    {
        handle(var1);
    }

    public virtual void onEntity(EntityS2CPacket var1)
    {
        handle(var1);
    }

    public virtual void onEntityPosition(EntityPositionS2CPacket var1)
    {
        handle(var1);
    }

    public virtual void onPlayerInteractBlock(PlayerInteractBlockC2SPacket var1)
    {
        handle(var1);
    }

    public virtual void onUpdateSelectedSlot(UpdateSelectedSlotC2SPacket var1)
    {
        handle(var1);
    }

    public virtual void onEntityDestroy(EntityDestroyS2CPacket var1)
    {
        handle(var1);
    }

    public virtual void onItemEntitySpawn(ItemEntitySpawnS2CPacket var1)
    {
        handle(var1);
    }

    public virtual void onItemPickupAnimation(ItemPickupAnimationS2CPacket var1)
    {
        handle(var1);
    }

    public virtual void onChatMessage(ChatMessagePacket var1)
    {
        handle(var1);
    }

    public virtual void onEntitySpawn(EntitySpawnS2CPacket var1)
    {
        handle(var1);
    }

    public virtual void onEntityAnimation(EntityAnimationPacket var1)
    {
        handle(var1);
    }

    public virtual void handleClientCommand(ClientCommandC2SPacket var1)
    {
        handle(var1);
    }

    public virtual void onHandshake(HandshakePacket var1)
    {
        handle(var1);
    }

    public virtual void onLivingEntitySpawn(LivingEntitySpawnS2CPacket var1)
    {
        handle(var1);
    }

    public virtual void onWorldTimeUpdate(WorldTimeUpdateS2CPacket var1)
    {
        handle(var1);
    }

    public virtual void onPlayerSpawnPosition(PlayerSpawnPositionS2CPacket var1)
    {
        handle(var1);
    }

    public virtual void onEntityVelocityUpdate(EntityVelocityUpdateS2CPacket var1)
    {
        handle(var1);
    }

    public virtual void onEntityTrackerUpdate(EntityTrackerUpdateS2CPacket var1)
    {
        handle(var1);
    }

    public virtual void onEntityVehicleSet(EntityVehicleSetS2CPacket var1)
    {
        handle(var1);
    }

    public virtual void handleInteractEntity(PlayerInteractEntityC2SPacket var1)
    {
        handle(var1);
    }

    public virtual void onEntityStatus(EntityStatusS2CPacket var1)
    {
        handle(var1);
    }

    public virtual void onHealthUpdate(HealthUpdateS2CPacket var1)
    {
        handle(var1);
    }

    public virtual void onPlayerRespawn(PlayerRespawnPacket var1)
    {
        handle(var1);
    }

    public virtual void onExplosion(ExplosionS2CPacket var1)
    {
        handle(var1);
    }

    public virtual void onOpenScreen(OpenScreenS2CPacket var1)
    {
        handle(var1);
    }

    public virtual void onCloseScreen(CloseScreenS2CPacket var1)
    {
        handle(var1);
    }

    public virtual void onClickSlot(ClickSlotC2SPacket var1)
    {
        handle(var1);
    }

    public virtual void onScreenHandlerSlotUpdate(ScreenHandlerSlotUpdateS2CPacket var1)
    {
        handle(var1);
    }

    public virtual void onInventory(InventoryS2CPacket var1)
    {
        handle(var1);
    }

    public virtual void handleUpdateSign(UpdateSignPacket var1)
    {
        handle(var1);
    }

    public virtual void onScreenHandlerPropertyUpdate(ScreenHandlerPropertyUpdateS2CPacket var1)
    {
        handle(var1);
    }

    public virtual void onEntityEquipmentUpdate(EntityEquipmentUpdateS2CPacket var1)
    {
        handle(var1);
    }

    public virtual void onScreenHandlerAcknowledgement(ScreenHandlerAcknowledgementPacket var1)
    {
        handle(var1);
    }

    public virtual void onPaintingEntitySpawn(PaintingEntitySpawnS2CPacket var1)
    {
        handle(var1);
    }

    public virtual void onPlayNoteSound(PlayNoteSoundS2CPacket var1)
    {
        handle(var1);
    }

    public virtual void onPlayerConnectionUpdate(PlayerConnectionUpdateS2CPacket packet)
    {
        handle(packet);
    }

    public virtual void onPlayerGameModeUpdate(PlayerGameModeUpdateS2CPacket packet)
    {
        handle(packet);
    }

    public virtual void onIncreaseStat(IncreaseStatS2CPacket var1)
    {
        handle(var1);
    }

    public virtual void onPlayerSleepUpdate(PlayerSleepUpdateS2CPacket var1)
    {
        handle(var1);
    }

    public virtual void onPlayerInput(PlayerInputC2SPacket var1)
    {
        handle(var1);
    }

    public virtual void onGameStateChange(GameStateChangeS2CPacket var1)
    {
        handle(var1);
    }

    public virtual void onLightningEntitySpawn(GlobalEntitySpawnS2CPacket var1)
    {
        handle(var1);
    }

    public virtual void onMapUpdate(MapUpdateS2CPacket var1)
    {
        handle(var1);
    }

    public virtual void onWorldEvent(WorldEventS2CPacket var1)
    {
        handle(var1);
    }
}
