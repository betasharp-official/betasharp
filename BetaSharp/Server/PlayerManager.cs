using BetaSharp.Blocks.Entities;
using BetaSharp.Entities;
using BetaSharp.Network.Packets;
using BetaSharp.Network.Packets.Play;
using BetaSharp.Network.Packets.S2CPlay;
using BetaSharp.Server.Network;
using BetaSharp.Server.Worlds;
using BetaSharp.Util;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core;
using BetaSharp.Worlds.Dimensions;

namespace BetaSharp.Server;

public class PlayerManager
{
    public List<ServerPlayerEntity> players = [];
    private readonly BetaSharpServer _server;
    private readonly ChunkMap[] _chunkMaps;
    private readonly int _maxPlayerCount;
    protected readonly HashSet<string> bannedPlayers = [];
    protected readonly HashSet<string> bannedIps = [];
    protected readonly HashSet<string> ops = [];
    protected readonly HashSet<string> whitelist = [];
    private IPlayerStorage _saveHandler;
    private readonly bool _whitelistEnabled;
    private volatile int _pendingViewDistance = -1;

    public PlayerManager(BetaSharpServer server)
    {
        _chunkMaps = new ChunkMap[2];
        _server = server;
        int var2 = server.config.GetViewDistance(10);
        _chunkMaps[0] = new ChunkMap(server, 0, var2);
        _chunkMaps[1] = new ChunkMap(server, -1, var2);
        _maxPlayerCount = server.config.GetMaxPlayers(20);
        _whitelistEnabled = server.config.GetWhiteList(false);
    }

    public void saveAllPlayers(ServerWorld[] world)
    {
        _saveHandler = world[0].GetWorldStorage().GetPlayerStorage();
        if (world.Length > 0 && world[0] != null)
        {
            world[0].ChunkMap = _chunkMaps[0];
        }
        if (world.Length > 1 && world[1] != null)
        {
            world[1].ChunkMap = _chunkMaps[1];
        }
    }

    public void updatePlayerAfterDimensionChange(ServerPlayerEntity player)
    {
        player.ResetChunkStreamingState();
        player.activeChunks.Clear();
        player.ChunksTerrainSentToClient.Clear();
        GetChunkMap(player.dimensionId).addPlayer(player);
        ServerWorld var2 = _server.getWorld(player.dimensionId);
        var2.ChunkCache.LoadChunk((int)player.x >> 4, (int)player.z >> 4);
    }

    public int getBlockViewDistance()
    {
        return _chunkMaps[0].getBlockViewDistance();
    }

    public void SetViewDistance(int newDistance)
    {
        _pendingViewDistance = newDistance;
    }

    private ChunkMap GetChunkMap(int dimensionId)
    {
        return dimensionId == -1 ? _chunkMaps[1] : _chunkMaps[0];
    }

    public void loadPlayerData(ServerPlayerEntity player)
    {
        _saveHandler.LoadPlayerData(player);
    }

    public void addPlayer(ServerPlayerEntity player)
    {
        players.Add(player);
        player.ResetChunkStreamingState();
        ServerWorld var2 = _server.getWorld(player.dimensionId);
        var2.ChunkCache.LoadChunk((int)player.x >> 4, (int)player.z >> 4);

        while (var2.Entities.GetEntityCollisions(player, player.boundingBox).Count != 0)
        {
            player.setPosition(player.x, player.y + 1.0, player.z);
        }

        var2.Entities.SpawnEntity(player);
        GetChunkMap(player.dimensionId).addPlayer(player);
    }

    public void updatePlayerChunks(ServerPlayerEntity player)
    {
        GetChunkMap(player.dimensionId).updatePlayerChunks(player);
    }

    public void disconnect(ServerPlayerEntity player)
    {
        _saveHandler.SavePlayerData(player);
        _server.getWorld(player.dimensionId).Entities.Remove(player);
        players.Remove(player);
        GetChunkMap(player.dimensionId).removePlayer(player);
    }

