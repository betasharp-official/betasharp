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
        var a = registryAccess.GetOrThrow(RegistryKeys.Recipes);
        if (!a.Any())
        {
            s_logger.LogCritical("Registries Rebuilt with 0 recipes. ignoring recipes reload.");
            return;
        }

        ClearRecipes();
        BuildRecipes(registryAccess.GetOrThrow(RegistryKeys.Recipes));
    }

    public static void Rebuild(IEnumerable<RecipeDefinition> items)
    {
        ClearRecipes();
        BuildRecipes(items);
    }

    public static void Rebuild(IEnumerable<Holder<RecipeDefinition>> items)
    {
        ClearRecipes();
        BuildRecipes(items);
    }

    private static void BuildRecipes(IEnumerable<RecipeDefinition> items)
    {
        ItemLookup.Initialize();

        foreach (RecipeDefinition def in items)
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

    private static void BuildRecipes(IEnumerable<Holder<RecipeDefinition>> items)
    {
        ItemLookup.Initialize();

        foreach (RecipeDefinition def in items)
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
