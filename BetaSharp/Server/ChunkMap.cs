using BetaSharp.Blocks;
using BetaSharp.Blocks.Entities;
using BetaSharp.Entities;
using BetaSharp.Network.Packets;
using BetaSharp.Network.Packets.S2CPlay;
using BetaSharp.Util;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds;
using Microsoft.Extensions.Logging;

namespace BetaSharp.Server;

internal class ChunkMap
{
    public List<ServerPlayerEntity> players = [];
    private readonly Dictionary<long, TrackedChunk> chunkMapping = new();
    private readonly List<TrackedChunk> chunksToUpdate = [];
    public readonly ChunkLoadingQueue loadQueue;
    private MinecraftServer server;
    private readonly int dimensionId;
    private int viewDistance;
    private readonly int[][] DIRECTIONS = [[1, 0], [0, 1], [-1, 0], [0, -1]];
    private readonly ILogger<ChunkMap> _logger = Log.Instance.For<ChunkMap>();

    public ChunkMap(MinecraftServer server, int dimensionId, int viewRadius)
    {
        if (viewRadius > 32)
        {
            throw new ArgumentException("Too big view radius! Max is 32.", nameof(viewRadius));
        }
        if (viewRadius < 4)
        {
            throw new ArgumentException("Too small view radius! Min is 4.", nameof(viewRadius));
        }

        viewDistance = viewRadius;
        this.server = server;
        this.dimensionId = dimensionId;
        loadQueue = new ChunkLoadingQueue(this);
    }

    public ServerWorld getWorld()
    {
        return server.getWorld(dimensionId);
    }

