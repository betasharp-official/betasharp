namespace BetaSharp.Worlds;

public class WorldType
{
    public static readonly WorldType[] WorldTypes = new WorldType[16];

    public static readonly WorldType Default = new WorldType(0, "default", "/gui/world_types/default.png").SetCanBeCreated();
    public static readonly WorldType Flat = new WorldType(1, "flat", "/gui/world_types/flat.png").SetCanBeCreated();
    public static readonly WorldType Sky = new WorldType(2, "sky", "/gui/world_types/sky.png").SetCanBeCreated();

    public string Name { get; }
    public string DisplayName { get; private set; }
    public string Description { get; private set; }
    public string IconPath { get; }
    public bool CanBeCreated { get; private set; }

    private WorldType(int id, string name, string iconPath = "")
    {
        Name = name;
        DisplayName = name;
        Description = "";
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

    public WorldType SetDescription(string description)
    {
        Description = description;
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
