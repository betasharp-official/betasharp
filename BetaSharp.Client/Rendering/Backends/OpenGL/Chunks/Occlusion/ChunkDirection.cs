namespace BetaSharp.Client.Rendering.Chunks.Occlusion;

public enum ChunkDirection
{
    Down = 0,
    Up = 1,
    North = 2,
    South = 3,
    West = 4,
    East = 5
}

public static class ChunkDirectionExtensions
{
    public const int Count = 6;

    public static ChunkDirection Opposite(this ChunkDirection direction)
    {
        return direction switch
        {
            ChunkDirection.Down => ChunkDirection.Up,
            ChunkDirection.Up => ChunkDirection.Down,
            ChunkDirection.North => ChunkDirection.South,
            ChunkDirection.South => ChunkDirection.North,
            ChunkDirection.West => ChunkDirection.East,
            ChunkDirection.East => ChunkDirection.West,
            _ => throw new ArgumentOutOfRangeException(nameof(direction))
        };
    }

    public static int GetX(this ChunkDirection direction)
    {
        return direction switch
        {
            ChunkDirection.West => -1,
            ChunkDirection.East => 1,
            _ => 0
        };
    }

    public static int GetY(this ChunkDirection direction)
    {
        return direction switch
        {
            ChunkDirection.Down => -1,
            ChunkDirection.Up => 1,
            _ => 0
        };
    }

    public static int GetZ(this ChunkDirection direction)
    {
        return direction switch
        {
            ChunkDirection.North => -1,
            ChunkDirection.South => 1,
            _ => 0
        };
    }
}

[Flags]
public enum ChunkDirectionMask
{
    None = 0,
    Down = 1 << ChunkDirection.Down,
    Up = 1 << ChunkDirection.Up,
    North = 1 << ChunkDirection.North,
    South = 1 << ChunkDirection.South,
    West = 1 << ChunkDirection.West,
    East = 1 << ChunkDirection.East,
    All = (1 << ChunkDirectionExtensions.Count) - 1
}
