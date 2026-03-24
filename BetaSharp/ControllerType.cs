namespace BetaSharp;

public class ControllerType
{
    public static ControllerType[] ControllerTypes { get; } = new ControllerType[8];

    public static readonly ControllerType XboxOne = new("xone", "Xbox One", 0);
    public static readonly ControllerType Xbox360 = new("x360", "Xbox 360", 1);
    public static readonly ControllerType PS3 = new("ps3", "PS3", 2);
    public static readonly ControllerType PS4 = new("ps4", "PS4", 3);
    public static readonly ControllerType PS5 = new("ps5", "PS5", 4);
    public static readonly ControllerType Steam = new("steam", "Steam Deck", 5);
    public static readonly ControllerType WiiU = new("wii_u", "Wii U", 6);
    public static readonly ControllerType Switch = new("switch", "Switch", 7);

    public string Key { get; }
    public string Label { get; }

    public ControllerType(string k, string l, int idx)
    {
        Key = k;
        Label = l;
        ControllerTypes[idx] = this;
    }
}
