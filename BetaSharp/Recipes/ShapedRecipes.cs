using BetaSharp.Inventorys;
using BetaSharp.Items;

namespace BetaSharp.Recipes;

internal class ShapedRecipes : IRecipe
{
    private int _width;
    private int _height;
    private ItemStack?[] _items;
    private ItemStack _output;
    public readonly int RecipeOutputItemID;

    public ShapedRecipes(int width, int height, ItemStack?[] items, ItemStack output)
    {
        RecipeOutputItemID = output.ItemId;
        _width = width;
        _height = height;
        _items = items;
        _output = output;
    }

    public ItemStack GetRecipeOutput()
    {
        return _output;
    }

    public bool Matches(InventoryCrafting craftingInventory)
    {
        for (int offsetX = 0; offsetX <= 3 - _width; ++offsetX)
        {
            for (int offsetY = 0; offsetY <= 3 - _height; ++offsetY)
            {
                if (matchesAtOffset(craftingInventory, offsetX, offsetY, true))
                    return true;
                if (matchesAtOffset(craftingInventory, offsetX, offsetY, false))
                    return true;
            }
        }

        return false;
    }

    private bool matchesAtOffset(InventoryCrafting craftingInventory, int offsetX, int offsetY, bool mirrored)
    {
        for (int gridX = 0; gridX < 3; ++gridX)
        {
            for (int gridY = 0; gridY < 3; ++gridY)
            {
                int recipeX = gridX - offsetX;
                int recipeY = gridY - offsetY;
                ItemStack expected = null;
                if (recipeX >= 0 && recipeY >= 0 && recipeX < _width && recipeY < _height)
                {
                    if (mirrored)
                        expected = _items[_width - recipeX - 1 + recipeY * _width];
                    else
                        expected = _items[recipeX + recipeY * _width];
                }

                ItemStack actual = craftingInventory.GetStackAt(gridX, gridY);
                if (actual != null || expected != null)
                {
                    if (actual == null && expected != null || actual != null && expected == null)
                    {
                        return false;
                    }

                    if (expected.ItemId != actual.ItemId)
                    {
                        return false;
                    }

                    if (expected.getDamage() != -1 && expected.getDamage() != actual.getDamage())
                    {
                        return false;
                    }
                }
            }
        }

        return true;
    }

    public ItemStack GetCraftingResult(InventoryCrafting craftingInventory)
    {
        return new ItemStack(_output.ItemId, _output.Count, _output.getDamage());
    }

    public int GetRecipeSize()
    {
        return _width * _height;
    }
}
