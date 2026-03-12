namespace BetaSharp.Worlds;

public class WorldType
{
    public static readonly WorldType[] WorldTypes = new WorldType[16];

    public static readonly WorldType Default = new WorldType(0, "default", "The standard Minecraft world.", "/gui/world_types/default.png").SetCanBeCreated().SetDisplayName("Default");
    public static readonly WorldType Flat = new WorldType(1, "flat", "A completely flat world.", "/gui/world_types/flat.png").SetCanBeCreated().SetDisplayName("Flat");
    public static readonly WorldType Sky = new WorldType(2, "sky", "A world floating in the sky.", "/gui/world_types/sky.png").SetCanBeCreated().SetDisplayName("Sky");

    public string Name { get; }
    public string DisplayName { get; private set; }
    public string Description { get; }
    public string IconPath { get; }
    public bool CanBeCreated { get; private set; }

    private WorldType(int id, string name, string description = "", string iconPath = "")
    {
        Name = name;
        DisplayName = name;
        Description = description;
        IconPath = iconPath;
        CanBeCreated = false;
        WorldTypes[id] = this;
    }

    public string GetTranslateName()
    {
        return $"generator.{Name}";
    }

    public WorldType SetCanBeCreated(bool val = true)
    {
        CanBeCreated = val;
        return this;
    }

    public WorldType SetDisplayName(string displayName)
    {
        DisplayName = displayName;
        return this;
    }

    public static WorldType ParseWorldType(string name)
    {
        foreach (WorldType type in WorldTypes)
        {
            if (type != null && type.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
            {
                return type;
            }
        }

        return Default;
    }
}
