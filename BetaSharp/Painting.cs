namespace BetaSharp;

public class Painting
{
    public static readonly Painting Kebab = new("Kebab", 16, 16, 0, 0);
    public static readonly Painting Aztec = new("Aztec", 16, 16, 16, 0);
    public static readonly Painting Alban = new("Alban", 16, 16, 32, 0);
    public static readonly Painting Aztec2 = new("Aztec2", 16, 16, 48, 0);
    public static readonly Painting Bomb = new("Bomb", 16, 16, 64, 0);
    public static readonly Painting Plant = new("Plant", 16, 16, 80, 0);
    public static readonly Painting Wasteland = new("Wasteland", 16, 16, 96, 0);
    public static readonly Painting Pool = new("Pool", 32, 16, 0, 32);
    public static readonly Painting Courbet = new("Courbet", 32, 16, 32, 32);
    public static readonly Painting Sea = new("Sea", 32, 16, 64, 32);
    public static readonly Painting Sunset = new("Sunset", 32, 16, 96, 32);
    public static readonly Painting Creebet = new("Creebet", 32, 16, 128, 32);
    public static readonly Painting Wanderer = new("Wanderer", 16, 32, 0, 64);
    public static readonly Painting Graham = new("Graham", 16, 32, 16, 64);
    public static readonly Painting Match = new("Match", 32, 32, 0, 128);
    public static readonly Painting Bust = new("Bust", 32, 32, 32, 128);
    public static readonly Painting Stage = new("Stage", 32, 32, 64, 128);
    public static readonly Painting Void = new("Void", 32, 32, 96, 128);
    public static readonly Painting SkullAndRoses = new("SkullAndRoses", 32, 32, 128, 128);
    public static readonly Painting Fighters = new("Fighters", 64, 32, 0, 96);
    public static readonly Painting Pointer = new("Pointer", 64, 64, 0, 192);
    public static readonly Painting Pigscene = new("Pigscene", 64, 64, 64, 192);
    public static readonly Painting BurningSkull = new("BurningSkull", 64, 64, 128, 192);
    public static readonly Painting Skeleton = new("Skeleton", 64, 48, 192, 64);
    public static readonly Painting DonkeyKong = new("DonkeyKong", 64, 48, 192, 112);

    public static readonly int MaxArtTitleLength = "SkullAndRoses".Length;

    public readonly string Title;
    public readonly int SizeX;
    public readonly int SizeY;
    public readonly int OffsetX;
    public readonly int OffsetY;

    public static readonly Painting[] Values =
    [
        Kebab, Aztec, Alban, Aztec2, Bomb, Plant, Wasteland, Pool, Courbet, Sea, Sunset, Creebet,
        Wanderer, Graham, Match, Bust, Stage, Void, SkullAndRoses, Fighters, Pointer, Pigscene,
        BurningSkull, Skeleton, DonkeyKong
    ];

    private Painting(string title, int sizeX, int sizeY, int offsetX, int offsetY)
    {
        Title = title;
        SizeX = sizeX;
        SizeY = sizeY;
        OffsetX = offsetX;
        OffsetY = offsetY;
    }
}
