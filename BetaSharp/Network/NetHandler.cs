using BetaSharp.Network.Packets;
using BetaSharp.Network.Packets.C2SPlay;
using BetaSharp.Network.Packets.Play;
using BetaSharp.Network.Packets.S2CPlay;

namespace BetaSharp.Network;

public abstract class NetHandler
{
    public abstract bool isServerSide();

    public virtual void handleChunkData(ChunkDataS2CPacket packet)
    {
    }

    public virtual void handle(Packet packet)
    {
    }

    public virtual void onDisconnected(string reason, object[]? details)
    {
    }

    public virtual void onDisconnect(DisconnectPacket packet)
    {
        handle(packet);
    }

    public virtual void onHello(LoginHelloPacket packet)
    {
        handle(packet);
    }

    public virtual void onPlayerMove(PlayerMovePacket packet)
    {
        handle(packet);
    }

    public virtual void onChunkDeltaUpdate(ChunkDeltaUpdateS2CPacket packet)
    {
        handle(packet);
    }

    public virtual void handlePlayerAction(PlayerActionC2SPacket packet)
    {
        handle(packet);
    }

    public virtual void onBlockUpdate(BlockUpdateS2CPacket packet)
    {
        handle(packet);
    }

    public virtual void onChunkStatusUpdate(ChunkStatusUpdateS2CPacket packet)
    {
        handle(packet);
    }

    public virtual void onPlayerSpawn(PlayerSpawnS2CPacket packet)
    {
        handle(packet);
    }

    public virtual void onEntity(EntityS2CPacket packet)
    {
        handle(packet);
    }

    public virtual void onEntityPosition(EntityPositionS2CPacket packet)
    {
        handle(packet);
    }

    public virtual void onPlayerInteractBlock(PlayerInteractBlockC2SPacket packet)
    {
        handle(packet);
    }

    public virtual void onUpdateSelectedSlot(UpdateSelectedSlotC2SPacket packet)
    {
        handle(packet);
    }

    public virtual void onEntityDestroy(EntityDestroyS2CPacket packet)
    {
        handle(packet);
    }

    public virtual void onItemEntitySpawn(ItemEntitySpawnS2CPacket packet)
    {
        handle(packet);
    }

    public virtual void onItemPickupAnimation(ItemPickupAnimationS2CPacket packet)
    {
        handle(packet);
    }

    public virtual void onChatMessage(ChatMessagePacket packet)
    {
        handle(packet);
    }

    public virtual void onEntitySpawn(EntitySpawnS2CPacket packet)
    {
        handle(packet);
    }

    public virtual void onEntityAnimation(EntityAnimationPacket packet)
    {
        handle(packet);
    }

    public virtual void handleClientCommand(ClientCommandC2SPacket packet)
    {
        handle(packet);
    }

    public virtual void onHandshake(HandshakePacket packet)
    {
        handle(packet);
    }

    public virtual void onLivingEntitySpawn(LivingEntitySpawnS2CPacket packet)
    {
        handle(packet);
    }

    public virtual void onWorldTimeUpdate(WorldTimeUpdateS2CPacket packet)
    {
        handle(packet);
    }

    public virtual void onPlayerSpawnPosition(PlayerSpawnPositionS2CPacket packet)
    {
        handle(packet);
    }

    public virtual void onEntityVelocityUpdate(EntityVelocityUpdateS2CPacket packet)
    {
        handle(packet);
    }

    public virtual void onEntityTrackerUpdate(EntityTrackerUpdateS2CPacket packet)
    {
        handle(packet);
    }

    public virtual void onEntityVehicleSet(EntityVehicleSetS2CPacket packet)
    {
        handle(packet);
    }

    public virtual void handleInteractEntity(PlayerInteractEntityC2SPacket packet)
    {
        handle(packet);
    }

    public virtual void onEntityStatus(EntityStatusS2CPacket packet)
    {
        handle(packet);
    }

    public virtual void onHealthUpdate(HealthUpdateS2CPacket packet)
    {
        handle(packet);
    }

    public virtual void onPlayerRespawn(PlayerRespawnPacket packet)
    {
        handle(packet);
    }

    public virtual void onExplosion(ExplosionS2CPacket packet)
    {
        handle(packet);
    }

    public virtual void onOpenScreen(OpenScreenS2CPacket packet)
    {
        handle(packet);
    }

    public virtual void onCloseScreen(CloseScreenS2CPacket packet)
    {
        handle(packet);
    }

    public virtual void onClickSlot(ClickSlotC2SPacket packet)
    {
        handle(packet);
    }

    public virtual void onScreenHandlerSlotUpdate(ScreenHandlerSlotUpdateS2CPacket packet)
    {
        handle(packet);
    }

    public virtual void onInventory(InventoryS2CPacket packet)
    {
        handle(packet);
    }

    public virtual void handleUpdateSign(UpdateSignPacket packet)
    {
        handle(packet);
    }

    public virtual void onScreenHandlerPropertyUpdate(ScreenHandlerPropertyUpdateS2CPacket packet)
    {
        handle(packet);
    }

    public virtual void onEntityEquipmentUpdate(EntityEquipmentUpdateS2CPacket packet)
    {
        handle(packet);
    }

    public virtual void onScreenHandlerAcknowledgement(ScreenHandlerAcknowledgementPacket packet)
    {
        handle(packet);
    }

    public virtual void onPaintingEntitySpawn(PaintingEntitySpawnS2CPacket packet)
    {
        handle(packet);
    }

    public virtual void onPlayNoteSound(PlayNoteSoundS2CPacket packet)
    {
        handle(packet);
    }

    public virtual void onPlayerConnectionUpdate(PlayerConnectionUpdateS2CPacket packet)
    {
        handle(packet);
    }

    public virtual void onPlayerGameModeUpdate(PlayerGameModeUpdateS2CPacket packet)
    {
        handle(packet);
    }

    public virtual void onRegistryData(RegistryDataS2CPacket packet)
    {
        handle(packet);
    }

    public virtual void onFinishConfiguration(FinishConfigurationS2CPacket packet)
    {
        handle(packet);
    }

    public virtual void onIncreaseStat(IncreaseStatS2CPacket packet)
    {
        handle(packet);
    }

    public virtual void onPlayerSleepUpdate(PlayerSleepUpdateS2CPacket packet)
    {
        handle(packet);
    }

    public virtual void onPlayerInput(PlayerInputC2SPacket packet)
    {
        handle(packet);
    }

    public virtual void onGameStateChange(GameStateChangeS2CPacket packet)
    {
        handle(packet);
    }

    public virtual void onLightningEntitySpawn(GlobalEntitySpawnS2CPacket packet)
    {
        handle(packet);
    }

    public virtual void onMapUpdate(MapUpdateS2CPacket packet)
    {
        handle(packet);
    }

    public virtual void onWorldEvent(WorldEventS2CPacket packet)
    {
        handle(packet);
    }
}
