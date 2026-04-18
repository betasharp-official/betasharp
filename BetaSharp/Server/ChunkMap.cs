using BetaSharp.Blocks;
using BetaSharp.Blocks.Entities;
using BetaSharp.Entities;
using BetaSharp.Network.Packets;
using BetaSharp.Network.Packets.S2CPlay;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Chunks;
using BetaSharp.Worlds.Core;
using Microsoft.Extensions.Logging;

namespace BetaSharp.Server;

internal class ChunkMap
{
    public List<ServerPlayerEntity> players = [];
    private readonly Dictionary<long, TrackedChunk> _chunkMapping = new();
    private readonly List<TrackedChunk> _chunksToUpdate = [];
    public readonly ChunkLoadingQueue loadQueue;
    private BetaSharpServer _server;
    private readonly int _dimensionId;
    private int _viewDistance;
    private readonly ILogger<ChunkMap> _logger = Log.Instance.For<ChunkMap>();

    public ChunkMap(BetaSharpServer server, int dimensionId, int viewRadius)
    {
        if (viewRadius > 32)
        {
            throw new ArgumentException("Too big view Radius! Max is 32.", nameof(viewRadius));
        }
        if (viewRadius < 4)
        {
            throw new ArgumentException("Too small view Radius! Min is 4.", nameof(viewRadius));
        }

        _viewDistance = viewRadius;
        _server = server;
        _dimensionId = dimensionId;
        loadQueue = new ChunkLoadingQueue(this);
    }

    public ServerWorld getWorld()
    {
        return _server.getWorld(_dimensionId);
    }

    public void SetViewDistance(int newDistance)
    {
        int oldDistance = _viewDistance;
        _viewDistance = newDistance;

        if (newDistance < oldDistance)
        {
            // Unload chunks that are now out of view distance
            foreach (var player in players)
            {
                int px = (int)player.LastX >> 4;
                int pz = (int)player.LastZ >> 4;

                foreach (var item in GetChunks(player, oldDistance))
                {
                    if (isWithinViewDistance(item.x, item.z, px, pz))
                    {
                        continue;
                    }

                    if (GetOrCreateChunk(item.x, item.z, false) is TrackedChunk chunk)
                    {
                        chunk.removePlayer(player);
                    }
                    else
                    {
                        loadQueue.Remove(item.x, item.z, player);
                        player.CancelChunkSend(item);
                    }
                }
            }
        }
        else if (newDistance > oldDistance)
        {
            // Load chunks that are now within view distance
            foreach (ServerPlayerEntity player in players)
            {
                int px = (int)player.LastX >> 4;
                int pz = (int)player.LastZ >> 4;

                foreach (ChunkPos item in GetChunks(player))
                {
                    if (isWithinOldViewDistance(item.x, item.z, px, pz, oldDistance))
                    {
                        continue;
                    }

                    if (GetOrCreateChunk(item.x, item.z, false) is TrackedChunk chunk)
                    {
                        if (!chunk.HasPlayer(player))
                        {
                            chunk.addPlayer(player);
                        }
                    }
                    else
                    {
                        loadQueue.Add(item.x, item.z, player);
                    }
                }
            }
        }
    }

    private static bool isWithinOldViewDistance(int chunkX, int chunkZ, int centerX, int centerZ, int oldDist)
    {
        int dx = chunkX - centerX;
        int dz = chunkZ - centerZ;
        return dx >= -oldDist && dx <= oldDist && dz >= -oldDist && dz <= oldDist;
    }

    public void updateChunks()
    {
        foreach (TrackedChunk chunk in _chunksToUpdate)
        {
            chunk.updateChunk();
        }

        _chunksToUpdate.Clear();
        loadQueue.Tick();
    }

    public static long GetChunkHash(int chunkX, int chunkZ)
    {
        return (chunkX + 2147483647L) | ((chunkZ + 2147483647L) << 32);
    }

    internal TrackedChunk? GetOrCreateChunk(int chunkX, int chunkZ, bool createIfAbsent)
    {
        long chunkHash = GetChunkHash(chunkX, chunkZ);
        TrackedChunk? chunk = _chunkMapping.GetValueOrDefault(chunkHash);
        if (chunk == null && createIfAbsent)
        {
            chunk = new TrackedChunk(this, chunkX, chunkZ);
            _chunkMapping[chunkHash] = chunk;
        }

        return chunk;
    }

    public void markBlockForUpdate(int x, int y, int z)
    {
        int chunkX = x >> 4;
        int chunkZ = z >> 4;
        TrackedChunk? trackedChunk = GetOrCreateChunk(chunkX, chunkZ, false);
        trackedChunk?.updatePlayerChunks(x & 15, y, z & 15);
    }

