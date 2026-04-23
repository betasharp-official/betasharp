using BetaSharp.Registries;
using Microsoft.Extensions.Logging;

namespace BetaSharp.Recipes;

public class RecipeManager
{
    private static RecipeManager? s_instance;

    private readonly ILogger<RecipeManager> _logger = Log.Instance.For<RecipeManager>();

    public static RecipeManager getInstance()
    {
        if (s_instance == null)
        {
            var loader = RegistryDefinitions.Recipes.CreateLoader();
            loader.LoadFromPaths(null, null, null);
            loader.Freeze();
            s_instance = new RecipeManager(loader);
        }

        return s_instance;
    }

    public static void Initialize(IReadableRegistry<RecipeDefinition> registry)
    {
        s_instance = new RecipeManager(registry);
    }

    private RecipeManager(IReadableRegistry<RecipeDefinition> registry)
    {
        ItemLookup.Initialize();

        foreach (RecipeDefinition def in registry)
        {
            try
            {
                if (string.Equals(def.Type, "shapeless", StringComparison.OrdinalIgnoreCase))
                {
                    RecipesCrafting.BuildShapelessRecipe(def);
                }
                else if (string.Equals(def.Type, "shaped", StringComparison.OrdinalIgnoreCase))
                {
                    RecipesCrafting.BuildShapedRecipe(def);
                }
                else if (string.Equals(def.Type, "smelting", StringComparison.OrdinalIgnoreCase))
                {
                    RecipesSmelting.BuildSmeltRecipe(def);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load recipe '{Name}'", def.Name);
            }
        }

        _logger.LogInformation("{Count} crafting recipes", RecipesCrafting.Recipes.Count);
        _logger.LogInformation("{Count} smelting recipes", RecipesSmelting.Recipes.Count);
    }
}
