namespace BetaSharp.Blocks;

internal static class PistonConstants
{
    /// <summary>Unit step from piston base toward the head block, per <see cref="Side"/>.</summary>
    public static int HeadOffsetX(Side facing) => facing switch
    {
        Side.West => -1,
        Side.East => 1,
        _ => 0
    };

    /// <summary>Unit step from piston base toward the head block, per <see cref="Side"/>.</summary>
    public static int HeadOffsetY(Side facing) => facing switch
    {
        Side.Down => -1,
        Side.Up => 1,
        _ => 0
    };

    /// <summary>Unit step from piston base toward the head block, per <see cref="Side"/>.</summary>
    public static int HeadOffsetZ(Side facing) => facing switch
    {
        Side.North => -1,
        Side.South => 1,
        _ => 0
    };

    public static int HeadOffsetX(int facing) => HeadOffsetX(facing.ToSide());

    public static int HeadOffsetY(int facing) => HeadOffsetY(facing.ToSide());

    public static int HeadOffsetZ(int facing) => HeadOffsetZ(facing.ToSide());
}