    public void SetViewDistance(int newDistance)
    {
        int oldDistance = viewDistance;
        viewDistance = newDistance;

        if (newDistance < oldDistance)
        {
            // Unload chunks that are now out of view distance
            foreach (var player in players)
            {
                int px = (int)player.lastX >> 4;
                int pz = (int)player.lastZ >> 4;

                foreach (var item in GetChunks(player))
                {
                    if (isWithinViewDistance(item.x, item.z, px, pz))
                    {
                        continue;
                    }

                    TrackedChunk chunk = GetOrCreateChunk(item.x, item.z, false);
                    chunk?.removePlayer(player);
                }
            }
        }
        else if (newDistance > oldDistance)
        {
            // Load chunks that are now within view distance
            foreach (var player in players)
            {
                int px = (int)player.lastX >> 4;
                int pz = (int)player.lastZ >> 4;

                foreach (var item in GetChunks(player))
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

    private bool isWithinOldViewDistance(int chunkX, int chunkZ, int centerX, int centerZ, int oldDist)
    {
        int dx = chunkX - centerX;
        int dz = chunkZ - centerZ;
        return dx >= -oldDist && dx <= oldDist && dz >= -oldDist && dz <= oldDist;
    }

    public void updateChunks()
    {
        foreach (var chunk in chunksToUpdate)
        {
            chunk.updateChunk();
        }

        chunksToUpdate.Clear();
        loadQueue.Tick();
    }

    public static long GetChunkHash(int chunkX, int chunkZ)
    {
        return (chunkX + 2147483647L) | ((chunkZ + 2147483647L) << 32);
    }

    internal TrackedChunk GetOrCreateChunk(int chunkX, int chunkZ, bool createIfAbsent)
    {
        long var4 = GetChunkHash(chunkX, chunkZ);
        TrackedChunk var6 = chunkMapping.GetValueOrDefault(var4);
        if (var6 == null && createIfAbsent)
        {
            var6 = new TrackedChunk(this, chunkX, chunkZ);
            chunkMapping[var4] = var6;
        }

        return var6;
    }

    public void markBlockForUpdate(int x, int y, int z)
    {
        int var4 = x >> 4;
        int var5 = z >> 4;
        TrackedChunk var6 = GetOrCreateChunk(var4, var5, false);
        if (var6 != null)
        {
            var6.updatePlayerChunks(x & 15, y, z & 15);
        }
    }

    public void addPlayer(ServerPlayerEntity player)
    {
        player.lastX = player.x;
        player.lastZ = player.z;

        foreach (var item in GetChunks(player))
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
        foreach (var item in GetChunks(player))
        {
            var chunk = GetOrCreateChunk(item.X, item.Z, false);
            chunk?.removePlayer(player);
        }

        players.Remove(player);
        loadQueue.RemovePlayer(player);
    }

    private bool isWithinViewDistance(int chunkX, int chunkZ, int centerX, int centerZ)
    {
        int var5 = chunkX - centerX;
        int var6 = chunkZ - centerZ;
        return var5 >= -viewDistance && var5 <= viewDistance && var6 >= -viewDistance && var6 <= viewDistance;
    }

    public void updatePlayerChunks(ServerPlayerEntity player)
    {
        int playerChunkCenterX = (int)player.x >> 4;
        int playerChunkCenterZ = (int)player.z >> 4;
        double playerDeltaX = player.lastX - player.x;
        double playerDeltaZ = player.lastZ - player.z;
        double playerDeltaSquared = playerDeltaX * playerDeltaX + playerDeltaZ * playerDeltaZ;
        if (playerDeltaSquared < 64.0)
        {
            return;
        }

        int playerLastChunkCenterX = (int)player.lastX >> 4;
        int playerLastChunkCenterZ = (int)player.lastZ >> 4;
        int playerChunkCenterDeltaX = playerChunkCenterX - playerLastChunkCenterX;
        int playerChunkCenterDeltaZ = playerChunkCenterZ - playerLastChunkCenterZ;
        if (playerChunkCenterDeltaX == 0 && playerChunkCenterDeltaZ == 0)
        {
            return;
        }

        for (int x = playerChunkCenterX - viewDistance; x <= playerChunkCenterX + viewDistance; x++)
        {
            for (int z = playerChunkCenterZ - viewDistance; z <= playerChunkCenterZ + viewDistance; z++)
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
                    TrackedChunk chunk = GetOrCreateChunk(x - playerChunkCenterDeltaX, z - playerChunkCenterDeltaZ, false);
                    chunk?.removePlayer(player);
                }
            }
        }

        player.lastX = player.x;
        player.lastZ = player.z;
    }

    public int getBlockViewDistance()
    {
        return viewDistance * 16 - 16;
    }

    private ReadOnlySpan<ChunkPos> GetChunks(ServerPlayerEntity player)
    {
        int playerChunkX = (int)player.x >> 4;
        int playerChunkZ = (int)player.z >> 4;
        int diameter = viewDistance * 2 + 1;
        var chunks = new ChunkPos[diameter * diameter];
        int index = 0;

        chunks[index++] = new ChunkPos(playerChunkX, playerChunkZ);

        for (int radius = 1; radius <= viewDistance; radius++)
        {
            for (int dx = -radius; dx <= radius; dx++)
                chunks[index++] = new ChunkPos(playerChunkX + dx, playerChunkZ - radius);

            for (int dz = -radius + 1; dz <= radius; dz++)
                chunks[index++] = new ChunkPos(playerChunkX + radius, playerChunkZ + dz);

            for (int dx = radius - 1; dx >= -radius; dx--)
                chunks[index++] = new ChunkPos(playerChunkX + dx, playerChunkZ + radius);

            for (int dz = radius - 1; dz >= -radius + 1; dz--)
                chunks[index++] = new ChunkPos(playerChunkX - radius, playerChunkZ + dz);
        }

        return chunks;
    }

    internal class TrackedChunk
    {
        private readonly ILogger<TrackedChunk> _logger = Log.Instance.For<TrackedChunk>();
        private readonly ChunkMap chunkMap;
        private readonly HashSet<ServerPlayerEntity> players;
        private readonly int chunkX;
        private readonly int chunkZ;
        private readonly ChunkPos chunkPos;
        private readonly short[] dirtyBlocks;
        private int dirtyBlockCount;
        private int minX;
        private int minY;
        private int minZ;
        private int maxX;
        private int maxY;
        private int maxZ;

        public TrackedChunk(ChunkMap chunkMap, int chunkX, int chunkZ)
        {
            this.chunkMap = chunkMap;
            players = [];
            dirtyBlocks = new short[10];
            dirtyBlockCount = 0;
            this.chunkX = chunkX;
            this.chunkZ = chunkZ;
            chunkPos = new ChunkPos(chunkX, chunkZ);
            chunkMap.getWorld().chunkCache.LoadChunk(chunkX, chunkZ);
        }

        public bool HasPlayer(ServerPlayerEntity player) => players.Contains(player);

        public void addPlayer(ServerPlayerEntity player)
        {
            if (!players.Add(player))
            {
                return;
            }

            if (player.activeChunks.Add(chunkPos))
            {
                player.networkHandler.sendPacket(new ChunkStatusUpdateS2CPacket(chunkPos.X, chunkPos.Z, true));
            }

            player.PendingChunkUpdates.Enqueue(chunkPos);
        }

        public void removePlayer(ServerPlayerEntity player)
        {
            if (players.Remove(player))
            {
                if (players.Count == 0)
                {
                    long var2 = ChunkMap.GetChunkHash(chunkX, chunkZ);
                    chunkMap.chunkMapping.Remove(var2);
                    if (dirtyBlockCount > 0)
                    {
                        chunkMap.chunksToUpdate.Remove(this);
                    }

                    chunkMap.getWorld().chunkCache.isLoaded(chunkX, chunkZ);
                }

                if (player.activeChunks.Remove(chunkPos))
                {
                    player.networkHandler.sendPacket(new ChunkStatusUpdateS2CPacket(chunkX, chunkZ, false));
                }
            }
        }

        public void updatePlayerChunks(int x, int y, int z)
        {
            if (dirtyBlockCount == 0)
            {
                chunkMap.chunksToUpdate.Add(this);
                minX = minY = x;
                minZ = maxX = y;
                maxY = maxZ = z;
            }

            if (minX > x)
            {
                minX = x;
            }

            if (minY < x)
            {
                minY = x;
            }

            if (minZ > y)
            {
                minZ = y;
            }

            if (maxX < y)
            {
                maxX = y;
            }

            if (maxY > z)
            {
                maxY = z;
            }

            if (maxZ < z)
            {
                maxZ = z;
            }

            if (dirtyBlockCount < 10)
            {
                short var4 = (short)(x << 12 | z << 8 | y);

                for (int var5 = 0; var5 < dirtyBlockCount; var5++)
                {
                    if (dirtyBlocks[var5] == var4)
                    {
                        return;
                    }
                }

                dirtyBlocks[dirtyBlockCount++] = var4;
            }
        }

        public void sendPacketToPlayers(Packet packet)
        {
            foreach (var var3 in players)
            {
                if (var3.activeChunks.Contains(chunkPos))
                {
                    var3.networkHandler.sendPacket(packet);
                }
            }
        }

        public void updateChunk()
        {
            ServerWorld var1 = chunkMap.getWorld();
            if (dirtyBlockCount != 0)
            {
                if (dirtyBlockCount == 1)
                {
                    int var2 = chunkX * 16 + minX;
                    int var3 = minZ;
                    int var4 = chunkZ * 16 + maxY;
                    sendPacketToPlayers(new BlockUpdateS2CPacket(var2, var3, var4, var1));
                    if (Block.BlocksWithEntity[var1.getBlockId(var2, var3, var4)])
                    {
                        sendBlockEntityUpdate(var1.getBlockEntity(var2, var3, var4));
                    }
                }
                else if (dirtyBlockCount == 10)
                {
                    minZ = minZ / 2 * 2;
                    maxX = (maxX / 2 + 1) * 2;
                    int var10 = minX + chunkX * 16;
                    int var12 = minZ;
                    int var14 = maxY + chunkZ * 16;
                    int var5 = minY - minX + 1;
                    int var6 = maxX - minZ + 2;
                    int var7 = maxZ - maxY + 1;
                    sendPacketToPlayers(new ChunkDataS2CPacket(var10, var12, var14, var5, var6, var7, var1));
                    var var8 = var1.getBlockEntities(var10, var12, var14, var10 + var5, var12 + var6, var14 + var7);

                    for (int var9 = 0; var9 < var8.Count; var9++)
                    {
                        sendBlockEntityUpdate(var8[var9]);
                    }
                }
                else
                {
                    sendPacketToPlayers(new ChunkDeltaUpdateS2CPacket(chunkX, chunkZ, dirtyBlocks, dirtyBlockCount, var1));

                    for (int var11 = 0; var11 < dirtyBlockCount; var11++)
                    {
                        int var13 = chunkX * 16 + (dirtyBlocks[var11] >> 12 & 15);
                        int var15 = dirtyBlocks[var11] & 0xFF;
                        int var16 = chunkZ * 16 + (dirtyBlocks[var11] >> 8 & 15);
                        if (Block.BlocksWithEntity[var1.getBlockId(var13, var15, var16)])
                        {
                            sendBlockEntityUpdate(var1.getBlockEntity(var13, var15, var16));
                        }
                    }
                }

                dirtyBlockCount = 0;
            }
        }

        private void sendBlockEntityUpdate(BlockEntity blockentity)
        {
            if (blockentity != null)
            {
                Packet var2 = blockentity.createUpdatePacket();
                if (var2 != null)
                {
                    sendPacketToPlayers(var2);
                }
            }
        }
    }
}
