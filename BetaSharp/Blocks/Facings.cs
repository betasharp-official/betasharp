namespace BetaSharp.Blocks;

public static class Facings
{
    /// <summary>Maps horizontal bed/repeater direction (0–3) to world <see cref="Side"/> (South, West, North, East).</summary>
    public static readonly Side[] ToDir = [Side.South, Side.West, Side.North, Side.East];

    /// <summary>Opposite horizontal direction index (0↔2, 1↔3), derived from <see cref="ToDir"/> and <see cref="SideExtensions.OppositeFace"/>.</summary>
    public static int OppositeHorizontalDir(int horizontalDir)
    {
        Side face = ToDir[horizontalDir & 3];
        Side opp = SideExtensions.OppositeFace(face);
        ReadOnlySpan<Side> span = ToDir;
        for (int i = 0; i < span.Length; i++)
        {
            if (span[i] == opp)
            {
                return i;
            }
        }

        return horizontalDir & 3;
    }

    /// <summary>Per-bed-orientation mapping from model face index (same order as <see cref="Side"/>) to world <see cref="Side"/>.</summary>
    public static readonly Side[][] BedFacings =
    [
        [Side.Up, Side.Down, Side.South, Side.North, Side.East, Side.West],
        [Side.Up, Side.Down, Side.East, Side.West, Side.North, Side.South],
        [Side.Up, Side.Down, Side.North, Side.South, Side.West, Side.East],
        [Side.Up, Side.Down, Side.West, Side.East, Side.South, Side.North]
    ];
}
