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
            player.Name
        ));
        server.playerManager.sendToAll(ChatMessagePacket.Get("§e" + player.Name + " left the game."));
        disconnected = true;
    }


    public override void onPlayerInput(PlayerInputC2SPacket packet) => player.updateInput(packet);

    public override void onPlayerMove(PlayerMovePacket packet)
    {
        ServerWorld sWorld = server.getWorld(player.DimensionId);
        moved = true;
        if (!teleported)
        {
            double teleportDeltaY = packet.Y - teleportTargetY;
            if (packet.X == teleportTargetX && teleportDeltaY * teleportDeltaY < 0.01 && packet.Z == teleportTargetZ)
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
                if (packet.ChangeLook)
                {
                    yaw = packet.Yaw;
                    pitch = packet.Pitch;
                }

                if (packet.ChangePosition && packet.Y <= -999.0 && packet.EyeHeight <= -999.0)
                {
                    moveX = packet.X;
                    moveZ = packet.Z;
                }

                player.OnGround = packet.OnGround;
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

            if (player.IsSleeping)
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
            if (packet.ChangePosition && packet.Y <= -999.0 && packet.EyeHeight <= -999.0)
            {
                packet.ChangePosition = false;
            }

            if (packet.ChangePosition)
            {
                targetX = packet.X;
                targetY = packet.Y;
                targetZ = packet.Z;
                double stanceHeight = packet.EyeHeight - packet.Y;
                if (!player.IsSleeping && (stanceHeight > 1.65 || stanceHeight < 0.1))
                {
                    disconnect("Illegal stance");
                    _logger.LogWarning($"{player.Name} had an illegal stance: {stanceHeight}");
                    return;
                }

                if (Math.Abs(packet.X) > 3.2E7 || Math.Abs(packet.Z) > 3.2E7)
                {
                    disconnect("Illegal position");
                    return;
                }
            }

            if (packet.ChangeLook)
            {
                targetYaw = packet.Yaw;
                targetPitch = packet.Pitch;
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
                _logger.LogWarning($"{player.Name} moved too quickly!");
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
            if (movedDistanceSq > 0.0625 && !player.IsSleeping)
            {
                validMove = true;
                _logger.LogWarning($"{player.Name} moved wrongly!");
                _logger.LogInformation($"Got position {targetX}, {targetY}, {targetZ}");
                _logger.LogInformation($"Expected {player.X}, {player.Y}, {player.Z}");
            }

            player.SetPositionAndAngles(targetX, targetY, targetZ, targetYaw, targetPitch);
            bool isClearNow = sWorld.Entities.GetEntityCollisionsScratch(player, player.BoundingBox.Contract(collisionPadding, collisionPadding, collisionPadding)).Count == 0;
            if (wasClear && (validMove || !isClearNow) && !player.IsSleeping)
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
                    _logger.LogWarning($"{player.Name} was kicked for floating too long!");
                    disconnect("Flying is not enabled on this server");
                    return;
                }
            }

            player.OnGround = packet.OnGround;
            server.playerManager.updatePlayerChunks(player);
            player.handleFall(player.Y - previousY, packet.OnGround);
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
        ServerWorld world = server.getWorld(player.DimensionId);
        if (packet.Action == 4)
        {
            player.DropSelectedItem();
        }
        else
        {
            int x = packet.X;
            int y = packet.Y;
            int z = packet.Z;

            if (packet.Action == 3)
            {
                if (MathHelper.GetDistSqr(player.X, player.Y, player.Z, x, y, z) < 256.0)
                {
                    player.NetworkHandler.SendPacket(BlockUpdateS2CPacket.Get(x, y, z, world));
                }

                return;
            }

            if (packet.Action == 0 || packet.Action == 2)
            {
                if (MathHelper.GetDistSqr(player.X, player.Y, player.Z, x, y, z) > 36.0)
                {
                    return;
                }
            }

            if (packet.Action == 0)
            {
                if (!CanBypassSpawnProtection(x, z, world))
                {
                    player.NetworkHandler.SendPacket(BlockUpdateS2CPacket.Get(x, y, z, world));
                }
                else
                {
                    player.InteractionManager.onBlockBreakingAction(x, y, z, packet.Direction);
                }
            }
            else if (packet.Action == 2)
            {
                player.InteractionManager.continueMining(x, y, z);
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
        notBlockedFromSpawnProtection = notBlockedFromSpawnProtection || world.BypassSpawnProtection || server is InternalServer || server.playerManager.isOperator(player.Name);
        return notBlockedFromSpawnProtection;
    }

    public override void onPlayerInteractBlock(PlayerInteractBlockC2SPacket packet)
    {
        ServerWorld world = server.getWorld(player.DimensionId);
        ItemStack stack = player.Inventory.ItemInHand;
        if (packet.Side == 255)
        {
            if (stack == null)
            {
                return;
            }

            player.InteractionManager.interactItem(player, world, stack);
        }
        else
        {
            int x = packet.X;
            int y = packet.Y;
            int z = packet.Z;
            int side = packet.Side;

            if (teleported && CanBypassSpawnProtection(x, z, world) && player.GetSquaredDistance(x + 0.5, y + 0.5, z + 0.5) < 64.0)
            {
                player.InteractionManager.interactBlock(player, world, stack, x, y, z, side);
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

        stack = player.Inventory.ItemInHand;
        if (stack != null && stack.Count == 0)
        {
            player.Inventory.Main[player.Inventory.SelectedSlot] = null;
        }

        player.SkipPacketSlotUpdates = true;
        player.Inventory.Main[player.Inventory.SelectedSlot] = ItemStack.clone(player.Inventory.Main[player.Inventory.SelectedSlot]);
        Slot slot = player.CurrentScreenHandler.GetSlot(player.Inventory, player.Inventory.SelectedSlot);
        player.CurrentScreenHandler.SendContentUpdates();
        player.SkipPacketSlotUpdates = false;
        if (!ItemStack.areEqual(player.Inventory.ItemInHand, packet.Stack))
        {
            SendPacket(ScreenHandlerSlotUpdateS2CPacket.Get(player.CurrentScreenHandler.SyncId, slot.id, player.Inventory.ItemInHand));
        }
    }

    public override void onDisconnected(string reason, object[]? objects)
    {
        _logger.LogInformation($"{player.Name} lost connection: {reason}");
        server.playerManager.disconnect(player);
        server.playerManager.sendToAll(PlayerConnectionUpdateS2CPacket.Get(
            player.ID,
            PlayerConnectionUpdateS2CPacket.ConnectionUpdateType.Leave,
            player.Name
        ));
        server.playerManager.sendToAll(ChatMessagePacket.Get("§e" + player.Name + " left the game."));
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
        if (packet.SelectedSlot >= 0 && packet.SelectedSlot <= InventoryPlayer.HotbarSize)
        {
            player.InteractionManager.UpdateMiningTool();
            player.Inventory.SelectedSlot = packet.SelectedSlot;
        }
        else
        {
            _logger.LogWarning($"{player.Name} tried to set an invalid carried item");
        }
    }

    public override void onChatMessage(ChatMessagePacket packet)
    {
        string msg = packet.ChatMessage;
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
                msg = "<" + player.Name + "> " + msg;
                _logger.LogInformation(msg);
                server.playerManager.sendToAll(ChatMessagePacket.Get(msg));
            }
        }
    }

    private void handleCommand(string message)
    {
        if (message.ToLower().StartsWith("/me "))
        {
            string emote = "* " + player.Name + " " + message[message.IndexOf(" ")..].Trim();
            _logger.LogInformation(emote);
            server.playerManager.sendToAll(ChatMessagePacket.Get(emote));
        }
        else if (server is InternalServer || server.playerManager.isOperator(player.Name))
        {
            string commandText = message[1..];
            _logger.LogInformation($"{player.Name} issued server command: {commandText}");
            server.QueueCommands(commandText, this);
        }
        else
        {
            string commandText = message[1..];
            _logger.LogInformation($"{player.Name} tried command: {commandText}");
            SendPacket(ChatMessagePacket.Get("§cYou do not have permission to use this command."));
        }
    }

    public override void onEntityAnimation(EntityAnimationPacket packet)
    {
        if (packet.AnimationId == 1)
        {
            player.SwingHand();
        }
    }

    public override void handleClientCommand(ClientCommandC2SPacket packet)
    {
        if (packet.Mode == 1)
        {
            player.SetSneaking(true);
        }
        else if (packet.Mode == 2)
        {
            player.SetSneaking(false);
        }
        else if (packet.Mode == 3)
        {
            player.WakeUp(false, true, true);
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

    public string Name => player.Name;
    public byte PermissionLevel => server.playerManager.isOperator(player.Name) ? (byte)4 : (byte)0;

    public override void handleInteractEntity(PlayerInteractEntityC2SPacket packet)
    {
        ServerWorld playerWorld = server.getWorld(player.DimensionId);
        Entity targetEntity = playerWorld.getEntity(packet.EntityId);
        if (targetEntity != null && player.CanSee(targetEntity) && player.GetSquaredDistance(targetEntity) < 36.0)
        {
            if (packet.IsLeftClick == 0)
            {
                player.Interact(targetEntity);
            }
            else if (packet.IsLeftClick == 1)
            {
                player.Attack(targetEntity);
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
        if (player.CurrentScreenHandler.SyncId == packet.SyncId && player.CurrentScreenHandler.canOpen(player))
        {
            ItemStack clickedStack = player.CurrentScreenHandler.onSlotClick(packet.Slot, packet.Button, packet.HoldingShift, player);
            if (ItemStack.areEqual(packet.Stack, clickedStack))
            {
                player.NetworkHandler.SendPacket(ScreenHandlerAcknowledgementPacket.Get(packet.SyncId, packet.ActionType, true));
                player.SkipPacketSlotUpdates = true;
                player.CurrentScreenHandler.SendContentUpdates();
                player.updateCursorStack();
                player.SkipPacketSlotUpdates = false;
            }
            else
            {
                // should something be done adding fails?
                transactions.TryAdd(player.CurrentScreenHandler.SyncId, packet.ActionType);
                player.NetworkHandler.SendPacket(ScreenHandlerAcknowledgementPacket.Get(packet.SyncId, packet.ActionType, false));
                player.CurrentScreenHandler.updatePlayerList(player, false);

                int size = player.CurrentScreenHandler.Slots.Count;
                List<ItemStack> slotStacks = new List<ItemStack>(size);

                for (int i = 0; i < size; i++)
                {
                    slotStacks.Add(player.CurrentScreenHandler.Slots[i].getStack());
                }

                player.onContentsUpdate(player.CurrentScreenHandler, slotStacks);
            }
        }
    }

    public override void onScreenHandlerAcknowledgement(ScreenHandlerAcknowledgementPacket packet)
    {
        if (transactions.TryGetValue(player.CurrentScreenHandler.SyncId, out short value)
            && packet.ActionType == value
            && player.CurrentScreenHandler.SyncId == packet.SyncId
            && !player.CurrentScreenHandler.canOpen(player))
        {
            player.CurrentScreenHandler.updatePlayerList(player, true);
        }
    }

    public override void handleUpdateSign(UpdateSignPacket packet)
    {
        ServerWorld playerWorld = server.getWorld(player.DimensionId);
        if (playerWorld.Reader.IsPosLoaded(packet.X, packet.Y, packet.Z))
        {
            BlockEntity blockEntity = playerWorld.Entities.GetBlockEntity<BlockEntitySign>(packet.X, packet.Y, packet.Z);
            BlockEntitySign? sign = blockEntity as BlockEntitySign;
            if (sign != null)
            {
                if (!sign.IsEditable())
                {
                    server.Warn("Player " + player.Name + " just tried to change non-editable sign");
                    return;
                }
            }

            for (int lineIndex = 0; lineIndex < 4; lineIndex++)
            {
                bool lineValid = true;
                if (packet.Text[lineIndex].Length > 15)
                {
                    lineValid = false;
                }
                else
                {
                    for (int charIndex = 0; charIndex < packet.Text[lineIndex].Length; charIndex++)
                    {
                        if (!ChatAllowedCharacters.IsAllowedCharacter(packet.Text[lineIndex][charIndex]))
                        {
                            lineValid = false;
                        }
                    }
                }

                if (!lineValid)
                {
                    packet.Text[lineIndex] = "!?";
                }
            }

            if (sign != null)
            {
                int x = packet.X;
                int y = packet.Y;
                int z = packet.Z;

                for (int textLineIndex = 0; textLineIndex < 4; textLineIndex++)
                {
                    sign.Texts[textLineIndex] = packet.Text[textLineIndex];
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
