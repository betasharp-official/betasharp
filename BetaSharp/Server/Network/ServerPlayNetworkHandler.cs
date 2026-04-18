using BetaSharp.Blocks.Entities;
using BetaSharp.Blocks.Materials;
using BetaSharp.Entities;
using BetaSharp.Inventorys;
using BetaSharp.Items;
using BetaSharp.Network;
using BetaSharp.Network.Packets;
using BetaSharp.Network.Packets.C2SPlay;
using BetaSharp.Network.Packets.Play;
using BetaSharp.Network.Packets.S2CPlay;
using BetaSharp.Screens.Slots;
using BetaSharp.Server.Command;
using BetaSharp.Server.Internal;
using BetaSharp.Util;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core;
using Microsoft.Extensions.Logging;

namespace BetaSharp.Server.Network;

public class ServerPlayNetworkHandler : NetHandler, ICommandOutput
{
    public Connection connection;
    public bool disconnected;
    private BetaSharpServer server;
    private ServerPlayerEntity player;
    private int ticks;
    private int lastKeepAliveTime;
    private int floatingTime;
    private bool moved;
    private double teleportTargetX;
    private double teleportTargetY;
    private double teleportTargetZ;
    private bool teleported = true;
    private Dictionary<int, short> transactions = new();

    private readonly ILogger<ServerPlayNetworkHandler> _logger = Log.Instance.For<ServerPlayNetworkHandler>();

    public ServerPlayNetworkHandler(BetaSharpServer server, Connection connection, ServerPlayerEntity player)
    {
        this.server = server;
        this.connection = connection;
        connection.setNetworkHandler(this);
        this.player = player;
        player.NetworkHandler = this;
    }

    public void tick()
    {
        moved = false;
        connection.tick();

        if (!moved) player.IdleTick();

        if (ticks++ - lastKeepAliveTime > 20) SendPacket(KeepAlivePacket.Get());
    }

    public void disconnect(string reason)
    {
        player.onDisconnect();
        SendPacket(DisconnectPacket.Get(reason));
        connection.disconnect();
        server.playerManager.disconnect(player);
        server.playerManager.sendToAll(PlayerConnectionUpdateS2CPacket.Get(
            player.ID,
            PlayerConnectionUpdateS2CPacket.ConnectionUpdateType.Leave,
            player.name
        ));
        server.playerManager.sendToAll(ChatMessagePacket.Get("§e" + player.name + " left the game."));
        disconnected = true;
    }


    public override void onPlayerInput(PlayerInputC2SPacket packet) => player.updateInput(packet);