    public ServerPlayerEntity? connectPlayer(ServerLoginNetworkHandler loginNetworkHandler, string name)
    {
        try
        {
            PlayerNameValidator.Validate(name);
        }
        catch (InvalidPlayerNameException ex)
        {
            loginNetworkHandler.disconnect($"Kicked: {ex.Message}");
            return null;
        }

        if (bannedPlayers.Contains(name.Trim().ToLower()))
        {
            loginNetworkHandler.disconnect("You are banned from this server!");
            return null;
        }

        if (!isWhitelisted(name))
        {
            loginNetworkHandler.disconnect("You are not white-listed on this server!");
            return null;
        }

        // TODO: This does not work with IPEndpoint's ToString
        string address = loginNetworkHandler.connection.getAddress().ToString();
        address = address.Substring(address.IndexOf("/") + 1);
        address = address.Substring(0, address.IndexOf(":"));
        if (bannedIps.Contains(address))
        {
            loginNetworkHandler.disconnect("Your IP address is banned from this server!");
            return null;
        }

        if (players.Count >= _maxPlayerCount)
        {
            loginNetworkHandler.disconnect("The server is full!");
            return null;
        }

        foreach (var playerEntity in players)
        {
            if (playerEntity.name.EqualsIgnoreCase(name))
            {
                playerEntity.NetworkHandler.disconnect("You logged in from another location");
            }
        }

        return new ServerPlayerEntity(_server, _server.getWorld(0), name, new ServerPlayerInteractionManager(_server.getWorld(0)));
    }

    public ServerPlayerEntity respawnPlayer(ServerPlayerEntity player, int dimensionId)
    {
        _server.getEntityTracker(player.dimensionId).removeListener(player);
        _server.getEntityTracker(player.dimensionId).onEntityRemoved(player);
        GetChunkMap(player.dimensionId).removePlayer(player);
        players.Remove(player);
        _server.getWorld(player.dimensionId).Entities.ServerRemove(player);
        Vec3i? var3 = player.getSpawnPos();
        player.dimensionId = dimensionId;
        ServerPlayerEntity serverPlayer = new(
            _server, _server.getWorld(player.dimensionId), player.name, new ServerPlayerInteractionManager(_server.getWorld(player.dimensionId))
        )
        {
            id = player.id,
            NetworkHandler = player.NetworkHandler
        };
        ServerWorld var5 = _server.getWorld(player.dimensionId);
        if (var3 is (int x, int y, int z))
        {
            Vec3i? var6 = EntityPlayer.findRespawnPosition(_server.getWorld(player.dimensionId), var3);
            if (var6 is (int x2, int y2, int z2))
            {
                serverPlayer.setPositionAndAnglesKeepPrevAngles(x2 + 0.5F, y2 + 0.1F, z2 + 0.5F, 0.0F, 0.0F);
                serverPlayer.setSpawnPos(var3);

            }
            else
            {
                serverPlayer.NetworkHandler.SendPacket(GameStateChangeS2CPacket.Get(0));
            }
        }

        var5.ChunkCache.LoadChunk((int)serverPlayer.x >> 4, (int)serverPlayer.z >> 4);

        while (var5.Entities.GetEntityCollisions(serverPlayer, serverPlayer.boundingBox).Count != 0)
        {
            serverPlayer.setPosition(serverPlayer.x, serverPlayer.y + 1.0, serverPlayer.z);
        }

        serverPlayer.NetworkHandler.SendPacket(PlayerRespawnPacket.Get((sbyte)serverPlayer.dimensionId));
        serverPlayer.NetworkHandler.teleport(serverPlayer.x, serverPlayer.y, serverPlayer.z, serverPlayer.yaw, serverPlayer.pitch);
        sendWorldInfo(serverPlayer, var5);
        GetChunkMap(serverPlayer.dimensionId).addPlayer(serverPlayer);
        var5.SpawnEntity(serverPlayer);
        players.Add(serverPlayer);
        serverPlayer.initScreenHandler();
        return serverPlayer;
    }

