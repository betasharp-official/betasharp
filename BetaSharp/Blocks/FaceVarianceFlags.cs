namespace BetaSharp.Blocks;

[Flags]
public enum FaceVarianceFlags : byte
{
    None      = 0,
    Top       = 1 << 0,
    Bottom    = 1 << 1,
    Sides     = 1 << 2,
    TopBottom = Top | Bottom,
    All       = Top | Bottom | Sides,
}
