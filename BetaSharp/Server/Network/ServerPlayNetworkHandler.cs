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
using BetaSharp.Server.Commands;
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
        player.networkHandler = this;
    }

    public void tick()
    {
        moved = false;
        connection.tick();
        player.PlayerTick(false);

        if (ticks++ - lastKeepAliveTime > 20)
        {
            sendPacket(KeepAlivePacket.Get());
        }
    }

    public void disconnect(string reason)
    {
        player.onDisconnect();
        sendPacket(DisconnectPacket.Get(reason));
        connection.disconnect();
        server.playerManager.disconnect(player);
        server.playerManager.sendToAll(PlayerConnectionUpdateS2CPacket.Get(
            player.id,
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
            double var3 = packet.y - teleportTargetY;
            if (packet.x == teleportTargetX && var3 * var3 < 0.01 && packet.z == teleportTargetZ)
            {
                teleported = true;
            }
        }

        if (teleported)
        {
            if (player.vehicle != null)
            {
                float var27 = player.yaw;
                float var4 = player.pitch;
                player.vehicle.updatePassengerPosition();
                double var28 = player.x;
                double var29 = player.y;
                double var30 = player.z;
                double var31 = 0.0;
                double var34 = 0.0;
                if (packet.changeLook)
                {
                    var27 = packet.yaw;
                    var4 = packet.pitch;
                }

                if (packet.changePosition && packet.y == -999.0 && packet.eyeHeight == -999.0)
                {
                    var31 = packet.x;
                    var34 = packet.z;
                }

                player.onGround = packet.onGround;
                player.move(var31, 0.0, var34);
                player.setPositionAndAngles(var28, var29, var30, var27, var4);
                player.velocityX = var31;
                player.velocityZ = var34;
                if (player.vehicle != null)
                {
                    sWorld.Entities.TickVehicleBypassingFilter(player.vehicle, true);
                }

                if (player.vehicle != null)
                {
                    player.vehicle.updatePassengerPosition();
                }

                server.playerManager.updatePlayerChunks(player);
                teleportTargetX = player.x;
                teleportTargetY = player.y;
                teleportTargetZ = player.z;
                sWorld.Entities.UpdateEntity(player, true);
                return;
            }

            if (player.isSleeping())
            {
                player.setPositionAndAngles(teleportTargetX, teleportTargetY, teleportTargetZ, player.yaw, player.pitch);
                sWorld.Entities.UpdateEntity(player, true);
                return;
            }

            double var26 = player.y;
            teleportTargetX = player.x;
            teleportTargetY = player.y;
            teleportTargetZ = player.z;
            double var5 = player.x;
            double var7 = player.y;
            double var9 = player.z;
            float var11 = player.yaw;
            float var12 = player.pitch;
            if (packet.changePosition && packet.y == -999.0 && packet.eyeHeight == -999.0)
            {
                packet.changePosition = false;
            }

            if (packet.changePosition)
            {
                var5 = packet.x;
                var7 = packet.y;
                var9 = packet.z;
                double var13 = packet.eyeHeight - packet.y;
                if (!player.isSleeping() && (var13 > 1.65 || var13 < 0.1))
                {
                    disconnect("Illegal stance");
                    _logger.LogWarning($"{player.name} had an illegal stance: {var13}");
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
                var11 = packet.yaw;
                var12 = packet.pitch;
            }

            player.cameraOffset = 0.0F;
            player.setPositionAndAngles(teleportTargetX, teleportTargetY, teleportTargetZ, var11, var12);
            if (!teleported)
            {
                return;
            }

            double var32 = var5 - player.x;
            double var15 = var7 - player.y;
            double var17 = var9 - player.z;
            double var19 = var32 * var32 + var15 * var15 + var17 * var17;
            if (var19 > 100.0)
            {
                _logger.LogWarning($"{player.name} moved too quickly!");
                disconnect("You moved too quickly :( (Hacking?)");
                return;
            }

            float var21 = (1 / 16f);
            bool var22 = sWorld.Entities.GetEntityCollisionsScratch(player, player.boundingBox.Contract(var21, var21, var21)).Count == 0;
            player.move(var32, var15, var17);
            var32 = var5 - player.x;
            var15 = var7 - player.y;
            if (var15 > -0.5 || var15 < 0.5)
            {
                var15 = 0.0;
            }

            var17 = var9 - player.z;
            var19 = var32 * var32 + var15 * var15 + var17 * var17;
            bool var23 = false;
            if (var19 > 0.0625 && !player.isSleeping())
            {
                var23 = true;
                _logger.LogWarning($"{player.name} moved wrongly!");
                _logger.LogInformation($"Got position {var5}, {var7}, {var9}");
                _logger.LogInformation($"Expected {player.x}, {player.y}, {player.z}");
            }

            player.setPositionAndAngles(var5, var7, var9, var11, var12);
            bool var24 = sWorld.Entities.GetEntityCollisionsScratch(player, player.boundingBox.Contract(var21, var21, var21)).Count == 0;
            if (var22 && (var23 || !var24) && !player.isSleeping())
            {
                teleport(teleportTargetX, teleportTargetY, teleportTargetZ, var11, var12);
                return;
            }

            Box var25 = player.boundingBox.Expand(var21, var21, var21).Stretch(0.0, -0.55, 0.0);
            if (server.flightEnabled || sWorld.Reader.IsMaterialInBox(var25, m => m != Material.Air))
            {
                floatingTime = 0;
            }
            else if (var15 >= -0.03125)
            {
                floatingTime++;
                if (floatingTime > 80)
                {
                    _logger.LogWarning($"{player.name} was kicked for floating too long!");
                    disconnect("Flying is not enabled on this server");
                    return;
                }
            }

            player.onGround = packet.onGround;
            server.playerManager.updatePlayerChunks(player);
            player.handleFall(player.y - var26, packet.onGround);
        }
    }

    public void teleport(double x, double y, double z, float yaw, float pitch)
    {
        teleported = false;
        teleportTargetX = x;
        teleportTargetY = y;
        teleportTargetZ = z;
        player.setPositionAndAngles(x, y, z, yaw, pitch);
        player.networkHandler.sendPacket(PlayerMoveFullPacket.Get(x, y + 1.62F, y, z, yaw, pitch, false));
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
                if (MathHelper.GetDistSqr(player.x, player.y, player.z, x, y, z) < 256.0)
                {
                    player.networkHandler.sendPacket(BlockUpdateS2CPacket.Get(x, y, z, world));
                }

                return;
            }

            if (packet.action == 0 || packet.action == 2)
            {
                if (MathHelper.GetDistSqr(player.x, player.y, player.z, x, y, z) > 36.0)
                {
                    return;
                }
            }

            if (packet.action == 0)
            {
                if (!CanBypassSpawnProtection(x, z, world))
                {
                    player.networkHandler.sendPacket(BlockUpdateS2CPacket.Get(x, y, z, world));
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
                    player.networkHandler.sendPacket(BlockUpdateS2CPacket.Get(x, y, z, world));
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
        ItemStack stack = player.inventory.getSelectedItem();
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

            if (teleported && CanBypassSpawnProtection(x, z, world) && player.getSquaredDistance(x + 0.5, y + 0.5, z + 0.5) < 64.0)
            {
                player.interactionManager.interactBlock(player, world, stack, x, y, z, side);
            }

            player.networkHandler.sendPacket(BlockUpdateS2CPacket.Get(x, y, z, world));
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

            player.networkHandler.sendPacket(BlockUpdateS2CPacket.Get(x, y, z, world));
        }

        stack = player.inventory.getSelectedItem();
        if (stack != null && stack.count == 0)
        {
            player.inventory.main[player.inventory.selectedSlot] = null;
        }

        player.skipPacketSlotUpdates = true;
        player.inventory.main[player.inventory.selectedSlot] = ItemStack.clone(player.inventory.main[player.inventory.selectedSlot]);
        Slot slot = player.currentScreenHandler.GetSlot(player.inventory, player.inventory.selectedSlot);
        player.currentScreenHandler.SendContentUpdates();
        player.skipPacketSlotUpdates = false;
        if (!ItemStack.areEqual(player.inventory.getSelectedItem(), packet.stack))
        {
            sendPacket(ScreenHandlerSlotUpdateS2CPacket.Get(player.currentScreenHandler.SyncId, slot.id, player.inventory.getSelectedItem()));
        }
    }

    public override void onDisconnected(string reason, object[]? objects)
    {
        _logger.LogInformation($"{player.name} lost connection: {reason}");
        server.playerManager.disconnect(player);
        server.playerManager.sendToAll(PlayerConnectionUpdateS2CPacket.Get(
            player.id,
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

    public void sendPacket(Packet packet)
    {
        connection.sendPacket(packet);
        lastKeepAliveTime = ticks;
    }

    public override void onUpdateSelectedSlot(UpdateSelectedSlotC2SPacket packet)
    {
        if (packet.selectedSlot >= 0 && packet.selectedSlot <= InventoryPlayer.getHotbarSize())
        {
            player.interactionManager.UpdateMiningTool();
            player.inventory.selectedSlot = packet.selectedSlot;
        }
        else
        {
            _logger.LogWarning($"{player.name} tried to set an invalid carried item");
        }
    }

    public override void onChatMessage(ChatMessagePacket packet)
    {
        string var2 = packet.chatMessage;
        if (var2.Length > 100)
        {
            disconnect("Chat message too long");
        }
        else
        {
            var2 = var2.Trim();

            for (int var3 = 0; var3 < var2.Length; var3++)
            {
                // Allow the section sign (§) for color/style codes as well as the standard allowed characters
                if (var2[var3] == (char)167) // '§'
                {
                    continue;
                }

                if (!ChatAllowedCharacters.IsAllowedCharacter(var2[var3]))
                {
                    disconnect("Illegal characters in chat");
                    return;
                }
            }

            if (var2.StartsWith("/"))
            {
                handleCommand(var2);
            }
            else
            {
                var2 = "<" + player.name + "> " + var2;
                _logger.LogInformation(var2);
                server.playerManager.sendToAll(ChatMessagePacket.Get(var2));
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
            sendPacket(ChatMessagePacket.Get("§cYou do not have permission to use this command."));
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
            player.setSneaking(true);
        }
        else if (packet.mode == 2)
        {
            player.setSneaking(false);
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
        sendPacket(ChatMessagePacket.Get("§7" + message));
    }

    public string Name => player.name;
    public byte PermissionLevel => server.playerManager.isOperator(player.name) ? (byte)4 : (byte)0;

    public override void handleInteractEntity(PlayerInteractEntityC2SPacket packet)
    {
        ServerWorld var2 = server.getWorld(player.dimensionId);
        Entity var3 = var2.getEntity(packet.entityId);
        if (var3 != null && player.canSee(var3) && player.getSquaredDistance(var3) < 36.0)
        {
            if (packet.isLeftClick == 0)
            {
                player.interact(var3);
            }
            else if (packet.isLeftClick == 1)
            {
                player.attack(var3);
            }
        }
    }

    public override void onPlayerRespawn(PlayerRespawnPacket packet)
    {
        if (player.health <= 0)
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
            ItemStack var2 = player.currentScreenHandler.onSlotClick(packet.slot, packet.button, packet.holdingShift, player);
            if (ItemStack.areEqual(packet.stack, var2))
            {
                player.networkHandler.sendPacket(ScreenHandlerAcknowledgementPacket.Get(packet.syncId, packet.actionType, true));
                player.skipPacketSlotUpdates = true;
                player.currentScreenHandler.SendContentUpdates();
                player.updateCursorStack();
                player.skipPacketSlotUpdates = false;
            }
            else
            {
                // should something be done adding fails?
                transactions.TryAdd(player.currentScreenHandler.SyncId, packet.actionType);
                player.networkHandler.sendPacket(ScreenHandlerAcknowledgementPacket.Get(packet.syncId, packet.actionType, false));
                player.currentScreenHandler.updatePlayerList(player, false);

                int size = player.currentScreenHandler.Slots.Count;
                List<ItemStack> var3 = new List<ItemStack>(size);

                for (int i = 0; i < size; i++)
                {
                    var3.Add(((Slot)player.currentScreenHandler.Slots[i]).getStack());
                }

                player.onContentsUpdate(player.currentScreenHandler, var3);
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
        ServerWorld var2 = server.getWorld(player.dimensionId);
        if (var2.Reader.IsPosLoaded(packet.x, packet.y, packet.z))
        {
            BlockEntity var3 = var2.Entities.GetBlockEntity<BlockEntitySign>(packet.x, packet.y, packet.z);
            if (var3 is BlockEntitySign var4)
            {
                if (!var4.IsEditable())
                {
                    server.Warn("Player " + player.name + " just tried to change non-editable sign");
                    return;
                }
            }

            for (int var9 = 0; var9 < 4; var9++)
            {
                bool var5 = true;
                if (packet.text[var9].Length > 15)
                {
                    var5 = false;
                }
                else
                {
                    for (int var6 = 0; var6 < packet.text[var9].Length; var6++)
                    {
                        if (!ChatAllowedCharacters.IsAllowedCharacter(packet.text[var9][var6]))
                        {
                            var5 = false;
                        }
                    }
                }

                if (!var5)
                {
                    packet.text[var9] = "!?";
                }
            }

            if (var3 is BlockEntitySign var7)
            {
                int var10 = packet.x;
                int var11 = packet.y;
                int var12 = packet.z;

                for (int var8 = 0; var8 < 4; var8++)
                {
                    var7.Texts[var8] = packet.text[var8];
                }

                var7.SetEditable(false);
                var7.markDirty();
                var2.Broadcaster.BlockUpdateEvent(var10, var11, var12);
            }
        }
    }

    public override bool isServerSide()
    {
        return true;
    }
}
