using BetaSharp.Inventorys;
using BetaSharp.Items;

namespace BetaSharp.Recipes;

public static class RecipesCrafting
{
    public static List<IRecipe> Recipes { get; } = [];

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

        Recipes.Add(new ShapedRecipes(width, height, grid, ParseResult(def.Result, def.Name)));
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

        Recipes.Add(new ShapelessRecipes(ParseResult(def.Result, def.Name), stacks));
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
        return Recipes
            .FirstOrDefault(r => r.Matches(craftingInventory))
            ?.GetCraftingResult(craftingInventory);
    }
}