    internal bool IsChunkTrackedAndSent(int chunkX, int chunkZ)
    {
        long key = GetChunkHash(chunkX, chunkZ);
        return _chunkMapping.TryGetValue(key, out TrackedChunk? trackedChunk) && trackedChunk != null && trackedChunk.HasPlayersAndHasBeenSent();
    }

    internal static bool HasPlayerReceivedChunkTerrain(ServerPlayerEntity player, int chunkX, int chunkZ)
    {
        var pos = new ChunkPos(chunkX, chunkZ);
        return player.ChunksTerrainSentToClient.ContainsKey(pos);
    }

    public void OnChunkDecorated(int chunkX, int chunkZ)
    {
        int[] dxs = [0, 1, 0, 1];
        int[] dzs = [0, 0, 1, 1];

        for (int c = 0; c < 4; c++)
        {
            int cx = chunkX + dxs[c];
            int cz = chunkZ + dzs[c];

            long key = GetChunkHash(cx, cz);
            if (!_chunkMapping.TryGetValue(key, out TrackedChunk? trackedChunk) || trackedChunk == null) continue;

            trackedChunk.updatePlayerChunks(0, 50, 0);
            trackedChunk.updatePlayerChunks(15, 124, 15);

            for (int i = 0; i < 8; i++)
            {
                trackedChunk.updatePlayerChunks(i, 64, i);
            }
        }
    }

    public void addPlayer(ServerPlayerEntity player)
    {
        player.ResetChunkStreamingState();
        player.LastX = player.X;
        player.LastZ = player.Z;

        foreach (ChunkPos item in GetChunks(player))
        {
            if (GetOrCreateChunk(item.X, item.Z, false) is { } centerChunk)
            {
                centerChunk.addPlayer(player);
            }
            else
            {
                loadQueue.Add(item.X, item.Z, player);
            }
        }

        players.Add(player);
    }

    public void removePlayer(ServerPlayerEntity player)
    {
        foreach (ChunkPos item in GetChunks(player))
        {
            TrackedChunk? chunk = GetOrCreateChunk(item.X, item.Z, false);
            chunk?.removePlayer(player);
        }

        players.Remove(player);
        loadQueue.RemovePlayer(player);
        player.ResetChunkStreamingState();
    }

    private bool isWithinViewDistance(int chunkX, int chunkZ, int centerX, int centerZ)
    {
        int deltaX = chunkX - centerX;
        int deltaZ = chunkZ - centerZ;
        return deltaX >= -_viewDistance && deltaX <= _viewDistance && deltaZ >= -_viewDistance && deltaZ <= _viewDistance;
    }

    public void updatePlayerChunks(ServerPlayerEntity player)
    {
        int playerChunkCenterX = (int)player.X >> 4;
        int playerChunkCenterZ = (int)player.Z >> 4;
        int playerLastChunkCenterX = (int)player.LastX >> 4;
        int playerLastChunkCenterZ = (int)player.LastZ >> 4;
        int playerChunkCenterDeltaX = playerChunkCenterX - playerLastChunkCenterX;
        int playerChunkCenterDeltaZ = playerChunkCenterZ - playerLastChunkCenterZ;
        if (playerChunkCenterDeltaX == 0 && playerChunkCenterDeltaZ == 0)
        {
            return;
        }

        player.UpdateChunkStreamingMotion(playerChunkCenterDeltaX, playerChunkCenterDeltaZ);

        for (int x = playerChunkCenterX - _viewDistance; x <= playerChunkCenterX + _viewDistance; x++)
        {
            for (int z = playerChunkCenterZ - _viewDistance; z <= playerChunkCenterZ + _viewDistance; z++)
            {
                if (!isWithinViewDistance(x, z, playerLastChunkCenterX, playerLastChunkCenterZ))
                {
                    if (GetOrCreateChunk(x, z, false) is { } chunk)
                    {
                        if (!chunk.HasPlayer(player))
                        {
                            chunk.addPlayer(player);
                        }
                    }
                    else
                    {
                        loadQueue.Add(x, z, player);
                    }
                }

                if (!isWithinViewDistance(x - playerChunkCenterDeltaX, z - playerChunkCenterDeltaZ, playerChunkCenterX, playerChunkCenterZ))
                {
                    int oldChunkX = x - playerChunkCenterDeltaX;
                    int oldChunkZ = z - playerChunkCenterDeltaZ;
                    if (GetOrCreateChunk(oldChunkX, oldChunkZ, false) is TrackedChunk chunk)
                    {
                        chunk.removePlayer(player);
                    }
                    else
                    {
                        ChunkPos oldChunkPos = new(oldChunkX, oldChunkZ);
                        loadQueue.Remove(oldChunkX, oldChunkZ, player);
                        player.CancelChunkSend(oldChunkPos);
                    }
                }
            }
        }

        player.LastX = player.X;
        player.LastZ = player.Z;
    }