    public override void onPlayerMove(PlayerMovePacket packet)
    {
        ServerWorld sWorld = server.getWorld(player.dimensionId);
        moved = true;
        if (!teleported)
        {
            double teleportDeltaY = packet.y - teleportTargetY;
            if (packet.x == teleportTargetX && teleportDeltaY * teleportDeltaY < 0.01 && packet.z == teleportTargetZ)
            {
                teleported = true;
            }
        }

        if (teleported)
        {
            if (player.Vehicle != null)
            {
                float yaw = player.Yaw;
                float pitch = player.Pitch;
                player.Vehicle.UpdatePassengerPosition();
                double vehicleX = player.X;
                double vehicleY = player.Y;
                double vehicleZ = player.Z;
                double moveX = 0.0;
                double moveZ = 0.0;
                if (packet.changeLook)
                {
                    yaw = packet.yaw;
                    pitch = packet.pitch;
                }

                if (packet.changePosition && packet.y == -999.0 && packet.eyeHeight == -999.0)
                {
                    moveX = packet.x;
                    moveZ = packet.z;
                }

                player.OnGround = packet.onGround;
                player.PlayerTick(false);
                player.Move(moveX, 0.0, moveZ);
                player.SetPositionAndAngles(vehicleX, vehicleY, vehicleZ, yaw, pitch);
                player.VelocityX = moveX;
                player.VelocityZ = moveZ;
                if (player.Vehicle != null)
                {
                    sWorld.Entities.TickVehicleBypassingFilter(player.Vehicle, true);
                }

                if (player.Vehicle != null)
                {
                    player.Vehicle.UpdatePassengerPosition();
                }

                server.playerManager.updatePlayerChunks(player);
                teleportTargetX = player.X;
                teleportTargetY = player.Y;
                teleportTargetZ = player.Z;
                sWorld.Entities.UpdateEntity(player, true);
                return;
            }

            if (player.isSleeping())
            {
                player.PlayerTick(false);
                player.SetPositionAndAngles(teleportTargetX, teleportTargetY, teleportTargetZ, player.Yaw, player.Pitch);
                sWorld.Entities.UpdateEntity(player, true);
                return;
            }

            double previousY = player.Y;
            teleportTargetX = player.X;
            teleportTargetY = player.Y;
            teleportTargetZ = player.Z;
            double targetX = player.X;
            double targetY = player.Y;
            double targetZ = player.Z;
            float targetYaw = player.Yaw;
            float targetPitch = player.Pitch;
            if (packet.changePosition && packet.y == -999.0 && packet.eyeHeight == -999.0)
            {
                packet.changePosition = false;
            }

            if (packet.changePosition)
            {
                targetX = packet.x;
                targetY = packet.y;
                targetZ = packet.z;
                double stanceHeight = packet.eyeHeight - packet.y;
                if (!player.isSleeping() && (stanceHeight > 1.65 || stanceHeight < 0.1))
                {
                    disconnect("Illegal stance");
                    _logger.LogWarning($"{player.name} had an illegal stance: {stanceHeight}");
                    return;
                }

                if (Math.Abs(packet.x) > 3.2E7 || Math.Abs(packet.z) > 3.2E7)
                {
                    disconnect("Illegal position");
                    return;
                }
            }

            if (packet.changeLook)
            {
                targetYaw = packet.yaw;
                targetPitch = packet.pitch;
            }

            player.PlayerTick(false);
            player.CameraOffset = 0.0F;
            player.SetPositionAndAngles(teleportTargetX, teleportTargetY, teleportTargetZ, targetYaw, targetPitch);
            if (!teleported)
            {
                return;
            }

            double deltaX = targetX - player.X;
            double deltaY = targetY - player.Y;
            double deltaZ = targetZ - player.Z;
            double movedDistanceSq = deltaX * deltaX + deltaY * deltaY + deltaZ * deltaZ;
            if (movedDistanceSq > 100.0)
            {
                _logger.LogWarning($"{player.name} moved too quickly!");
                disconnect("You moved too quickly :( (Hacking?)");
                return;
            }

            float collisionPadding = (1 / 16f);
            bool wasClear = sWorld.Entities.GetEntityCollisionsScratch(player, player.BoundingBox.Contract(collisionPadding, collisionPadding, collisionPadding)).Count == 0;
            player.Move(deltaX, deltaY, deltaZ);
            deltaX = targetX - player.X;
            deltaY = targetY - player.Y;
            if (deltaY > -0.5 || deltaY < 0.5)
            {
                deltaY = 0.0;
            }

            deltaZ = targetZ - player.Z;
            movedDistanceSq = deltaX * deltaX + deltaY * deltaY + deltaZ * deltaZ;
            bool validMove = false;
            if (movedDistanceSq > 0.0625 && !player.isSleeping())
            {
                validMove = true;
                _logger.LogWarning($"{player.name} moved wrongly!");
                _logger.LogInformation($"Got position {targetX}, {targetY}, {targetZ}");
                _logger.LogInformation($"Expected {player.X}, {player.Y}, {player.Z}");
            }

            player.SetPositionAndAngles(targetX, targetY, targetZ, targetYaw, targetPitch);
            bool isClearNow = sWorld.Entities.GetEntityCollisionsScratch(player, player.BoundingBox.Contract(collisionPadding, collisionPadding, collisionPadding)).Count == 0;
            if (wasClear && (validMove || !isClearNow) && !player.isSleeping())
            {
                teleport(teleportTargetX, teleportTargetY, teleportTargetZ, targetYaw, targetPitch);
                return;
            }

            Box flightCheckBox = player.BoundingBox.Expand(collisionPadding, collisionPadding, collisionPadding).Stretch(0.0, -0.55, 0.0);
            if (server.flightEnabled || sWorld.Reader.IsMaterialInBox(flightCheckBox, m => m != Material.Air))
            {
                floatingTime = 0;
            }
            else if (deltaY >= -0.03125)
            {
                floatingTime++;
                if (floatingTime > 80 && player.GameMode.DisallowFlying)
                {
                    _logger.LogWarning($"{player.name} was kicked for floating too long!");
                    disconnect("Flying is not enabled on this server");
                    return;
                }
            }

            player.OnGround = packet.onGround;
            server.playerManager.updatePlayerChunks(player);
            player.handleFall(player.Y - previousY, packet.onGround);
        }
    }

