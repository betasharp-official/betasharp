using BetaSharp.Blocks;
using BetaSharp.Items;

namespace BetaSharp.Recipes;

internal class RecipesCrafting
{
    public static void AddRecipes(CraftingManager manager)
    {
        manager.AddRecipe(new ItemStack(Block.Chest), "###", "# #", "###", '#', Block.Planks);
        manager.AddRecipe(new ItemStack(Block.Furnace), "###", "# #", "###", '#', Block.Cobblestone);
        manager.AddRecipe(new ItemStack(Block.CraftingTable), "##", "##", '#', Block.Planks);
        manager.AddRecipe(new ItemStack(Block.Sandstone), "##", "##", '#', Block.Sand);
    }
}