    public int getBlockViewDistance()
    {
        return _viewDistance * 16 - 16;
    }

    private ReadOnlySpan<ChunkPos> GetChunks(ServerPlayerEntity player)
    {
        return GetChunks(player, _viewDistance);
    }

    private static ReadOnlySpan<ChunkPos> GetChunks(ServerPlayerEntity player, int radius)
    {
        int playerChunkX = (int)player.X >> 4;
        int playerChunkZ = (int)player.Z >> 4;
        int diameter = radius * 2 + 1;
        var chunks = new ChunkPos[diameter * diameter];
        int index = 0;

        chunks[index++] = new ChunkPos(playerChunkX, playerChunkZ);

        for (int currentRadius = 1; currentRadius <= radius; currentRadius++)
        {
            for (int dx = -currentRadius; dx <= currentRadius; dx++)
                chunks[index++] = new ChunkPos(playerChunkX + dx, playerChunkZ - currentRadius);

            for (int dz = -currentRadius + 1; dz <= currentRadius; dz++)
                chunks[index++] = new ChunkPos(playerChunkX + currentRadius, playerChunkZ + dz);

            for (int dx = currentRadius - 1; dx >= -currentRadius; dx--)
                chunks[index++] = new ChunkPos(playerChunkX + dx, playerChunkZ + currentRadius);

            for (int dz = currentRadius - 1; dz >= -currentRadius + 1; dz--)
                chunks[index++] = new ChunkPos(playerChunkX - currentRadius, playerChunkZ + dz);
        }

        return chunks;
    }

    internal class TrackedChunk
    {
        private const int MaxDirtyBlocks = 10;
        private readonly ILogger<TrackedChunk> _logger = Log.Instance.For<TrackedChunk>();
        private readonly ChunkMap _chunkMap;
        private readonly HashSet<ServerPlayerEntity> _players;
        private readonly ChunkPos _chunkPos;
        private readonly short[] _dirtyBlocks;
        private int _dirtyBlockCount;
        private int _dirtyBlockMinX;
        private int _dirtyBlockMinY;
        private int _dirtyBlockMinZ;
        private int _dirtyBlockMaxX;
        private int _dirtyBlockMaxY;
        private int _dirtyBlockMaxZ;
        private bool _hasBeenSent;

        public TrackedChunk(ChunkMap chunkMap, int chunkX, int chunkZ)
        {
            _chunkMap = chunkMap;
            _players = [];
            _dirtyBlocks = new short[MaxDirtyBlocks];
            _dirtyBlockCount = 0;
            _chunkPos = new ChunkPos(chunkX, chunkZ);
            chunkMap.getWorld().ChunkCache.LoadChunk(chunkX, chunkZ);
        }

        public bool HasPlayer(ServerPlayerEntity player) => _players.Contains(player);

        public void addPlayer(ServerPlayerEntity player)
        {
            if (!_players.Add(player))
            {
                return;
            }

            if (player.ActiveChunks.Add(_chunkPos))
            {
                player.NetworkHandler.SendPacket(ChunkStatusUpdateS2CPacket.Get(_chunkPos.X, _chunkPos.Z, true));
            }

            player.ScheduleChunkSend(_chunkPos);
        }

        public void EnqueueForTrackingPlayers()
        {
            if (_hasBeenSent)
            {
                return;
            }

            _hasBeenSent = true;

            foreach (var player in _players)
            {
                player.PendingChunkUpdates.Enqueue(_chunkPos);
            }
        }

        internal bool HasPlayersAndHasBeenSent() => _hasBeenSent && _players.Count > 0;

        public void removePlayer(ServerPlayerEntity player)
        {
            if (_players.Remove(player))
            {
                if (_players.Count == 0)
                {
                    long chunkHash = GetChunkHash(_chunkPos.X, _chunkPos.Z);
                    _chunkMap._chunkMapping.Remove(chunkHash);
                    if (_dirtyBlockCount > 0)
                    {
                        _chunkMap._chunksToUpdate.Remove(this);
                    }

                    _chunkMap.getWorld().ChunkCache.isLoaded(_chunkPos.X, _chunkPos.Z);
                }

                if (player.ActiveChunks.Remove(_chunkPos))
                {
                    player.NetworkHandler.SendPacket(ChunkStatusUpdateS2CPacket.Get(_chunkPos.X, _chunkPos.Z, false));
                }

                player.CancelChunkSend(_chunkPos);
            }
        }

