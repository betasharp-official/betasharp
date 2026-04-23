using BetaSharp.Items;

namespace BetaSharp.Recipes;

public static class RecipesSmelting
{
    public static Dictionary<int, ItemStack> Recipes { get; } = [];

    public static void BuildSmeltRecipe(RecipeDefinition def)
    {
        if (string.IsNullOrEmpty(def.Input))
            throw new InvalidOperationException("Smelting recipe has no input.");

        if (!ItemLookup.TryGetItemId(def.Input, out int inputId))
            throw new InvalidOperationException($"Unknown input '{def.Input}'.");

        if (!ItemLookup.TryGetItem(def.Result.Id, out ItemStack? output, def.Result.Count))
            throw new InvalidOperationException($"Unknown result '{def.Result.Id}'.");

        Recipes[inputId] = output;
    }

    public static ItemStack? Craft(int inputId)
    {
        Recipes.TryGetValue(inputId, out ItemStack? result);
        return result;
    }
}
