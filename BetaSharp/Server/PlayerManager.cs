using BetaSharp.Blocks.Entities;
using BetaSharp.Entities;
using BetaSharp.Network.Packets;
using BetaSharp.Network.Packets.Play;
using BetaSharp.Network.Packets.S2CPlay;
using BetaSharp.Server.Network;
using BetaSharp.Server.Worlds;
using BetaSharp.Util;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds;
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
        _saveHandler = world[0].getWorldStorage().GetPlayerStorage();
    }

    public void updatePlayerAfterDimensionChange(ServerPlayerEntity player)
    {
        // Removal from the source chunk map is handled by sendPlayerToDimension before coordinate scaling.
        player.activeChunks.Clear();
        GetChunkMap(player.dimensionId).addPlayer(player);
        ServerWorld var2 = _server.getWorld(player.dimensionId);
        var2.chunkCache.LoadChunk((int)player.x >> 4, (int)player.z >> 4);
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
        ServerWorld var2 = _server.getWorld(player.dimensionId);
        var2.chunkCache.LoadChunk((int)player.x >> 4, (int)player.z >> 4);

        while (var2.GetEntityCollisions(player, player.boundingBox).Count != 0)
        {
            player.setPosition(player.x, player.y + 1.0, player.z);
        }

        var2.SpawnEntity(player);
        GetChunkMap(player.dimensionId).addPlayer(player);
    }

    public void updatePlayerChunks(ServerPlayerEntity player)
    {
        GetChunkMap(player.dimensionId).updatePlayerChunks(player);
    }

    public void disconnect(ServerPlayerEntity player)
    {
        _saveHandler.SavePlayerData(player);
        _server.getWorld(player.dimensionId).Remove(player);
        players.Remove(player);
        GetChunkMap(player.dimensionId).removePlayer(player);
    }

    public ServerPlayerEntity connectPlayer(ServerLoginNetworkHandler loginNetworkHandler, string name)
    {
        if (bannedPlayers.Contains(name.Trim().ToLower()))
        {
            loginNetworkHandler.disconnect("You are banned from this server!");
            return null;
        }
        else if (!isWhitelisted(name))
        {
            loginNetworkHandler.disconnect("You are not white-listed on this server!");
            return null;
        }
        else
        {
            // TODO: This does not work with IPEndpoint's ToString
            string var3 = loginNetworkHandler.connection.getAddress().ToString();
            var3 = var3.Substring(var3.IndexOf("/") + 1);
            var3 = var3.Substring(0, var3.IndexOf(":"));
            if (bannedIps.Contains(var3))
            {
                loginNetworkHandler.disconnect("Your IP address is banned from this server!");
                return null;
            }
            else if (players.Count >= _maxPlayerCount)
            {
                loginNetworkHandler.disconnect("The server is full!");
                return null;
            }
            else
            {
                for (int var4 = 0; var4 < players.Count; var4++)
                {
                    ServerPlayerEntity var5 = players[var4];
                    if (var5.name.EqualsIgnoreCase(name))
                    {
                        var5.networkHandler.disconnect("You logged in from another location");
                    }
                }

                return new ServerPlayerEntity(_server, _server.getWorld(0), name, new ServerPlayerInteractionManager(_server.getWorld(0)));
            }
        }
    }

    public ServerPlayerEntity respawnPlayer(ServerPlayerEntity player, int dimensionId)
    {
        _server.getEntityTracker(player.dimensionId).removeListener(player);
        _server.getEntityTracker(player.dimensionId).onEntityRemoved(player);
        GetChunkMap(player.dimensionId).removePlayer(player);
        players.Remove(player);
        _server.getWorld(player.dimensionId).serverRemove(player);
        Vec3i? var3 = player.getSpawnPos();
        player.dimensionId = dimensionId;
        ServerPlayerEntity var4 = new(
            _server, _server.getWorld(player.dimensionId), player.name, new ServerPlayerInteractionManager(_server.getWorld(player.dimensionId))
        )
        {
            id = player.id,
            networkHandler = player.networkHandler
        };
        ServerWorld var5 = _server.getWorld(player.dimensionId);
        if (var3 is (int x, int y, int z))
        {
            Vec3i? var6 = EntityPlayer.findRespawnPosition(_server.getWorld(player.dimensionId), var3);
            if (var6 is (int x2, int y2, int z2))
            {
                var4.setPositionAndAnglesKeepPrevAngles(x2 + 0.5F, y2 + 0.1F, z2 + 0.5F, 0.0F, 0.0F);
                var4.setSpawnPos(var3);

            }
            else
            {
                var4.networkHandler.sendPacket(new GameStateChangeS2CPacket(0));
            }
        }

        var5.chunkCache.LoadChunk((int)var4.x >> 4, (int)var4.z >> 4);

        while (var5.GetEntityCollisions(var4, var4.boundingBox).Count != 0)
        {
            var4.setPosition(var4.x, var4.y + 1.0, var4.z);
        }

        var4.networkHandler.sendPacket(new PlayerRespawnPacket((sbyte)var4.dimensionId));
        var4.networkHandler.teleport(var4.x, var4.y, var4.z, var4.yaw, var4.pitch);
        sendWorldInfo(var4, var5);
        GetChunkMap(var4.dimensionId).addPlayer(var4);
        var5.SpawnEntity(var4);
        players.Add(var4);
        var4.initScreenHandler();
        return var4;
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
        player.networkHandler.sendPacket(new PlayerRespawnPacket((sbyte)player.dimensionId));
        currentWorld.serverRemove(player);
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
                currentWorld.updateEntity(player, false);
            }
        }
        else
        {
            x *= scale;
            z *= scale;
            player.setPositionAndAnglesKeepPrevAngles(x, player.y, z, player.yaw, player.pitch);
            if (player.isAlive())
            {
                currentWorld.updateEntity(player, false);
            }
        }

        if (player.isAlive())
        {
            targetWorld.SpawnEntity(player);
            player.setPositionAndAnglesKeepPrevAngles(x, player.y, z, player.yaw, player.pitch);
            targetWorld.updateEntity(player, false);
            targetWorld.chunkCache.forceLoad = true;
            new PortalForcer().MoveToPortal(targetWorld, player);
            targetWorld.chunkCache.forceLoad = false;

            // Fully drain lighting updates generated during portal chunk
            // creation before the chunks are queued for the client. 
            while (targetWorld.doLightingUpdates()) { }
        }

        updatePlayerAfterDimensionChange(player);
        player.networkHandler.teleport(player.x, player.y, player.z, player.yaw, player.pitch);
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

    public void markDirty(int x, int y, int z, int dimensionId)
    {
        GetChunkMap(dimensionId).markBlockForUpdate(x, y, z);
    }

    public void sendToAll(Packet packet)
    {
        for (int var2 = 0; var2 < players.Count; var2++)
        {
            ServerPlayerEntity var3 = players[var2];
            var3.networkHandler.sendPacket(packet);
        }
    }

    public void sendToDimension(Packet packet, int dimensionId)
    {
        for (int var3 = 0; var3 < players.Count; var3++)
        {
            ServerPlayerEntity var4 = players[var3];
            if (var4.dimensionId == dimensionId)
            {
                var4.networkHandler.sendPacket(packet);
            }
        }
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

    public ServerPlayerEntity getPlayer(string name)
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

    public void messagePlayer(string name, string message)
    {
        ServerPlayerEntity var3 = getPlayer(name);
        if (var3 != null)
        {
            var3.networkHandler.sendPacket(new ChatMessagePacket(message));
        }
    }

    public void sendToAround(double x, double y, double z, double range, int dimensionId, Packet packet)
    {
        sendToAround(null, x, y, z, range, dimensionId, packet);
    }

    public void sendToAround(EntityPlayer player, double x, double y, double z, double range, int dimensionId, Packet packet)
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
                    var13.networkHandler.sendPacket(packet);
                }
            }
        }
    }

    public void broadcast(string message)
    {
        ChatMessagePacket var2 = new(message);

        for (int var3 = 0; var3 < players.Count; var3++)
        {
            ServerPlayerEntity var4 = players[var3];
            if (isOperator(var4.name))
            {
                var4.networkHandler.sendPacket(var2);
            }
        }
    }

    public bool sendPacket(string player, Packet packet)
    {
        ServerPlayerEntity var3 = getPlayer(player);
        if (var3 != null)
        {
            var3.networkHandler.sendPacket(packet);
            return true;
        }
        else
        {
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

    public void updateBlockEntity(int x, int y, int z, BlockEntity blockentity)
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

    public void sendWorldInfo(ServerPlayerEntity player, ServerWorld world)
    {
        player.networkHandler.sendPacket(new WorldTimeUpdateS2CPacket(world.getTime()));
        if (world.isRaining())
        {
            player.networkHandler.sendPacket(new GameStateChangeS2CPacket(1));
        }
    }

    public void sendPlayerStatus(ServerPlayerEntity player)
    {
        player.onContentsUpdate(player.playerScreenHandler);
        player.markHealthDirty();
    }
}
