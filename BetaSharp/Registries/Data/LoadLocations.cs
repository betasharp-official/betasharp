namespace BetaSharp.Registries.Data;

[Flags]
public enum LoadLocations : byte
{
    None = 0,
    Assets = 1,
    GameDatapack = 2,
    WorldDatapack = 4,
    Resourcepack = 8,

    AllInit = Assets | GameDatapack,
    AllData = AllInit | WorldDatapack,
}