    public void teleport(double x, double y, double z, float yaw, float pitch)
    {
        teleported = false;
        teleportTargetX = x;
        teleportTargetY = y;
        teleportTargetZ = z;
        player.SetPositionAndAngles(x, y, z, yaw, pitch);
        player.NetworkHandler.SendPacket(PlayerMoveFullPacket.Get(x, y + 1.62F, y, z, yaw, pitch, false));
    }


    public override void handlePlayerAction(PlayerActionC2SPacket packet)
    {
        ServerWorld world = server.getWorld(player.dimensionId);
        if (packet.action == 4)
        {
            player.DropSelectedItem();
        }
        else
        {
            int x = packet.x;
            int y = packet.y;
            int z = packet.z;

            if (packet.action == 3)
            {
                if (MathHelper.GetDistSqr(player.X, player.Y, player.Z, x, y, z) < 256.0)
                {
                    player.NetworkHandler.SendPacket(BlockUpdateS2CPacket.Get(x, y, z, world));
                }

                return;
            }

            if (packet.action == 0 || packet.action == 2)
            {
                if (MathHelper.GetDistSqr(player.X, player.Y, player.Z, x, y, z) > 36.0)
                {
                    return;
                }
            }

            if (packet.action == 0)
            {
                if (!CanBypassSpawnProtection(x, z, world))
                {
                    player.NetworkHandler.SendPacket(BlockUpdateS2CPacket.Get(x, y, z, world));
                }
                else
                {
                    player.interactionManager.onBlockBreakingAction(x, y, z, packet.direction);
                }
            }
            else if (packet.action == 2)
            {
                player.interactionManager.continueMining(x, y, z);
                if (world.Reader.GetBlockId(x, y, z) != 0)
                {
                    player.NetworkHandler.SendPacket(BlockUpdateS2CPacket.Get(x, y, z, world));
                }
            }
        }
    }

    private bool CanBypassSpawnProtection(int x, int z, ServerWorld world)
    {
        const int spawnProtection = 16;
        Vec3i spawnPos = world.Properties.GetSpawnPos();
        bool notBlockedFromSpawnProtection = Math.Abs(x - spawnPos.X) > spawnProtection || Math.Abs(z - spawnPos.Z) > spawnProtection;
        notBlockedFromSpawnProtection = notBlockedFromSpawnProtection || world.BypassSpawnProtection || server is InternalServer || server.playerManager.isOperator(player.name);
        return notBlockedFromSpawnProtection;
    }

    public override void onPlayerInteractBlock(PlayerInteractBlockC2SPacket packet)
    {
        ServerWorld world = server.getWorld(player.dimensionId);
        ItemStack stack = player.inventory.GetItemInHand();
        if (packet.side == 255)
        {
            if (stack == null)
            {
                return;
            }

            player.interactionManager.interactItem(player, world, stack);
        }
        else
        {
            int x = packet.x;
            int y = packet.y;
            int z = packet.z;
            int side = packet.side;

            if (teleported && CanBypassSpawnProtection(x, z, world) && player.GetSquaredDistance(x + 0.5, y + 0.5, z + 0.5) < 64.0)
            {
                player.interactionManager.interactBlock(player, world, stack, x, y, z, side);
            }

            player.NetworkHandler.SendPacket(BlockUpdateS2CPacket.Get(x, y, z, world));
            switch (side)
            {
                case 0:
                    y--;
                    break;
                case 1:
                    y++;
                    break;
                case 2:
                    z--;
                    break;
                case 3:
                    z++;
                    break;
                case 4:
                    x--;
                    break;
                case 5:
                    x++;
                    break;
            }

            player.NetworkHandler.SendPacket(BlockUpdateS2CPacket.Get(x, y, z, world));
        }

        stack = player.inventory.GetItemInHand();
        if (stack != null && stack.Count == 0)
        {
            player.inventory.Main[player.inventory.SelectedSlot] = null;
        }

        player.skipPacketSlotUpdates = true;
        player.inventory.Main[player.inventory.SelectedSlot] = ItemStack.clone(player.inventory.Main[player.inventory.SelectedSlot]);
        Slot slot = player.currentScreenHandler.GetSlot(player.inventory, player.inventory.SelectedSlot);
        player.currentScreenHandler.SendContentUpdates();
        player.skipPacketSlotUpdates = false;
        if (!ItemStack.areEqual(player.inventory.GetItemInHand(), packet.stack))
        {
            SendPacket(ScreenHandlerSlotUpdateS2CPacket.Get(player.currentScreenHandler.SyncId, slot.id, player.inventory.GetItemInHand()));
        }
    }

