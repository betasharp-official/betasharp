[Flags]
public enum TextureVariance : byte
{
    None = 0b0000,
    Rotate90 = 0b0001,
    Rotate180 = 0b0010,
    Rotate270 = 0b0011,
    FlipU = 0b0100,
    FlipV = 0b1000,
    FlipBoth = FlipU | FlipV,
    Rotations = Rotate90 | Rotate180 | Rotate270,
    All = Rotations | FlipBoth,
}
