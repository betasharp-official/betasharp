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
    public List<ServerPlayerEntity> Players = [];
    private readonly MinecraftServer _server;
    private readonly ChunkMap[] _chunkMaps;
    private readonly int _maxPlayerCount;
    protected readonly HashSet<string> BannedPlayers = [];
    protected readonly HashSet<string> BannedIps = [];
    protected readonly HashSet<string> Ops = [];
    protected readonly HashSet<string> Whitelist = [];
    private IPlayerStorage _saveHandler;
    private readonly bool _whitelistEnabled;
    private volatile int _pendingViewDistance = -1;

    public PlayerManager(MinecraftServer server)
    {
        _chunkMaps = new ChunkMap[2];
        _server = server;
        int var2 = server.Config.GetViewDistance(10);
        _chunkMaps[0] = new ChunkMap(server, 0, var2);
        _chunkMaps[1] = new ChunkMap(server, -1, var2);
        _maxPlayerCount = server.Config.GetMaxPlayers(20);
        _whitelistEnabled = server.Config.GetWhiteList(false);
    }

    public void SaveAllPlayers(ServerWorld[] world)
    {
        _saveHandler = world[0].getWorldStorage().GetPlayerStorage();
    }

    public void UpdatePlayerAfterDimensionChange(ServerPlayerEntity player)
    {
        _chunkMaps[0].RemovePlayer(player);
        _chunkMaps[1].RemovePlayer(player);
        GetChunkMap(player.dimensionId).AddPlayer(player);
        ServerWorld var2 = _server.GetWorld(player.dimensionId);
        var2.chunkCache.LoadChunk((int)player.x >> 4, (int)player.z >> 4);
    }

    public int GetBlockViewDistance()
    {
        return _chunkMaps[0].GetBlockViewDistance();
    }

    public void SetViewDistance(int newDistance)
    {
        _pendingViewDistance = newDistance;
    }

    private ChunkMap GetChunkMap(int dimensionId)
    {
        return dimensionId == -1 ? _chunkMaps[1] : _chunkMaps[0];
    }

    public void LoadPlayerData(ServerPlayerEntity player)
    {
        _saveHandler.LoadPlayerData(player);
    }

    public void AddPlayer(ServerPlayerEntity player)
    {
        Players.Add(player);
        ServerWorld var2 = _server.GetWorld(player.dimensionId);
        var2.chunkCache.LoadChunk((int)player.x >> 4, (int)player.z >> 4);

        while (var2.getEntityCollisions(player, player.boundingBox).Count != 0)
        {
            player.setPosition(player.x, player.y + 1.0, player.z);
        }

        var2.SpawnEntity(player);
        GetChunkMap(player.dimensionId).AddPlayer(player);
    }

    public void UpdatePlayerChunks(ServerPlayerEntity player)
    {
        GetChunkMap(player.dimensionId).UpdatePlayerChunks(player);
    }

    public void Disconnect(ServerPlayerEntity player)
    {
        _saveHandler.SavePlayerData(player);
        _server.GetWorld(player.dimensionId).Remove(player);
        Players.Remove(player);
        GetChunkMap(player.dimensionId).RemovePlayer(player);
    }

    public ServerPlayerEntity ConnectPlayer(ServerLoginNetworkHandler loginNetworkHandler, string name)
    {
        if (BannedPlayers.Contains(name.Trim().ToLower()))
        {
            loginNetworkHandler.Disconnect("You are banned from this server!");
            return null;
        }
        else if (!IsWhitelisted(name))
        {
            loginNetworkHandler.Disconnect("You are not white-listed on this server!");
            return null;
        }
        else
        {
            // TODO: This does not work with IPEndpoint's ToString
            string var3 = loginNetworkHandler.Connection.getAddress().ToString();
            var3 = var3.Substring(var3.IndexOf("/") + 1);
            var3 = var3.Substring(0, var3.IndexOf(":"));
            if (BannedIps.Contains(var3))
            {
                loginNetworkHandler.Disconnect("Your IP address is banned from this server!");
                return null;
            }
            else if (Players.Count >= _maxPlayerCount)
            {
                loginNetworkHandler.Disconnect("The server is full!");
                return null;
            }
            else
            {
                for (int var4 = 0; var4 < Players.Count; var4++)
                {
                    ServerPlayerEntity var5 = Players[var4];
                    if (var5.name.EqualsIgnoreCase(name))
                    {
                        var5.networkHandler.Disconnect("You logged in from another location");
                    }
                }

                return new ServerPlayerEntity(_server, _server.GetWorld(0), name, new ServerPlayerInteractionManager(_server.GetWorld(0)));
            }
        }
    }

    public ServerPlayerEntity RespawnPlayer(ServerPlayerEntity player, int dimensionId)
    {
        _server.GetEntityTracker(player.dimensionId).RemoveListener(player);
        _server.GetEntityTracker(player.dimensionId).OnEntityRemoved(player);
        GetChunkMap(player.dimensionId).RemovePlayer(player);
        Players.Remove(player);
        _server.GetWorld(player.dimensionId).serverRemove(player);
        Vec3i? var3 = player.getSpawnPos();
        player.dimensionId = dimensionId;
        ServerPlayerEntity var4 = new(
            _server, _server.GetWorld(player.dimensionId), player.name, new ServerPlayerInteractionManager(_server.GetWorld(player.dimensionId))
        )
        {
            id = player.id,
            networkHandler = player.networkHandler
        };
        ServerWorld var5 = _server.GetWorld(player.dimensionId);
        if (var3 is (int x, int y, int z))
        {
            Vec3i? var6 = EntityPlayer.findRespawnPosition(_server.GetWorld(player.dimensionId), var3);
            if (var6 is (int x2, int y2, int z2))
            {
                var4.setPositionAndAnglesKeepPrevAngles(x2 + 0.5F, y2 + 0.1F, z2 + 0.5F, 0.0F, 0.0F);
                var4.setSpawnPos(var3);

            }
            else
            {
                var4.networkHandler.SendPacket(new GameStateChangeS2CPacket(0));
            }
        }

        var5.chunkCache.LoadChunk((int)var4.x >> 4, (int)var4.z >> 4);

        while (var5.getEntityCollisions(var4, var4.boundingBox).Count != 0)
        {
            var4.setPosition(var4.x, var4.y + 1.0, var4.z);
        }

        var4.networkHandler.SendPacket(new PlayerRespawnPacket((sbyte)var4.dimensionId));
        var4.networkHandler.Teleport(var4.x, var4.y, var4.z, var4.yaw, var4.pitch);
        SendWorldInfo(var4, var5);
        GetChunkMap(var4.dimensionId).AddPlayer(var4);
        var5.SpawnEntity(var4);
        Players.Add(var4);
        var4.initScreenHandler();
        return var4;
    }

    public void ChangePlayerDimension(ServerPlayerEntity player)
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

        SendPlayerToDimension(player, targetDim);
    }

    public void SendPlayerToDimension(ServerPlayerEntity player, int targetDim)
    {
        ServerWorld currentWorld = _server.GetWorld(player.dimensionId);
        ServerWorld targetWorld = _server.GetWorld(targetDim);

        if (targetWorld == null)
        {
            return;
        }

        player.dimensionId = targetDim;
        player.networkHandler.SendPacket(new PlayerRespawnPacket((sbyte)player.dimensionId));
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
            targetWorld.chunkCache.ForceLoad = true;
            new PortalForcer().MoveToPortal(targetWorld, player);
            targetWorld.chunkCache.ForceLoad = false;
        }

        UpdatePlayerAfterDimensionChange(player);
        player.networkHandler.Teleport(player.x, player.y, player.z, player.yaw, player.pitch);
        player.setWorld(targetWorld);
        SendWorldInfo(player, targetWorld);
        SendPlayerStatus(player);
    }

    public void UpdateAllChunks()
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
            _chunkMaps[var1].UpdateChunks();
        }
    }

    public void MarkDirty(int x, int y, int z, int dimensionId)
    {
        GetChunkMap(dimensionId).MarkBlockForUpdate(x, y, z);
    }

    public void SendToAll(Packet packet)
    {
        for (int var2 = 0; var2 < Players.Count; var2++)
        {
            ServerPlayerEntity var3 = Players[var2];
            var3.networkHandler.SendPacket(packet);
        }
    }

    public void SendToDimension(Packet packet, int dimensionId)
    {
        for (int var3 = 0; var3 < Players.Count; var3++)
        {
            ServerPlayerEntity var4 = Players[var3];
            if (var4.dimensionId == dimensionId)
            {
                var4.networkHandler.SendPacket(packet);
            }
        }
    }

    public string GetPlayerList()
    {
        string var1 = "";

        for (int var2 = 0; var2 < Players.Count; var2++)
        {
            if (var2 > 0)
            {
                var1 += ", ";
            }

            var1 += Players[var2].name;
        }

        return var1;
    }

    public void BanPlayer(string name)
    {
        BannedPlayers.Add(name.ToLower());
        SaveBannedPlayers();
    }

    public void UnbanPlayer(string name)
    {
        BannedPlayers.Remove(name.ToLower());
        SaveBannedPlayers();
    }

    protected virtual void LoadBannedPlayers()
    {
    }

    protected virtual void SaveBannedPlayers()
    {
    }

    public void BanIp(string ip)
    {
        BannedIps.Add(ip.ToLower());
        SaveBannedIps();
    }

    public void UnbanIp(string ip)
    {
        BannedIps.Remove(ip.ToLower());
        SaveBannedIps();
    }

    protected virtual void LoadBannedIps()
    {
    }

    protected virtual void SaveBannedIps()
    {
    }

    public void AddToOperators(string name)
    {
        Ops.Add(name.ToLower());
        SaveOperators();
    }

    public void RemoveFromOperators(string name)
    {
        Ops.Remove(name.ToLower());
        SaveOperators();
    }

    protected virtual void LoadOperators()
    {
    }

    protected virtual void SaveOperators()
    {
    }

    protected virtual void LoadWhitelist()
    {
    }

    protected virtual void SaveWhitelist()
    {
    }

    public bool IsWhitelisted(string name)
    {
        name = name.Trim().ToLower();
        return !_whitelistEnabled || Ops.Contains(name) || Whitelist.Contains(name);
    }

    public bool IsOperator(string name)
    {
        return Ops.Contains(name.Trim().ToLower());
    }

    public ServerPlayerEntity GetPlayer(string name)
    {
        for (int var2 = 0; var2 < Players.Count; var2++)
        {
            ServerPlayerEntity var3 = Players[var2];
            if (var3.name.EqualsIgnoreCase(name))
            {
                return var3;
            }
        }

        return null;
    }

    public void MessagePlayer(string name, string message)
    {
        ServerPlayerEntity var3 = GetPlayer(name);
        if (var3 != null)
        {
            var3.networkHandler.SendPacket(new ChatMessagePacket(message));
        }
    }

    public void SendToAround(double x, double y, double z, double range, int dimensionId, Packet packet)
    {
        SendToAround(null, x, y, z, range, dimensionId, packet);
    }

    public void SendToAround(EntityPlayer player, double x, double y, double z, double range, int dimensionId, Packet packet)
    {
        for (int var12 = 0; var12 < Players.Count; var12++)
        {
            ServerPlayerEntity var13 = Players[var12];
            if (var13 != player && var13.dimensionId == dimensionId)
            {
                double var14 = x - var13.x;
                double var16 = y - var13.y;
                double var18 = z - var13.z;
                if (var14 * var14 + var16 * var16 + var18 * var18 < range * range)
                {
                    var13.networkHandler.SendPacket(packet);
                }
            }
        }
    }

    public void Broadcast(string message)
    {
        ChatMessagePacket var2 = new(message);

        for (int var3 = 0; var3 < Players.Count; var3++)
        {
            ServerPlayerEntity var4 = Players[var3];
            if (IsOperator(var4.name))
            {
                var4.networkHandler.SendPacket(var2);
            }
        }
    }

    public bool SendPacket(string player, Packet packet)
    {
        ServerPlayerEntity var3 = GetPlayer(player);
        if (var3 != null)
        {
            var3.networkHandler.SendPacket(packet);
            return true;
        }
        else
        {
            return false;
        }
    }

    public void SavePlayers()
    {
        for (int var1 = 0; var1 < Players.Count; var1++)
        {
            _saveHandler.SavePlayerData(Players[var1]);
        }
    }

    public void UpdateBlockEntity(int x, int y, int z, BlockEntity blockentity)
    {
    }

    public void AddToWhitelist(string name)
    {
        Whitelist.Add(name);
        SaveWhitelist();
    }

    public void RemoveFromWhitelist(string name)
    {
        Whitelist.Remove(name);
        SaveWhitelist();
    }

    public HashSet<string> GetWhitelist()
    {
        return Whitelist;
    }

    public void ReloadWhitelist()
    {
        LoadWhitelist();
    }

    public void SendWorldInfo(ServerPlayerEntity player, ServerWorld world)
    {
        player.networkHandler.SendPacket(new WorldTimeUpdateS2CPacket(world.getTime()));
        if (world.isRaining())
        {
            player.networkHandler.SendPacket(new GameStateChangeS2CPacket(1));
        }
    }

    public void SendPlayerStatus(ServerPlayerEntity player)
    {
        player.onContentsUpdate(player.playerScreenHandler);
        player.markHealthDirty();
    }
}
