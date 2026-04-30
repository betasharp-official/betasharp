using BetaSharp.Inventorys;
using BetaSharp.Items;

namespace BetaSharp.Recipes;

public interface IRecipe
{
    bool Matches(InventoryCrafting InventoryCrafting);

    ItemStack GetCraftingResult(InventoryCrafting InventoryCrafting);

    int GetRecipeSize();

    ItemStack GetRecipeOutput();

    public bool Equals(IRecipe b) => Equals(this, b);

    public static bool Equals(IRecipe a, IRecipe b)
    {
        // The hash should 99.99% of cases be accurate.
        // If we need to hash often, we should cache the hash.
        if (a.GetHashCode() != b.GetHashCode()) return false;
        if (a.GetType() != b.GetType()) return false;
        if (!a.GetRecipeOutput().Equals(b.GetRecipeOutput())) return false;
        if (a.GetRecipeSize() != b.GetRecipeSize()) return false;

        return true;
    }
}
