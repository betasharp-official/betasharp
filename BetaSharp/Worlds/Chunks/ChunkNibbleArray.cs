using System.Runtime.CompilerServices;

namespace BetaSharp.Worlds.Chunks;

public readonly struct ChunkNibbleArray
{
    public readonly byte[] Bytes;

    public ChunkNibbleArray(int size)
    {
        Bytes = new byte[size >> 1];
    }

    public ChunkNibbleArray(byte[] bytes)
    {
        Bytes = bytes;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int GetNibble(int x, int y, int z)
    {
        int index = ChuckFormat.GetNibIndex(x, y, z);

        return (y & 1) == 0
            ? Bytes[index] & 0x0F
            : (Bytes[index] >> 4) & 0x0F;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetNibble(int x, int y, int z, int value)
    {
        int index = ChuckFormat.GetNibIndex(x, y, z);

        if ((y & 1) == 0)
        {
            Bytes[index] = (byte)((Bytes[index] & 0xF0) | (value & 0x0F));
        }
        else
        {
            Bytes[index] = (byte)((Bytes[index] & 0x0F) | ((value & 0x0F) << 4));
        }
    }

    public bool IsInitialized => Bytes != null;
}