        public void updatePlayerChunks(int x, int y, int z)
        {
            if (_dirtyBlockCount == 0)
            {
                _chunkMap._chunksToUpdate.Add(this);
                _dirtyBlockMinX = _dirtyBlockMaxX = x;
                _dirtyBlockMinY = _dirtyBlockMaxY = y;
                _dirtyBlockMinZ = _dirtyBlockMaxZ = z;
            }

            if (_dirtyBlockMinX > x)
            {
                _dirtyBlockMinX = x;
            }

            if (_dirtyBlockMinY > y)
            {
                _dirtyBlockMinY = y;
            }

            if (_dirtyBlockMinZ > z)
            {
                _dirtyBlockMinZ = z;
            }

            if (_dirtyBlockMaxX < x)
            {
                _dirtyBlockMaxX = x;
            }

            if (_dirtyBlockMaxY < y)
            {
                _dirtyBlockMaxY = y;
            }

            if (_dirtyBlockMaxZ < z)
            {
                _dirtyBlockMaxZ = z;
            }

            if (_dirtyBlockCount < MaxDirtyBlocks)
            {
                short blockArrayIndex = (short)(ChuckFormat.GetIndex(x, y, z));

                for (int i = 0; i < _dirtyBlockCount; i++)
                {
                    if (_dirtyBlocks[i] == blockArrayIndex)
                    {
                        return;
                    }
                }

                _dirtyBlocks[_dirtyBlockCount++] = blockArrayIndex;
            }
        }

        public void sendPacketToPlayers(Packet packet)
        {
            foreach (ServerPlayerEntity serverPlayer in _players)
            {
                if (serverPlayer.ActiveChunks.Contains(_chunkPos))
                {
                    serverPlayer.NetworkHandler.SendPacket(packet);
                }
            }
            packet.Return();
        }

        public void updateChunk()
        {
            ServerWorld sWorld = _chunkMap.getWorld();
            if (_dirtyBlockCount != 0)
            {
                if (_dirtyBlockCount == 1)
                {
                    int worldX = _chunkPos.X * 16 + _dirtyBlockMinX;
                    int worldY = _dirtyBlockMinY;
                    int worldZ = _chunkPos.Z * 16 + _dirtyBlockMinZ;
                    sendPacketToPlayers(BlockUpdateS2CPacket.Get(worldX, worldY, worldZ, sWorld));
                    if (Block.BlocksWithEntity[sWorld.Reader.GetBlockId(worldX, worldY, worldZ)])
                    {
                        sendBlockEntityUpdate(sWorld.Entities.GetBlockEntity<BlockEntity>(worldX, worldY, worldZ));
                    }
                }
                else if (_dirtyBlockCount == MaxDirtyBlocks)
                {
                    _dirtyBlockMinY = _dirtyBlockMinY / 2 * 2;
                    _dirtyBlockMaxY = (_dirtyBlockMaxY / 2 + 1) * 2;
                    int worldX = _dirtyBlockMinX + _chunkPos.X * 16;
                    int worldY = _dirtyBlockMinY;
                    int worldZ = _dirtyBlockMinZ + _chunkPos.Z * 16;
                    int sizeX = _dirtyBlockMaxX - _dirtyBlockMinX + 1;
                    int sizeY = _dirtyBlockMaxY - _dirtyBlockMinY + 2;
                    int sizeZ = _dirtyBlockMaxZ - _dirtyBlockMinZ + 1;
                    sendPacketToPlayers(ChunkDataS2CPacket.Get(worldX, worldY, worldZ, sizeX, sizeY, sizeZ, sWorld));
                    List<BlockEntity> blockEntities = sWorld.getBlockEntities(worldX, worldY, worldZ, worldX + sizeX, worldY + sizeY, worldZ + sizeZ);

                    for (int i = 0; i < blockEntities.Count; i++)
                    {
                        sendBlockEntityUpdate(blockEntities[i]);
                    }
                }
                else
                {
                    sendPacketToPlayers(ChunkDeltaUpdateS2CPacket.Get(_chunkPos.X, _chunkPos.Z, _dirtyBlocks, _dirtyBlockCount, sWorld));

                    for (int i = 0; i < _dirtyBlockCount; i++)
                    {
                        int worldX = _chunkPos.X * 16 + (_dirtyBlocks[i] >> 12 & 15);
                        int worldY = _dirtyBlocks[i] & 0xFF;
                        int worldZ = _chunkPos.Z * 16 + (_dirtyBlocks[i] >> 8 & 15);
                        if (Block.BlocksWithEntity[sWorld.Reader.GetBlockId(worldX, worldY, worldZ)])
                        {
                            sendBlockEntityUpdate(sWorld.Entities.GetBlockEntity<BlockEntity>(worldX, worldY, worldZ));
                        }
                    }
                }

                _dirtyBlockCount = 0;
            }
        }

        private void sendBlockEntityUpdate(BlockEntity? blockentity)
        {
            if (blockentity != null)
            {
                Packet packet = blockentity.createUpdatePacket();
                if (packet != null)
                {
                    sendPacketToPlayers(packet);
                }
            }
        }
    }
}