    public override void onDisconnected(string reason, object[]? objects)
    {
        _logger.LogInformation($"{player.name} lost connection: {reason}");
        server.playerManager.disconnect(player);
        server.playerManager.sendToAll(PlayerConnectionUpdateS2CPacket.Get(
            player.ID,
            PlayerConnectionUpdateS2CPacket.ConnectionUpdateType.Leave,
            player.name
        ));
        server.playerManager.sendToAll(ChatMessagePacket.Get("§e" + player.name + " left the game."));
        disconnected = true;
    }

    public override void handle(Packet packet)
    {
        _logger.LogWarning($"{GetType()} wasn't prepared to deal with a {packet.GetType()}");
        disconnect("Protocol error, unexpected packet");
    }

    public void SendPacket(Packet packet)
    {
        connection.sendPacket(packet);
        lastKeepAliveTime = ticks;
    }

    public override void onUpdateSelectedSlot(UpdateSelectedSlotC2SPacket packet)
    {
        if (packet.selectedSlot >= 0 && packet.selectedSlot <= InventoryPlayer.HotbarSize)
        {
            player.interactionManager.UpdateMiningTool();
            player.inventory.SelectedSlot = packet.selectedSlot;
        }
        else
        {
            _logger.LogWarning($"{player.name} tried to set an invalid carried item");
        }
    }

    public override void onChatMessage(ChatMessagePacket packet)
    {
        string msg = packet.chatMessage;
        if (msg.Length > 100)
        {
            disconnect("Chat message too long");
        }
        else
        {
            msg = msg.Trim();

            for (int charIndex = 0; charIndex < msg.Length; charIndex++)
            {
                // Allow the section sign (§) for color/style codes as well as the standard allowed characters
                if (msg[charIndex] == (char)167) // '§'
                {
                    continue;
                }

                if (!ChatAllowedCharacters.IsAllowedCharacter(msg[charIndex]))
                {
                    disconnect("Illegal characters in chat");
                    return;
                }
            }

            if (msg.StartsWith("/"))
            {
                handleCommand(msg);
            }
            else
            {
                msg = "<" + player.name + "> " + msg;
                _logger.LogInformation(msg);
                server.playerManager.sendToAll(ChatMessagePacket.Get(msg));
            }
        }
    }

    private void handleCommand(string message)
    {
        if (message.ToLower().StartsWith("/me "))
        {
            string emote = "* " + player.name + " " + message[message.IndexOf(" ")..].Trim();
            _logger.LogInformation(emote);
            server.playerManager.sendToAll(ChatMessagePacket.Get(emote));
        }
        else if (server is InternalServer || server.playerManager.isOperator(player.name))
        {
            string commandText = message[1..];
            _logger.LogInformation($"{player.name} issued server command: {commandText}");
            server.QueueCommands(commandText, this);
        }
        else
        {
            string commandText = message[1..];
            _logger.LogInformation($"{player.name} tried command: {commandText}");
            SendPacket(ChatMessagePacket.Get("§cYou do not have permission to use this command."));
        }
    }

    public override void onEntityAnimation(EntityAnimationPacket packet)
    {
        if (packet.animationId == 1)
        {
            player.swingHand();
        }
    }

    public override void handleClientCommand(ClientCommandC2SPacket packet)
    {
        if (packet.mode == 1)
        {
            player.SetSneaking(true);
        }
        else if (packet.mode == 2)
        {
            player.SetSneaking(false);
        }
        else if (packet.mode == 3)
        {
            player.wakeUp(false, true, true);
            teleported = false;
        }
    }

    public override void onDisconnect(DisconnectPacket packet)
    {
        connection.disconnect("disconnect.quitting");
    }

    public int getBlockDataSendQueueSize()
    {
        return getWorldPacketBacklog();
    }

    public int getWorldPacketBacklog()
    {
        return connection.getWorldPacketBacklog();
    }

    public void SendMessage(string message)
    {
        SendPacket(ChatMessagePacket.Get("§7" + message));
    }

    public string Name => player.name;
    public byte PermissionLevel => server.playerManager.isOperator(player.name) ? (byte)4 : (byte)0;

