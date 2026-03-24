using BetaSharp.Inventorys;
using BetaSharp.Items;
namespace BetaSharp.Recipes;

internal class ShapelessRecipes : IRecipe
{

    private readonly ItemStack _output;
    private readonly List<ItemStack> _recipeItems;

    public ShapelessRecipes(ItemStack output, List<ItemStack> items)
    {
        _output = output;
        _recipeItems = items;
    }

    public ItemStack GetRecipeOutput()
    {
        return _output;
    }

    public bool Matches(InventoryCrafting craftingInventory)
    {
        List<ItemStack> remainingIngredients = [.. _recipeItems];

        for (int row = 0; row < 3; ++row)
        {
            for (int col = 0; col < 3; ++col)
            {
                ItemStack gridStack = craftingInventory.getStackAt(col, row);
                if (gridStack != null)
                {
                    bool foundMatch = false;
                    List<ItemStack>.Enumerator iterator = remainingIngredients.GetEnumerator();

                    while (iterator.MoveNext())
                    {
                        ItemStack recipeItem = iterator.Current;
                        if (gridStack.itemId == recipeItem.itemId && (recipeItem.getDamage() == -1 || gridStack.getDamage() == recipeItem.getDamage()))
                        {
                            foundMatch = true;
                            remainingIngredients.Remove(recipeItem);
                            break;
                        }
                    }

                    if (!foundMatch)
                    {
                        return false;
                    }
                }
            }
        }

        return remainingIngredients.Count == 0;
    }

    public ItemStack GetCraftingResult(InventoryCrafting craftingInventory)
    {
        return _output.copy();
    }

    public int GetRecipeSize()
    {
        return _recipeItems.Count;
    }
}
