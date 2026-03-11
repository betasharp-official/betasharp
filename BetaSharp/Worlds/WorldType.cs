namespace BetaSharp.Worlds;

public class WorldType
{
    public static readonly WorldType[] worldTypes = new WorldType[16];

    public static readonly WorldType Default = new WorldType(0, "default").SetCanBeCreated();
    public static readonly WorldType Flat = new WorldType(1, "flat").SetCanBeCreated();

    public string Name { get; }
    public bool CanBeCreated { get; private set; }

    private WorldType(int id, string name)
    {
        Name = name;
        CanBeCreated = false;
        worldTypes[id] = this;
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

    public static WorldType ParseWorldType(string name)
    {
        foreach (WorldType type in worldTypes)
        {
            if (type != null && type.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
            {
                return type;
            }
        }

        return Default;
    }
}
