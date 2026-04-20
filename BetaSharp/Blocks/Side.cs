namespace BetaSharp.Blocks;

/// <summary>
///     A block face, used for block placement and interaction.
/// </summary>
public enum Side : byte
{
    Down = 0,
    Up = 1,
    North = 2,
    South = 3,
    West = 4,
    East = 5
}

/// <summary>
///     Extensions for <see cref="Side" /> enum.
/// </summary>
public static class SideExtensions
{
    /// <summary>
    ///     Check if a <see cref="Side" /> value is valid.
    /// </summary>
    public static bool IsValidSide(this Side v) => (byte)v <= 5;

    /// <summary>
    ///     Convert an integer to a <see cref="Side" /> enum value.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when the integer value is not a valid <see cref="Side" />.</exception>
    public static Side ToSide(this int v) => ((Side)v).IsValidSide() ? (Side)v : throw new ArgumentException("Invalid side");

    /// <summary>
    ///     Convert a exsting <see cref="Side" /> enum value to an integer.
    /// </summary>
    public static int ToInt(this Side s) => (int)s;

    /// <summary>
    ///     Gets the opposite face of a given <see cref="Side" />. For example, the opposite of <see cref="Side.Down" /> is
    ///     <see cref="Side.Up" />, and the opposite of <see cref="Side.North" /> is <see cref="Side.South" />.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the <see cref="Side" /> value is not valid.</exception>
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
