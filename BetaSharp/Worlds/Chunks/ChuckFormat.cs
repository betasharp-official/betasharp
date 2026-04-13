using BetaSharp.Util.Maths;

namespace BetaSharp.Worlds.Chunks;

public static class ChuckFormat
{
    /// <summary>
    /// Unlike <see cref="ChunkHeight"/>, WorldHeight can be changed without breaking the game or compatibility.
    /// </summary>
    public static int WorldHeight => 128;
    public static int ChunkHeight => 128;
    public static int ChunkSize => ChunkHeight * 16 * 16;
    public static int GetIndex(int x, int y, int z) => (x << 11) | (z << 7) | y;
    public static int GetIndex(int x, int z) => (x << 11) | (z << 7);
    public static Vec3i GetPos(int i) => new((i >> 11) & 15, i & 127, (i >> 7) & 15);
    public static int GetNibIndex(Vec3i pos) => GetIndex(pos.X, pos.Y, pos.Z) >> 1;
    public static int GetNibIndex(int x, int y, int z) => GetIndex(x, y, z) >> 1;
}
