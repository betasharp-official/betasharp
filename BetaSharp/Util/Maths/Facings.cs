using BetaSharp.Blocks;

namespace BetaSharp.Util.Maths;

public static class Facings
{
    /// <summary>Maps horizontal bed/repeater direction (0–3) to world <see cref="Side"/> (South, West, North, East).</summary>
    public static readonly Side[] ToDir = [Side.South, Side.West, Side.North, Side.East];
    
}
