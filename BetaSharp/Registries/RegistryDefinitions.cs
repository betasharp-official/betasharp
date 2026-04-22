namespace BetaSharp.Registries;

internal static class RegistryDefinitions
{
    public static readonly RegistryDefinition<GameMode> GameModes =
        new(RegistryKeys.GameModes, "gamemode");
}
