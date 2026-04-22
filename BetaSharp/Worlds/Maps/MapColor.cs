namespace BetaSharp.Worlds.Maps;

public record struct MapColor // TODO: Move Color from Client project to Core and use it here instead of uint
{
    private static readonly List<MapColor> s_colors = [];

    public static MapColor ById(int id) => s_colors[id];

    public static MapColor Create(uint colorValue)
    {
        int id = s_colors.Count;
        var result = new MapColor(id, colorValue);
        s_colors.Add(result);
        return result;
    }

    public static MapColor Air { get; } = Create(0x000000);
    public static MapColor Grass { get; } = Create(0x7FB238);
    public static MapColor Sand { get; } = Create(0xF7E9A3);
    public static MapColor Cloth { get; } = Create(0xA7A7A7);
    public static MapColor TNT { get; } = Create(0xFF0000);
    public static MapColor Ice { get; } = Create(0xA0A0FF);
    public static MapColor Iron { get; } = Create(0xA7A7A7);
    public static MapColor Foliage { get; } = Create(0x007C00);
    public static MapColor Snow { get; } = Create(0xFFFFFF);
    public static MapColor Clay { get; } = Create(0xA4A8B8);
    public static MapColor Dirt { get; } = Create(0xB76A2F);
    public static MapColor Stone { get; } = Create(0x707070);
    public static MapColor Water { get; } = Create(0x4040FF);
    public static MapColor Wood { get; } = Create(0x685332);

    public int Id { get; }
    public uint ColorValue { get; }

    private MapColor(int id, uint colorValue)
    {
        Id = id;
        ColorValue = colorValue;
    }
}