    public void changePlayerDimension(ServerPlayerEntity player)
    {
        int targetDim = 0;
        if (player.dimensionId == -1)
        {
            targetDim = 0;
        }
        else
        {
            targetDim = -1;
        }

        sendPlayerToDimension(player, targetDim);
    }

    public void sendPlayerToDimension(ServerPlayerEntity player, int targetDim)
    {
        int sourceDim = player.dimensionId;
        ServerWorld currentWorld = _server.getWorld(sourceDim);
        ServerWorld targetWorld = _server.getWorld(targetDim);

        if (targetWorld == null)
        {
            return;
        }

        // Remove from source chunk map NOW, while player.x/z are still in
        // source-dimension space.
        GetChunkMap(sourceDim).removePlayer(player);

        player.dimensionId = targetDim;
        player.NetworkHandler.SendPacket(PlayerRespawnPacket.Get((sbyte)player.dimensionId));
        currentWorld.Entities.ServerRemove(player);
        player.dead = false;
        double x = player.x;
        double z = player.z;
        double scale = 8.0;

        if (player.dimensionId == -1)
        {
            x /= scale;
            z /= scale;
            player.setPositionAndAnglesKeepPrevAngles(x, player.y, z, player.yaw, player.pitch);
            if (player.isAlive())
            {
                currentWorld.Entities.UpdateEntity(player, false);
            }
        }
        else
        {
            x *= scale;
            z *= scale;
            player.setPositionAndAnglesKeepPrevAngles(x, player.y, z, player.yaw, player.pitch);
            if (player.isAlive())
            {
                currentWorld.Entities.UpdateEntity(player, false);
            }
        }

        if (player.isAlive())
        {
            targetWorld.Entities.SpawnEntity(player);
            player.setPositionAndAnglesKeepPrevAngles(x, player.y, z, player.yaw, player.pitch);
            targetWorld.Entities.UpdateEntity(player, false);
            targetWorld.ChunkCache.forceLoad = true;
            new PortalForcer().MoveToPortal(targetWorld, player);
            targetWorld.ChunkCache.forceLoad = false;

            // Fully drain lighting updates generated during portal chunk
            // creation before the chunks are queued for the client.
            while (targetWorld.Lighting.DoLightingUpdates()) { }
        }

        updatePlayerAfterDimensionChange(player);
        player.NetworkHandler.teleport(player.x, player.y, player.z, player.yaw, player.pitch);
        player.setWorld(targetWorld);
        sendWorldInfo(player, targetWorld);
        sendPlayerStatus(player);
    }

    public void updateAllChunks()
    {
        int viewDistanceUpdate = _pendingViewDistance;
        if (viewDistanceUpdate != -1)
        {
            _chunkMaps[0].SetViewDistance(viewDistanceUpdate);
            _chunkMaps[1].SetViewDistance(viewDistanceUpdate);
            _pendingViewDistance = -1;
        }

        for (int var1 = 0; var1 < _chunkMaps.Length; var1++)
        {
            _chunkMaps[var1].updateChunks();
        }
    }

    public void flushPendingChunkUpdates()
    {
        for (int i = 0; i < players.Count; i++)
        {
            players[i].FlushPendingChunkUpdates();
        }
    }

    public void markDirty(int x, int y, int z, int dimensionId)
    {
        GetChunkMap(dimensionId).markBlockForUpdate(x, y, z);
    }

    public void sendToAll(Packet packet)
    {
        for (int var2 = 0; var2 < players.Count; var2++)
        {
            ServerPlayerEntity var3 = players[var2];
            var3.NetworkHandler.SendPacket(packet);
        }
        packet.Return();
    }

    public void sendToDimension(Packet packet, int dimensionId)
    {
        for (int var3 = 0; var3 < players.Count; var3++)
        {
            ServerPlayerEntity var4 = players[var3];
            if (var4.dimensionId == dimensionId)
            {
                var4.NetworkHandler.SendPacket(packet);
            }
        }
        packet.Return();
    }

