namespace BetaSharp.Blocks;

public enum Side : byte
{
    Down = 0,
    Up = 1,
    North = 2,
    South = 3,
    West = 4,
    East = 5
}

public static class SideExtensions
{
    public static bool IsValidSide(this Side v) => (byte)v <= 5;
    public static Side ToSide(this int v) => ((Side)v).IsValidSide() ? (Side)v : throw new ArgumentException("Invalid side");
    public static int ToInt(this Side s) => (int)s;
    public static Side OppositeFace(this Side side) => side switch
    {
        Side.Down => Side.Up,
        Side.Up => Side.Down,
        Side.North => Side.South,
        Side.South => Side.North,
        Side.West => Side.East,
        Side.East => Side.West,
        _ => throw new ArgumentOutOfRangeException(nameof(side), side, null)
    };
}
