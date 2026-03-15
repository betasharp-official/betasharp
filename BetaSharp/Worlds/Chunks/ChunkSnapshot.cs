using System.Buffers;
using BetaSharp.Blocks.Entities;
using BetaSharp.NBT;
using BetaSharp.Util.Maths;

namespace BetaSharp.Worlds.Chunks;

internal class ChunkSnapshot : IDisposable
{
    public ChunkNibbleArray SkylightMap { get; private set; }
    public ChunkNibbleArray BlocklightMap { get; private set; }

    private readonly byte[] _blocks;
    private readonly ChunkNibbleArray _data;
    private readonly Dictionary<int, NBTTagCompound> _tileEntities = [];
    private bool _disposed;
    private bool _isLit;

    public ChunkSnapshot(Chunk toSnapshot)
    {
        _blocks = ArrayPool<byte>.Shared.Rent(32768);
        Buffer.BlockCopy(toSnapshot.Blocks, 0, _blocks, 0, toSnapshot.Blocks.Length);
        
        _data = createNibbleArray(toSnapshot.Meta.Bytes);
        SkylightMap = createNibbleArray(toSnapshot.SkyLight.Bytes);
        BlocklightMap = createNibbleArray(toSnapshot.BlockLight.Bytes);

        foreach (KeyValuePair<BlockPos, BlockEntity> entry in toSnapshot.BlockEntities)
        {
            BlockEntity entity = entry.Value;
            BlockPos pos = entry.Key;

            if (pos.y < 0 || pos.y >= 128) continue;

            NBTTagCompound nbt = new();
            entity.writeNbt(nbt);

            int localX = pos.x & 15;
            int localZ = pos.z & 15;
            int localY = pos.y;

            int index = localX << 11 | localZ << 7 | localY;
            _tileEntities[index] = nbt;
        }
    }

    private static ChunkNibbleArray createNibbleArray(byte[] toCopy)
    {
        byte[] bytes = ArrayPool<byte>.Shared.Rent(toCopy.Length);
        Buffer.BlockCopy(toCopy, 0, bytes, 0, toCopy.Length);
        return new(bytes);
    }

    public int getBlockID(int x, int y, int z)
    {
        return _blocks[x << 11 | z << 7 | y] & 255;
    }

    public int getBlockMetadata(int x, int y, int z)
    {
        return _data.GetNibble(x, y, z);
    }

    public int getBlockLightValue(int x, int y, int z, int ambientDarkness)
    {
        int skyLight = SkylightMap.GetNibble(x, y, z);
        if (skyLight > 0)
        {
            _isLit = true;
        }

        skyLight -= ambientDarkness;
        int blockLight = BlocklightMap.GetNibble(x, y, z);
        if (blockLight > skyLight)
        {
            skyLight = blockLight;
        }

        return skyLight;
    }

    public bool getIsLit()
    {
        return _isLit;
    }

    public NBTTagCompound? GetTileEntityNbt(int x, int y, int z)
    {
        int index = x << 11 | z << 7 | y;
        return _tileEntities.TryGetValue(index, out NBTTagCompound? nbt) ? nbt : null;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        GC.SuppressFinalize(this);
        ArrayPool<byte>.Shared.Return(_blocks);
        ArrayPool<byte>.Shared.Return(_data.Bytes);
        ArrayPool<byte>.Shared.Return(SkylightMap.Bytes);
        ArrayPool<byte>.Shared.Return(BlocklightMap.Bytes);
        _disposed = true;
    }
}