    public override void handleInteractEntity(PlayerInteractEntityC2SPacket packet)
    {
        ServerWorld playerWorld = server.getWorld(player.dimensionId);
        Entity targetEntity = playerWorld.getEntity(packet.entityId);
        if (targetEntity != null && player.canSee(targetEntity) && player.GetSquaredDistance(targetEntity) < 36.0)
        {
            if (packet.isLeftClick == 0)
            {
                player.interact(targetEntity);
            }
            else if (packet.isLeftClick == 1)
            {
                player.attack(targetEntity);
            }
        }
    }

    public override void onPlayerRespawn(PlayerRespawnPacket packet)
    {
        if (player.Health <= 0)
        {
            player = server.playerManager.respawnPlayer(player, 0);
        }
    }

    public override void onCloseScreen(CloseScreenS2CPacket packet)
    {
        player.onHandledScreenClosed();
    }

    public override void onClickSlot(ClickSlotC2SPacket packet)
    {
        if (player.currentScreenHandler.SyncId == packet.syncId && player.currentScreenHandler.canOpen(player))
        {
            ItemStack clickedStack = player.currentScreenHandler.onSlotClick(packet.slot, packet.button, packet.holdingShift, player);
            if (ItemStack.areEqual(packet.stack, clickedStack))
            {
                player.NetworkHandler.SendPacket(ScreenHandlerAcknowledgementPacket.Get(packet.syncId, packet.actionType, true));
                player.skipPacketSlotUpdates = true;
                player.currentScreenHandler.SendContentUpdates();
                player.updateCursorStack();
                player.skipPacketSlotUpdates = false;
            }
            else
            {
                // should something be done adding fails?
                transactions.TryAdd(player.currentScreenHandler.SyncId, packet.actionType);
                player.NetworkHandler.SendPacket(ScreenHandlerAcknowledgementPacket.Get(packet.syncId, packet.actionType, false));
                player.currentScreenHandler.updatePlayerList(player, false);

                int size = player.currentScreenHandler.Slots.Count;
                List<ItemStack> slotStacks = new List<ItemStack>(size);

                for (int i = 0; i < size; i++)
                {
                    slotStacks.Add(((Slot)player.currentScreenHandler.Slots[i]).getStack());
                }

                player.onContentsUpdate(player.currentScreenHandler, slotStacks);
            }
        }
    }

    public override void onScreenHandlerAcknowledgement(ScreenHandlerAcknowledgementPacket packet)
    {
        if (transactions.TryGetValue(player.currentScreenHandler.SyncId, out short value)
            && packet.actionType == value
            && player.currentScreenHandler.SyncId == packet.syncId
            && !player.currentScreenHandler.canOpen(player))
        {
            player.currentScreenHandler.updatePlayerList(player, true);
        }
    }

    public override void handleUpdateSign(UpdateSignPacket packet)
    {
        ServerWorld playerWorld = server.getWorld(player.dimensionId);
        if (playerWorld.Reader.IsPosLoaded(packet.x, packet.y, packet.z))
        {
            BlockEntity blockEntity = playerWorld.Entities.GetBlockEntity<BlockEntitySign>(packet.x, packet.y, packet.z);
            BlockEntitySign? sign = blockEntity as BlockEntitySign;
            if (sign != null)
            {
                if (!sign.IsEditable())
                {
                    server.Warn("Player " + player.name + " just tried to change non-editable sign");
                    return;
                }
            }

            for (int lineIndex = 0; lineIndex < 4; lineIndex++)
            {
                bool lineValid = true;
                if (packet.text[lineIndex].Length > 15)
                {
                    lineValid = false;
                }
                else
                {
                    for (int charIndex = 0; charIndex < packet.text[lineIndex].Length; charIndex++)
                    {
                        if (!ChatAllowedCharacters.IsAllowedCharacter(packet.text[lineIndex][charIndex]))
                        {
                            lineValid = false;
                        }
                    }
                }

                if (!lineValid)
                {
                    packet.text[lineIndex] = "!?";
                }
            }

            if (sign != null)
            {
                int x = packet.x;
                int y = packet.y;
                int z = packet.z;

                for (int textLineIndex = 0; textLineIndex < 4; textLineIndex++)
                {
                    sign.Texts[textLineIndex] = packet.text[textLineIndex];
                }

                sign.SetEditable(false);
                sign.MarkDirty();
                playerWorld.Broadcaster.BlockUpdateEvent(x, y, z);
            }
        }
    }

    public override bool isServerSide()
    {
        return true;
    }
}
