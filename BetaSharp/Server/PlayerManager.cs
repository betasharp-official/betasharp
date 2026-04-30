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
        int viewDistance = server.config.GetViewDistance(10);
        _chunkMaps[0] = new ChunkMap(server, 0, viewDistance);
        _chunkMaps[1] = new ChunkMap(server, -1, viewDistance);
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
        player.ActiveChunks.Clear();
        player.ChunksTerrainSentToClient.Clear();
        GetChunkMap(player.DimensionId).addPlayer(player);
        ServerWorld var2 = _server.getWorld(player.DimensionId);
        var2.ChunkCache.LoadChunk((int)player.X >> 4, (int)player.Z >> 4);
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
        ServerWorld playerWorld = _server.getWorld(player.DimensionId);
        playerWorld.ChunkCache.LoadChunk((int)player.X >> 4, (int)player.Z >> 4);

        while (playerWorld.Entities.GetEntityCollisions(player, player.BoundingBox).Count != 0)
        {
            player.SetPosition(player.X, player.Y + 1.0, player.Z);
        }

        playerWorld.Entities.SpawnEntity(player);
        GetChunkMap(player.DimensionId).addPlayer(player);
    }

    public void updatePlayerChunks(ServerPlayerEntity player)
    {
        GetChunkMap(player.DimensionId).updatePlayerChunks(player);
    }

    public void disconnect(ServerPlayerEntity player)
    {
        _saveHandler.SavePlayerData(player);
        _server.getWorld(player.DimensionId).Entities.Remove(player);
        players.Remove(player);
        GetChunkMap(player.DimensionId).removePlayer(player);
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
            if (playerEntity.Name.EqualsIgnoreCase(name))
            {
                playerEntity.NetworkHandler.disconnect("You logged in from another location");
            }
        }

        return new ServerPlayerEntity(_server, _server.getWorld(0), name, new ServerPlayerInteractionManager(_server.getWorld(0)));
    }

    public ServerPlayerEntity respawnPlayer(ServerPlayerEntity player, int dimensionId)
    {
        _server.getEntityTracker(player.DimensionId).removeListener(player);
        _server.getEntityTracker(player.DimensionId).onEntityRemoved(player);
        GetChunkMap(player.DimensionId).removePlayer(player);
        players.Remove(player);
        _server.getWorld(player.DimensionId).Entities.ServerRemove(player);
        Vec3i? spawnPos = player.GetSpawnPos();
        player.DimensionId = dimensionId;
        ServerPlayerEntity serverPlayer = new(
            _server, _server.getWorld(player.DimensionId), player.Name, new ServerPlayerInteractionManager(_server.getWorld(player.DimensionId))
        )
        {
            ID = player.ID,
            NetworkHandler = player.NetworkHandler
        };
        ServerWorld targetWorld = _server.getWorld(player.DimensionId);
        if (spawnPos is (int x, int y, int z))
        {
            Vec3i? respawnPosition = EntityPlayer.FindRespawnPosition(_server.getWorld(player.DimensionId), spawnPos);
            if (respawnPosition is (int x2, int y2, int z2))
            {
                serverPlayer.SetPositionAndAnglesKeepPrevAngles(x2 + 0.5F, y2 + 0.1F, z2 + 0.5F, 0.0F, 0.0F);
                serverPlayer.SetSpawnPos(spawnPos);

            }
            else
            {
                serverPlayer.NetworkHandler.SendPacket(GameStateChangeS2CPacket.Get(0));
            }
        }

        targetWorld.ChunkCache.LoadChunk((int)serverPlayer.X >> 4, (int)serverPlayer.Z >> 4);

        while (targetWorld.Entities.GetEntityCollisions(serverPlayer, serverPlayer.BoundingBox).Count != 0)
        {
            serverPlayer.SetPosition(serverPlayer.X, serverPlayer.Y + 1.0, serverPlayer.Z);
        }

        serverPlayer.NetworkHandler.SendPacket(PlayerRespawnPacket.Get((sbyte)serverPlayer.DimensionId));
        serverPlayer.NetworkHandler.teleport(serverPlayer.X, serverPlayer.Y, serverPlayer.Z, serverPlayer.Yaw, serverPlayer.Pitch);
        sendWorldInfo(serverPlayer, targetWorld);
        GetChunkMap(serverPlayer.DimensionId).addPlayer(serverPlayer);
        targetWorld.SpawnEntity(serverPlayer);
        players.Add(serverPlayer);
        serverPlayer.initScreenHandler();
        return serverPlayer;
    }

    public void changePlayerDimension(ServerPlayerEntity player)
    {
        int targetDim = 0;
        if (player.DimensionId == -1)
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
        int sourceDim = player.DimensionId;
        ServerWorld currentWorld = _server.getWorld(sourceDim);
        ServerWorld targetWorld = _server.getWorld(targetDim);

        if (targetWorld == null)
        {
            return;
        }

        // Remove from source chunk map NOW, while player.x/z are still in
        // source-dimension space.
        GetChunkMap(sourceDim).removePlayer(player);

        player.DimensionId = targetDim;
        player.NetworkHandler.SendPacket(PlayerRespawnPacket.Get((sbyte)player.DimensionId));
        currentWorld.Entities.ServerRemove(player);
        player.Dead = false;
        double x = player.X;
        double z = player.Z;
        double scale = 8.0;

        if (player.DimensionId == -1)
        {
            x /= scale;
            z /= scale;
            player.SetPositionAndAnglesKeepPrevAngles(x, player.Y, z, player.Yaw, player.Pitch);
            if (player.IsAlive)
            {
                currentWorld.Entities.UpdateEntity(player, false);
            }
        }
        else
        {
            x *= scale;
            z *= scale;
            player.SetPositionAndAnglesKeepPrevAngles(x, player.Y, z, player.Yaw, player.Pitch);
            if (player.IsAlive)
            {
                currentWorld.Entities.UpdateEntity(player, false);
            }
        }

        if (player.IsAlive)
        {
            targetWorld.Entities.SpawnEntity(player);
            player.SetPositionAndAnglesKeepPrevAngles(x, player.Y, z, player.Yaw, player.Pitch);
            targetWorld.Entities.UpdateEntity(player, false);
            targetWorld.ChunkCache.forceLoad = true;
            new PortalForcer().MoveToPortal(targetWorld, player);
            targetWorld.ChunkCache.forceLoad = false;

            // Fully drain lighting updates generated during portal chunk
            // creation before the chunks are queued for the client.
            while (targetWorld.Lighting.DoLightingUpdates()) { }
        }

        updatePlayerAfterDimensionChange(player);
        player.NetworkHandler.teleport(player.X, player.Y, player.Z, player.Yaw, player.Pitch);
        player.SetWorld(targetWorld);
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

        for (int chunkMapIndex = 0; chunkMapIndex < _chunkMaps.Length; chunkMapIndex++)
        {
            _chunkMaps[chunkMapIndex].updateChunks();
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
        for (int playerIndex = 0; playerIndex < players.Count; playerIndex++)
        {
            ServerPlayerEntity playerEntity = players[playerIndex];
            playerEntity.NetworkHandler.SendPacket(packet);
        }
        packet.Return();
    }

    public void sendToDimension(Packet packet, int dimensionId)
    {
        for (int playerIndex = 0; playerIndex < players.Count; playerIndex++)
        {
            ServerPlayerEntity playerEntity = players[playerIndex];
            if (playerEntity.DimensionId == dimensionId)
            {
                playerEntity.NetworkHandler.SendPacket(packet);
            }
        }
        packet.Return();
    }

    public string getPlayerList()
    {
        return string.Join(", ", players.ConvertAll(p => p.Name));
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
        for (int playerIndex = 0; playerIndex < players.Count; playerIndex++)
        {
            ServerPlayerEntity playerEntity = players[playerIndex];
            if (playerEntity.Name.EqualsIgnoreCase(name))
            {
                return playerEntity;
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
                if (p.Value.DimensionId != d)
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
        ServerPlayerEntity playerEntity = getPlayer(name);
        if (playerEntity != null)
        {
            playerEntity.NetworkHandler.SendPacket(ChatMessagePacket.Get(message));
        }
    }

    public void sendToAround(double x, double y, double z, double range, int dimensionId, Packet packet)
    {
        sendToAround(null, x, y, z, range, dimensionId, packet);
    }

    public void sendToAround(EntityPlayer? player, double x, double y, double z, double range, int dimensionId, Packet packet)
    {
        for (int playerIndex = 0; playerIndex < players.Count; playerIndex++)
        {
            ServerPlayerEntity playerEntity = players[playerIndex];
            if (playerEntity != player && playerEntity.DimensionId == dimensionId)
            {
                double deltaX = x - playerEntity.X;
                double deltaY = y - playerEntity.Y;
                double deltaZ = z - playerEntity.Z;
                if (deltaX * deltaX + deltaY * deltaY + deltaZ * deltaZ < range * range)
                {
                    playerEntity.NetworkHandler.SendPacket(packet);
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
            if (isOperator(player.Name))
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
        for (int playerIndex = 0; playerIndex < players.Count; playerIndex++)
        {
            _saveHandler.SavePlayerData(players[playerIndex]);
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
        player.onContentsUpdate(player.PlayerScreenHandler);
        player.markHealthDirty();
    }
}
