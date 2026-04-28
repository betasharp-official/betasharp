using BetaSharp.Registries;
using Microsoft.Extensions.Logging;

namespace BetaSharp.Recipes;

public class RecipeManager : IRegistryReloadListener
{
    private static readonly ILogger<RecipeManager> s_logger = Log.Instance.For<RecipeManager>();

    /// <summary>
    /// Registered crafting recipe asset handlers.
    /// </summary>
    public static List<ICraftingRegistry> CraftingTypes = [
        new ShapedCraftingRegistry(),
        new ShapelessCraftingRegistry(),
        new SmeltingCraftingRegistry()
    ];

    public void OnRegistriesRebuilt(RegistryAccess registryAccess)
    {
        ClearRecipes();
        BuildRecipes(registryAccess.GetOrThrow(RegistryKeys.Recipes));
    }

    private static void BuildRecipes(IReadableRegistry<RecipeDefinition> registry)
    {
        ItemLookup.Initialize();

        foreach (RecipeDefinition def in registry)
        {
            try
            {
                if (!BuildRecipe(def))
                {
                    throw new InvalidOperationException($"Unknown crafting type: {def.Type}");
                }
            }
            catch (Exception ex)
            {
                s_logger.LogWarning(ex, "Failed to load recipe '{Name}'", def.Name);
            }
        }

        foreach (ICraftingRegistry craftingType in CraftingTypes)
        {
            s_logger.LogInformation("{Count} {Type} recipes loaded.", craftingType.Count, craftingType.Name);
        }
    }

    private static bool BuildRecipe(RecipeDefinition def)
    {
        foreach (ICraftingRegistry craftingType in CraftingTypes)
        {
            if (string.Equals(def.Type, craftingType.Name, StringComparison.OrdinalIgnoreCase))
            {
                craftingType.BuildRecipe(def);
                return true;
            }
        }

        return false;
    }

    private static void ClearRecipes()
    {
        foreach (ICraftingRegistry craftingType in CraftingTypes)
        {
            craftingType.Clear();
        }
    }
}
