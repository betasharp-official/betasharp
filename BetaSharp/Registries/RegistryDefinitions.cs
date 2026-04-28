using BetaSharp.Recipes;

namespace BetaSharp.Registries;

internal static class RegistryDefinitions
{
    public static readonly RegistryDefinition<GameMode> GameModes =
        new(RegistryKeys.GameModes, "gamemode");

    public static readonly RegistryDefinition<RecipeDefinition> Recipes =
        new(RegistryKeys.Recipes, "recipe", serversideOnly: true);

}
