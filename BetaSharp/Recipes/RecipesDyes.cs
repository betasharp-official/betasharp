using BetaSharp.Blocks;
using BetaSharp.Items;

namespace BetaSharp.Recipes;

internal class RecipesDyes
{
    public static void AddRecipes(CraftingManager m)
    {
        for (int colorIndex = 0; colorIndex < 16; ++colorIndex)
        {
            m.AddShapelessRecipe(new ItemStack(Block.Wool, 1, BlockCloth.getItemMeta(colorIndex)), new ItemStack(Item.Dye, 1, colorIndex), new ItemStack(Item.ITEMS[Block.Wool.id], 1, 0));
        }

        m.AddShapelessRecipe(new ItemStack(Item.Dye, 2, 11), Block.Dandelion);
        m.AddShapelessRecipe(new ItemStack(Item.Dye, 2, 1), Block.Rose);
        m.AddShapelessRecipe(new ItemStack(Item.Dye, 3, 15), Item.Bone);
        m.AddShapelessRecipe(new ItemStack(Item.Dye, 2, 9), new ItemStack(Item.Dye, 1, 1), new ItemStack(Item.Dye, 1, 15));
        m.AddShapelessRecipe(new ItemStack(Item.Dye, 2, 14), new ItemStack(Item.Dye, 1, 1), new ItemStack(Item.Dye, 1, 11));
        m.AddShapelessRecipe(new ItemStack(Item.Dye, 2, 10), new ItemStack(Item.Dye, 1, 2), new ItemStack(Item.Dye, 1, 15));
        m.AddShapelessRecipe(new ItemStack(Item.Dye, 2, 8), new ItemStack(Item.Dye, 1, 0), new ItemStack(Item.Dye, 1, 15));
        m.AddShapelessRecipe(new ItemStack(Item.Dye, 2, 7), new ItemStack(Item.Dye, 1, 8), new ItemStack(Item.Dye, 1, 15));
        m.AddShapelessRecipe(new ItemStack(Item.Dye, 3, 7), new ItemStack(Item.Dye, 1, 0), new ItemStack(Item.Dye, 1, 15), new ItemStack(Item.Dye, 1, 15));
        m.AddShapelessRecipe(new ItemStack(Item.Dye, 2, 12), new ItemStack(Item.Dye, 1, 4), new ItemStack(Item.Dye, 1, 15));
        m.AddShapelessRecipe(new ItemStack(Item.Dye, 2, 6), new ItemStack(Item.Dye, 1, 4), new ItemStack(Item.Dye, 1, 2));
        m.AddShapelessRecipe(new ItemStack(Item.Dye, 2, 5), new ItemStack(Item.Dye, 1, 4), new ItemStack(Item.Dye, 1, 1));
        m.AddShapelessRecipe(new ItemStack(Item.Dye, 2, 13), new ItemStack(Item.Dye, 1, 5), new ItemStack(Item.Dye, 1, 9));
        m.AddShapelessRecipe(new ItemStack(Item.Dye, 3, 13), new ItemStack(Item.Dye, 1, 4), new ItemStack(Item.Dye, 1, 1), new ItemStack(Item.Dye, 1, 9));
        m.AddShapelessRecipe(new ItemStack(Item.Dye, 4, 13), new ItemStack(Item.Dye, 1, 4), new ItemStack(Item.Dye, 1, 1), new ItemStack(Item.Dye, 1, 1), new ItemStack(Item.Dye, 1, 15));
    }
}
