using System.Runtime.CompilerServices;

namespace BetaSharp.Worlds.Chunks;

public class ChunkNibbleArray
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
        int index = (x << 11) | (z << 7) | y;
        int byteIndex = index >> 1;

        return (index & 1) == 0
            ? Bytes[byteIndex] & 0x0F
            : (Bytes[byteIndex] >> 4) & 0x0F;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetNibble(int x, int y, int z, int value)
    {
        int index = (x << 11) | (z << 7) | y;
        int byteIndex = index >> 1;

        if ((index & 1) == 0)
        {
            Bytes[byteIndex] = (byte)((Bytes[byteIndex] & 0xF0) | (value & 0x0F));
        }
        else
        {
            Bytes[byteIndex] = (byte)((Bytes[byteIndex] & 0x0F) | ((value & 0x0F) << 4));
        }
    }

    public bool IsInitialized => Bytes != null;
}
