using BetaSharp.Blocks;
using BetaSharp.Items;
using java.lang;

namespace BetaSharp.Recipes;

public class RecipesFood
{
    public void AddRecipes(CraftingManager m)
    {
        m.AddRecipe(new ItemStack(Item.MushroomStew), "Y", "X", "#", 'X', Block.BrownMushroom, 'Y', Block.RedMushroom, '#', Item.Bowl);
        m.AddRecipe(new ItemStack(Item.MushroomStew), "Y", "X", "#", 'X', Block.RedMushroom, 'Y', Block.BrownMushroom, '#', Item.Bowl);
        m.AddRecipe(new ItemStack(Item.Cookie, 8), "#X#", 'X', new ItemStack(Item.Dye, 1, 3), '#', Item.Wheat);
        m.AddRecipe(new ItemStack(Item.PumkinPie, 2), "YX#", 'Y', Block.Pumpkin,'X', Item.MilkBucket, '#', Item.Wheat);
    }
}