    public string getPlayerList()
    {
        return string.Join(", ", players.ConvertAll(p => p.name));
    }

    public void banPlayer(string name)
    {
        bannedPlayers.Add(name.ToLower());
        saveBannedPlayers();
    }

    public void unbanPlayer(string name)
    {
        bannedPlayers.Remove(name.ToLower());
        saveBannedPlayers();
    }

    protected virtual void loadBannedPlayers()
    {
    }

    protected virtual void saveBannedPlayers()
    {
    }

    public void banIp(string ip)
    {
        bannedIps.Add(ip.ToLower());
        saveBannedIps();
    }

    public void unbanIp(string ip)
    {
        bannedIps.Remove(ip.ToLower());
        saveBannedIps();
    }

    protected virtual void loadBannedIps()
    {
    }

    protected virtual void saveBannedIps()
    {
    }

    public void addToOperators(string name)
    {
        ops.Add(name.ToLower());
        saveOperators();
    }

    public void removeFromOperators(string name)
    {
        ops.Remove(name.ToLower());
        saveOperators();
    }

    protected virtual void loadOperators()
    {
    }

    protected virtual void saveOperators()
    {
    }

    protected virtual void loadWhitelist()
    {
    }

    protected virtual void saveWhitelist()
    {
    }

    public bool isWhitelisted(string name)
    {
        name = name.Trim().ToLower();
        return !_whitelistEnabled || ops.Contains(name) || whitelist.Contains(name);
    }

    public bool isOperator(string name)
    {
        return ops.Contains(name.Trim().ToLower());
    }

    public ServerPlayerEntity? getPlayer(string name)
    {
        for (int var2 = 0; var2 < players.Count; var2++)
        {
            ServerPlayerEntity var3 = players[var2];
            if (var3.name.EqualsIgnoreCase(name))
            {
                return var3;
            }
        }

        return null;
    }

    public ServerPlayerEntity? GetRandomPlayer()
    {
        if (players.Count == 0) return null;
        int id = Random.Shared.Next(players.Count);
        return players[id];
    }

    public ServerPlayerEntity[] GetPlayers(Vec3D? position = null, double range = -1, Selector selector = Selector.Arbitrary, int limit = -1, int? dimensionId = null)
    {
        // Perform the fast selecting first if applicable.
        if (players.Count == 0) return [];

        if (position == null) range = -1;
        if (selector == Selector.Arbitrary && dimensionId == null && range <= 0)
        {
            if (limit == -1 || limit >= players.Count) return players.ToArray();
            var array = new ServerPlayerEntity[limit];
            players.CopyTo(0, array, 0, limit);
            return array;
        }

        Dictionary<double, ServerPlayerEntity> dict;

        if (range > 0)
        {
            Vec3D pos = position!.Value;
            dict = players.Select((item, i) => (i, item)).ToDictionary(t => t.item.Position.distanceTo(pos), t => t.item);
        }
        else
        {
            switch (selector)
            {
                case Selector.Arbitrary:
                case Selector.Random:
                    dict = players.Select((item, i) => (i, item)).ToDictionary(t => (double)t.i, t => t.item);
                    break;
                case Selector.Nearest:
                case Selector.Furthest:
                    if (position == null) throw new ArgumentNullException(nameof(position));
                    Vec3D pos = position.Value;
                    dict = players.Select((item, i) => (i, item)).ToDictionary(t => t.item.Position.distanceTo(pos), t => t.item);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(selector), selector, null);
            }
        }

        if (dimensionId != null)
        {
            int d = dimensionId.Value;
            var keysToRemove = new List<double>();
            foreach (var p in dict)
            {
                if (p.Value.dimensionId != d)
                {
                    keysToRemove.Add(p.Key);
                }
            }
            foreach (double key in keysToRemove)
            {
                dict.Remove(key);
            }
        }

