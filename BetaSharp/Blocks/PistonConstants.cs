namespace BetaSharp.Blocks;

/// <summary>
///     Constants related to pistons, such as the offset of the piston head relative to the base for each direction.
/// </summary>
internal static class PistonConstants
{
    public static readonly int[] HeadOffsetX = [0, 0, 0, 0, -1, 1];
    public static readonly int[] HeadOffsetY = [-1, 1, 0, 0, 0, 0];
    public static readonly int[] HeadOffsetZ = [0, 0, -1, 1, 0, 0];
}
