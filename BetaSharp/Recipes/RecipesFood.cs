using BetaSharp.Blocks;
using BetaSharp.Items;

namespace BetaSharp.Recipes;

internal class RecipesFood
{
    public static void AddRecipes(CraftingManager m)
    {
        m.AddRecipe(new ItemStack(Item.MushroomStew), "Y", "X", "#", 'X', Block.BrownMushroom, 'Y', Block.RedMushroom, '#', Item.Bowl);
        m.AddRecipe(new ItemStack(Item.MushroomStew), "Y", "X", "#", 'X', Block.RedMushroom, 'Y', Block.BrownMushroom, '#', Item.Bowl);
        m.AddRecipe(new ItemStack(Item.Cookie, 8), "#X#", 'X', new ItemStack(Item.Dye, 1, 3), '#', Item.Wheat);
    }
}