        if (range > 0)
        {
            var keysToRemove = new List<double>();
            foreach (var p in dict)
            {
                if (p.Key <= range)
                {
                    keysToRemove.Add(p.Key);
                }
            }
            foreach (double key in keysToRemove)
            {
                dict.Remove(key);
            }
        }

        IEnumerable<ServerPlayerEntity> outData;

        if (selector != Selector.Arbitrary && selector != Selector.Random)
        {
            outData = dict.OrderBy(p => p.Key).ToDictionary().Values;
        }
        else if (selector == Selector.Random)
        {
            outData = dict.Values.Shuffle();
        }
        else
        {
            outData = dict.Values;
        }

        if (limit > dict.Count) limit = -1;

        if (limit > 0) return outData.Take(limit).ToArray();
        return outData.ToArray();
    }

    public void messagePlayer(string name, string message)
    {
        ServerPlayerEntity var3 = getPlayer(name);
        if (var3 != null)
        {
            var3.NetworkHandler.SendPacket(ChatMessagePacket.Get(message));
        }
    }

    public void sendToAround(double x, double y, double z, double range, int dimensionId, Packet packet)
    {
        sendToAround(null, x, y, z, range, dimensionId, packet);
    }

    public void sendToAround(EntityPlayer? player, double x, double y, double z, double range, int dimensionId, Packet packet)
    {
        for (int var12 = 0; var12 < players.Count; var12++)
        {
            ServerPlayerEntity var13 = players[var12];
            if (var13 != player && var13.dimensionId == dimensionId)
            {
                double var14 = x - var13.x;
                double var16 = y - var13.y;
                double var18 = z - var13.z;
                if (var14 * var14 + var16 * var16 + var18 * var18 < range * range)
                {
                    var13.NetworkHandler.SendPacket(packet);
                }
            }
        }
        packet.Return();
    }

    /// <summary>
    /// Send <see cref="ChatMessagePacket"/> to all operators.
    /// </summary>
    /// <param name="message">message to log</param>
    public void BroadcastOp(string message)
    {
        var chatMessagePacket = ChatMessagePacket.Get(message);

        foreach (var player in players)
        {
            if (isOperator(player.name))
            {
                player.NetworkHandler.SendPacket(chatMessagePacket);
            }
        }

        chatMessagePacket.Return();
    }

    public bool sendPacket(string player, Packet packet)
    {
        return sendPacket(getPlayer(player), packet);
    }

    public static bool sendPacket(ServerPlayerEntity? player, Packet packet)
    {
        if (player != null)
        {
            player.NetworkHandler.SendPacket(packet);
            return true;
        }
        else
        {
            packet.Return();
            return false;
        }
    }

    public void savePlayers()
    {
        for (int var1 = 0; var1 < players.Count; var1++)
        {
            _saveHandler.SavePlayerData(players[var1]);
        }
    }

    public static void updateBlockEntity(int x, int y, int z, BlockEntity blockentity)
    {
    }

    public void addToWhitelist(string name)
    {
        whitelist.Add(name);
        saveWhitelist();
    }

    public void removeFromWhitelist(string name)
    {
        whitelist.Remove(name);
        saveWhitelist();
    }

    public HashSet<string> getWhitelist()
    {
        return whitelist;
    }

    public void reloadWhitelist()
    {
        loadWhitelist();
    }

    public static void sendWorldInfo(ServerPlayerEntity player, ServerWorld world)
    {
        player.NetworkHandler.SendPacket(WorldTimeUpdateS2CPacket.Get(world.GetTime()));
        if (world.Properties.IsRaining)
        {
            player.NetworkHandler.SendPacket(GameStateChangeS2CPacket.Get(1));
        }
    }

    public static void sendPlayerStatus(ServerPlayerEntity player)
    {
        player.onContentsUpdate(player.playerScreenHandler);
        player.markHealthDirty();
    }
}
