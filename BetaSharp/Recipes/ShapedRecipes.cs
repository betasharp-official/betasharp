using BetaSharp.Inventorys;
using BetaSharp.Items;

namespace BetaSharp.Recipes;

internal class ShapedRecipes : IRecipe
{
    private readonly int _width;
    private readonly int _height;
    private readonly ItemStack?[] _items;
    private readonly ItemStack _output;

    public ShapedRecipes(int width, int height, ItemStack?[] items, ItemStack output)
    {
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

    public override int GetHashCode()
    {
        int hash = 0;

        for (int i = 0; i < _items.Length; i++)
        {
            if (_items[i] != null)
                hash += (_items[i].ItemId + (_items[i].getDamage() << 8)) * (i + 1);
        }

        hash += (_output.ItemId << 12) + (_output.getDamage() << 20) + _output.Count;

        return ((_width + _height * 4) << 28) + hash;
    }
}
