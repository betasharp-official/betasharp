namespace BetaSharp.Blocks;

/// <summary>
///     Enum for texture variance, which specifies how a block's texture can be rotated or flipped when rendered. This
///     allows for more visual variety without needing additional textures.
/// </summary>
[Flags]
public enum TextureVariance : byte
{
    None = 0b0000,
    Rotate90 = 0b0001, // can be rotated 90 degrees clockwise
    Rotate180 = 0b0010, // can be rotated 180 degrees
    Rotate270 = 0b0011, // can be rotated 270 degrees clockwise (or 90 degrees counterclockwise)
    FlipU = 0b0100, // can be flipped horizontally (mirrored along the vertical axis)
    FlipV = 0b1000, // can be flipped vertically (mirrored along the horizontal axis)
    FlipBoth = FlipU | FlipV, // can be flipped both horizontally and vertically (mirrored along both axes)
    Rotations = Rotate90 | Rotate180 | Rotate270, // can be rotated in any of the three non-original orientations
    All = Rotations | FlipBoth // can be rotated in any orientation and flipped in any way
}
