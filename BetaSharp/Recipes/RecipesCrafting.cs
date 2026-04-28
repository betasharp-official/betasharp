using BetaSharp.Inventorys;
using BetaSharp.Items;

namespace BetaSharp.Recipes;

public class ShapedCraftingRegistry : ICraftingRegistry
{
    internal const string Name = "shaped";
    string ICraftingRegistry.Name => Name;
    public int Count => RecipesCrafting.ShapedRecipeCount;
    public void Clear() => RecipesCrafting.Clear();
    public void BuildRecipe(RecipeDefinition def) => RecipesCrafting.BuildShapedRecipe(def);
}

public class ShapelessCraftingRegistry : ICraftingRegistry
{
    internal const string Name = "shapeless";
    string ICraftingRegistry.Name => Name;
    public int Count => RecipesCrafting.ShapelessRecipeCount;
    public void Clear() => RecipesCrafting.Clear();
    public void BuildRecipe(RecipeDefinition def) => RecipesCrafting.BuildShapelessRecipe(def);
}

public static class RecipesCrafting
{
    public static int ShapedRecipeCount = 0;
    public static int ShapelessRecipeCount = 0;

    public static Dictionary<ResourceLocation, IRecipe> Recipes { get; } = [];

    public static void Clear()
    {
        ShapedRecipeCount = 0;
        ShapelessRecipeCount = 0;
        Recipes.Clear();
    }

    public static void BuildShapedRecipe(RecipeDefinition def)
    {
        if (def.Pattern == null || def.Pattern.Length == 0)
            throw new InvalidOperationException("Shaped recipe has no pattern.");
        if (def.Key == null)
            throw new InvalidOperationException("Shaped recipe has no key.");

        int height = def.Pattern.Length;
        int width = def.Pattern.Max(r => r.Length);

        var keyMap = new Dictionary<char, ItemStack?>();
        foreach ((string keyStr, string ingredientRef) in def.Key)
            keyMap[keyStr[0]] = ParseIngredient(ingredientRef, def.Name);

        var grid = new ItemStack?[width * height];
        for (int row = 0; row < height; row++)
        {
            string rowStr = def.Pattern[row];
            for (int col = 0; col < rowStr.Length; col++)
            {
                char c = rowStr[col];
                if (keyMap.TryGetValue(c, out ItemStack? stack))
                    grid[row * width + col] = stack?.copy();
            }
        }

        ResourceLocation key = new(def.Namespace, def.Name);
        ShapedRecipes recipe = new(width, height, grid, ParseResult(def.Result, def.Name));

        foreach (var r in Recipes)
        {
            if (r.Value.Equals(recipe))
            {
                throw new DuplicateRecipeException(key, ShapedCraftingRegistry.Name);
            }
        }

        if (!Recipes.TryAdd(key, recipe))
        {
            throw new DuplicateRecipeException(key, ShapedCraftingRegistry.Name);
        }

        ShapedRecipeCount++;
    }

    public static void BuildShapelessRecipe(RecipeDefinition def)
    {
        if (def.Ingredients == null || def.Ingredients.Length == 0)
            throw new InvalidOperationException("Shapeless recipe has no ingredients.");

        var stacks = def.Ingredients
            .Select(i => ParseIngredient(i, def.Name))
            .Where(s => s != null)
            .Select(s => s!)
            .ToList();

        ResourceLocation key = new(def.Namespace, def.Name);
        ShapelessRecipes recipe = new(ParseResult(def.Result, def.Name), stacks);

        foreach (var r in Recipes)
        {
            if (r.Value.Equals(recipe))
            {
                throw new DuplicateRecipeException(key, ShapelessCraftingRegistry.Name);
            }
        }

        if (!Recipes.TryAdd(key, recipe))
        {
            throw new DuplicateRecipeException(key, ShapelessCraftingRegistry.Name);
        }

        ShapelessRecipeCount++;
    }

    private static ItemStack ParseIngredient(string name, string recipeName)
    {
        if (ItemLookup.TryGetItem(name, out ItemStack? item, 1, -1)) return item;
        throw new InvalidOperationException($"Recipe '{recipeName}': unknown item/block '{name}'.");
    }

    private static ItemStack ParseResult(ResultRef result, string recipeName)
    {
        if (ItemLookup.TryGetItem(result.Id, out ItemStack? item, result.Count)) return item;
        throw new InvalidOperationException($"Recipe '{recipeName}': unknown item/block result '{result.Id}'.");
    }

    public static ItemStack? Craft(InventoryCrafting craftingInventory)
    {
        try
        {
            return Recipes.First(r => r.Value.Matches(craftingInventory)).Value.GetCraftingResult(craftingInventory);
        }
        catch
        {
            return null;
        }
    }
}
