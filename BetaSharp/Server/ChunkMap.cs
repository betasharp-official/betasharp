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
    public List<ServerPlayerEntity> Players = [];
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

    public ServerWorld GetWorld()
    {
        return server.GetWorld(dimensionId);
    }

    public void SetViewDistance(int newDistance)
    {
        int oldDistance = viewDistance;
        viewDistance = newDistance;

        if (newDistance < oldDistance)
        {
            // Unload chunks that are now out of view distance
            for (int i = 0; i < Players.Count; i++)
            {
                ServerPlayerEntity player = Players[i];
                int px = (int)player.lastX >> 4;
                int pz = (int)player.lastZ >> 4;

                for (int cx = px - oldDistance; cx <= px + oldDistance; cx++)
                {
                    for (int cz = pz - oldDistance; cz <= pz + oldDistance; cz++)
                    {
                        if (!isWithinViewDistance(cx, cz, px, pz))
                        {
                            TrackedChunk chunk = GetOrCreateChunk(cx, cz, false);
                            chunk?.RemovePlayer(player);
                        }
                    }
                }
            }
        }
        else if (newDistance > oldDistance)
        {
            // Load chunks that are now within view distance
            for (int i = 0; i < Players.Count; i++)
            {
                ServerPlayerEntity player = Players[i];
                int px = (int)player.lastX >> 4;
                int pz = (int)player.lastZ >> 4;

                for (int cx = px - newDistance; cx <= px + newDistance; cx++)
                {
                    for (int cz = pz - newDistance; cz <= pz + newDistance; cz++)
                    {
                        if (!isWithinOldViewDistance(cx, cz, px, pz, oldDistance))
                        {
                            if (GetOrCreateChunk(cx, cz, false) is TrackedChunk chunk)
                            {
                                if (!chunk.HasPlayer(player))
                                {
                                    chunk.AddPlayer(player);
                                }
                            }
                            else
                            {
                                loadQueue.Add(cx, cz, player);
                            }
                        }
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

    public void UpdateChunks()
    {
        for (int var1 = 0; var1 < chunksToUpdate.Count; var1++)
        {
            chunksToUpdate[var1].UpdateChunk();
        }

        chunksToUpdate.Clear();
        loadQueue.Tick();
    }

    public static long GetChunkHash(int chunkX, int chunkZ)
    {
        return chunkX + 2147483647L | chunkZ + 2147483647L << 32;
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

    public void MarkBlockForUpdate(int x, int y, int z)
    {
        int var4 = x >> 4;
        int var5 = z >> 4;
        TrackedChunk var6 = GetOrCreateChunk(var4, var5, false);
        if (var6 != null)
        {
            var6.UpdatePlayerChunks(x & 15, y, z & 15);
        }
    }

    public void AddPlayer(ServerPlayerEntity player)
    {
        int var2 = (int)player.x >> 4;
        int var3 = (int)player.z >> 4;
        player.lastX = player.x;
        player.lastZ = player.z;
        int var4 = 0;
        int var5 = viewDistance;
        int var6 = 0;
        int var7 = 0;
        if (GetOrCreateChunk(var2, var3, false) is TrackedChunk centerChunk)
        {
            centerChunk.AddPlayer(player);
        }
        else
        {
            loadQueue.Add(var2, var3, player);
        }

        for (int var8 = 1; var8 <= var5 * 2; var8++)
        {
            for (int var9 = 0; var9 < 2; var9++)
            {
                int[] var10 = DIRECTIONS[var4++ % 4];

                for (int var11 = 0; var11 < var8; var11++)
                {
                    var6 += var10[0];
                    var7 += var10[1];
                    if (GetOrCreateChunk(var2 + var6, var3 + var7, false) is TrackedChunk chunk)
                    {
                        if (!chunk.HasPlayer(player))
                        {
                            chunk.AddPlayer(player);
                        }
                    }
                    else
                    {
                        loadQueue.Add(var2 + var6, var3 + var7, player);
                    }
                }
            }
        }

        var4 %= 4;

        for (int var13 = 0; var13 < var5 * 2; var13++)
        {
            var6 += DIRECTIONS[var4][0];
            var7 += DIRECTIONS[var4][1];
            if (GetOrCreateChunk(var2 + var6, var3 + var7, false) is TrackedChunk chunk)
            {
                if (!chunk.HasPlayer(player))
                {
                    chunk.AddPlayer(player);
                }
            }
            else
            {
                loadQueue.Add(var2 + var6, var3 + var7, player);
            }
        }

        Players.Add(player);
    }

    public void RemovePlayer(ServerPlayerEntity player)
    {
        int var2 = (int)player.lastX >> 4;
        int var3 = (int)player.lastZ >> 4;

        for (int var4 = var2 - viewDistance; var4 <= var2 + viewDistance; var4++)
        {
            for (int var5 = var3 - viewDistance; var5 <= var3 + viewDistance; var5++)
            {
                TrackedChunk var6 = GetOrCreateChunk(var4, var5, false);
                var6?.RemovePlayer(player);
            }
        }

        Players.Remove(player);
        loadQueue.RemovePlayer(player);
    }

    private bool isWithinViewDistance(int chunkX, int chunkZ, int centerX, int centerZ)
    {
        int var5 = chunkX - centerX;
        int var6 = chunkZ - centerZ;
        return var5 >= -viewDistance && var5 <= viewDistance && var6 >= -viewDistance && var6 <= viewDistance;
    }

    public void UpdatePlayerChunks(ServerPlayerEntity player)
    {
        int var2 = (int)player.x >> 4;
        int var3 = (int)player.z >> 4;
        double var4 = player.lastX - player.x;
        double var6 = player.lastZ - player.z;
        double var8 = var4 * var4 + var6 * var6;
        if (!(var8 < 64.0))
        {
            int var10 = (int)player.lastX >> 4;
            int var11 = (int)player.lastZ >> 4;
            int var12 = var2 - var10;
            int var13 = var3 - var11;
            if (var12 != 0 || var13 != 0)
            {
                for (int var14 = var2 - viewDistance; var14 <= var2 + viewDistance; var14++)
                {
                    for (int var15 = var3 - viewDistance; var15 <= var3 + viewDistance; var15++)
                    {
                        if (!isWithinViewDistance(var14, var15, var10, var11))
                        {
                            if (GetOrCreateChunk(var14, var15, false) is TrackedChunk chunk)
                            {
                                if (!chunk.HasPlayer(player))
                                {
                                    chunk.AddPlayer(player);
                                }
                            }
                            else
                            {
                                loadQueue.Add(var14, var15, player);
                            }
                        }

                        if (!isWithinViewDistance(var14 - var12, var15 - var13, var2, var3))
                        {
                            TrackedChunk var16 = GetOrCreateChunk(var14 - var12, var15 - var13, false);
                            var16?.RemovePlayer(player);
                        }
                    }
                }

                player.lastX = player.x;
                player.lastZ = player.z;
            }
        }
    }

    public int GetBlockViewDistance()
    {
        return viewDistance * 16 - 16;
    }

    internal class TrackedChunk
    {
        private readonly ILogger<TrackedChunk> _logger = Log.Instance.For<TrackedChunk>();
        private readonly ChunkMap chunkMap;
        private readonly List<ServerPlayerEntity> players;
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
            chunkMap.GetWorld().chunkCache.LoadChunk(chunkX, chunkZ);
        }

        public bool HasPlayer(ServerPlayerEntity player) => players.Contains(player);

        public void AddPlayer(ServerPlayerEntity player)
        {
            if (players.Contains(player))
            {
                return;
            }

            if (player.activeChunks.Add(chunkPos))
            {
                player.networkHandler.SendPacket(new ChunkStatusUpdateS2CPacket(chunkPos.X, chunkPos.Z, true));
            }

            players.Add(player);
            player.PendingChunkUpdates.Enqueue(chunkPos);
        }

        public void RemovePlayer(ServerPlayerEntity player)
        {
            if (players.Contains(player))
            {
                players.Remove(player);
                if (players.Count == 0)
                {
                    long var2 = chunkX + 2147483647L | chunkZ + 2147483647L << 32;
                    chunkMap.chunkMapping.Remove(var2);
                    if (dirtyBlockCount > 0)
                    {
                        chunkMap.chunksToUpdate.Remove(this);
                    }

                    chunkMap.GetWorld().chunkCache.QueueUnloadCheck(chunkX, chunkZ);
                }

                if (player.activeChunks.Remove(chunkPos))
                {
                    player.networkHandler.SendPacket(new ChunkStatusUpdateS2CPacket(chunkX, chunkZ, false));
                }
            }
        }

        public void UpdatePlayerChunks(int x, int y, int z)
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

        public void SendPacketToPlayers(Packet packet)
        {
            for (int var2 = 0; var2 < players.Count; var2++)
            {
                ServerPlayerEntity var3 = players[var2];
                if (var3.activeChunks.Contains(chunkPos))
                {
                    var3.networkHandler.SendPacket(packet);
                }
            }
        }

        public void UpdateChunk()
        {
            ServerWorld var1 = chunkMap.GetWorld();
            if (dirtyBlockCount != 0)
            {
                if (dirtyBlockCount == 1)
                {
                    int var2 = chunkX * 16 + minX;
                    int var3 = minZ;
                    int var4 = chunkZ * 16 + maxY;
                    SendPacketToPlayers(new BlockUpdateS2CPacket(var2, var3, var4, var1));
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
                    SendPacketToPlayers(new ChunkDataS2CPacket(var10, var12, var14, var5, var6, var7, var1));
                    var var8 = var1.getBlockEntities(var10, var12, var14, var10 + var5, var12 + var6, var14 + var7);

                    for (int var9 = 0; var9 < var8.Count; var9++)
                    {
                        sendBlockEntityUpdate(var8[var9]);
                    }
                }
                else
                {
                    SendPacketToPlayers(new ChunkDeltaUpdateS2CPacket(chunkX, chunkZ, dirtyBlocks, dirtyBlockCount, var1));

                    for (int var11 = 0; var11 < dirtyBlockCount; var11++)
                    {
                        int var13 = chunkX * 16 + (dirtyBlockCount >> 12 & 15);
                        int var15 = dirtyBlockCount & 0xFF;
                        int var16 = chunkZ * 16 + (dirtyBlockCount >> 8 & 15);
                        if (Block.BlocksWithEntity[var1.getBlockId(var13, var15, var16)])
                        {
                            _logger.LogInformation("Sending!");
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
                    SendPacketToPlayers(var2);
                }
            }
        }
    }
}
