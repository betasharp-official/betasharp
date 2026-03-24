using BetaSharp.Blocks;
using BetaSharp.Inventorys;
using BetaSharp.Items;
using Microsoft.Extensions.Logging;

namespace BetaSharp.Recipes;

internal class CraftingManager
{
    private static CraftingManager instance { get; } = new();
    public List<IRecipe> Recipes { get; } = [];

    private readonly ILogger<CraftingManager> _logger = Log.Instance.For<CraftingManager>();

    public static CraftingManager getInstance()
    {
        return instance;
    }

    private CraftingManager()
    {
        new RecipesTools().AddRecipes(this);
        new RecipesWeapons().AddRecipes(this);
        new RecipesIngots().AddRecipes(this);
        new RecipesFood().AddRecipes(this);
        new RecipesCrafting().AddRecipes(this);
        new RecipesArmor().AddRecipes(this);
        new RecipesDyes().AddRecipes(this);
        AddRecipe(new ItemStack(Item.Paper, 3), ["###", '#', Item.SugarCane]);
        AddRecipe(new ItemStack(Item.Book, 1), ["#", "#", "#", '#', Item.Paper]);
        AddRecipe(new ItemStack(Block.Fence, 2), ["###", "###", '#', Item.Stick]);
        AddRecipe(new ItemStack(Block.Jukebox, 1), ["###", "#X#", "###", '#', Block.Planks, 'X', Item.Diamond]);
        AddRecipe(new ItemStack(Block.Noteblock, 1), ["###", "#X#", "###", '#', Block.Planks, 'X', Item.Redstone]);
        AddRecipe(new ItemStack(Block.Bookshelf, 1), ["###", "XXX", "###", '#', Block.Planks, 'X', Item.Book]);
        AddRecipe(new ItemStack(Block.SnowBlock, 1), ["##", "##", '#', Item.Snowball]);
        AddRecipe(new ItemStack(Block.Clay, 1), ["##", "##", '#', Item.Clay]);
        AddRecipe(new ItemStack(Block.Bricks, 1), ["##", "##", '#', Item.Brick]);
        AddRecipe(new ItemStack(Block.Glowstone, 1), ["##", "##", '#', Item.GlowstoneDust]);
        AddRecipe(new ItemStack(Block.Wool, 1), ["##", "##", '#', Item.String]);
        AddRecipe(new ItemStack(Block.TNT, 1), ["X#X", "#X#", "X#X", 'X', Item.Gunpowder, '#', Block.Sand]);
        AddRecipe(new ItemStack(Block.Slab, 3, 3), ["###", '#', Block.Cobblestone]);
        AddRecipe(new ItemStack(Block.Slab, 3, 0), ["###", '#', Block.Stone]);
        AddRecipe(new ItemStack(Block.Slab, 3, 1), ["###", '#', Block.Sandstone]);
        AddRecipe(new ItemStack(Block.Slab, 3, 2), ["###", '#', Block.Planks]);
        AddRecipe(new ItemStack(Block.Ladder, 2), ["# #", "###", "# #", '#', Item.Stick]);
        AddRecipe(new ItemStack(Item.WoodenDoor, 1), ["##", "##", "##", '#', Block.Planks]);
        AddRecipe(new ItemStack(Block.Trapdoor, 2), ["###", "###", '#', Block.Planks]);
        AddRecipe(new ItemStack(Item.IronDoor, 1), ["##", "##", "##", '#', Item.IronIngot]);
        AddRecipe(new ItemStack(Item.Sign, 1), ["###", "###", " X ", '#', Block.Planks, 'X', Item.Stick]);
        AddRecipe(new ItemStack(Item.Cake, 1), ["AAA", "BEB", "CCC", 'A', Item.MilkBucket, 'B', Item.Sugar, 'C', Item.Wheat, 'E', Item.Egg]);
        AddRecipe(new ItemStack(Item.Sugar, 1), ["#", '#', Item.SugarCane]);
        AddRecipe(new ItemStack(Block.Planks, 4), ["#", '#', Block.Log]);
        AddRecipe(new ItemStack(Item.Stick, 4), ["#", "#", '#', Block.Planks]);
        AddRecipe(new ItemStack(Block.Torch, 4), ["X", "#", 'X', Item.Coal, '#', Item.Stick]);
        AddRecipe(new ItemStack(Block.Torch, 4), ["X", "#", 'X', new ItemStack(Item.Coal, 1, 1), '#', Item.Stick]);
        AddRecipe(new ItemStack(Item.Bowl, 4), ["# #", " # ", '#', Block.Planks]);
        AddRecipe(new ItemStack(Block.Rail, 16), ["X X", "X#X", "X X", 'X', Item.IronIngot, '#', Item.Stick]);
        AddRecipe(new ItemStack(Block.PoweredRail, 6), ["X X", "X#X", "XRX", 'X', Item.GoldIngot, 'R', Item.Redstone, '#', Item.Stick]);
        AddRecipe(new ItemStack(Block.DetectorRail, 6), ["X X", "X#X", "XRX", 'X', Item.IronIngot, 'R', Item.Redstone, '#', Block.StonePressurePlate]);
        AddRecipe(new ItemStack(Item.Minecart, 1), ["# #", "###", '#', Item.IronIngot]);
        AddRecipe(new ItemStack(Block.JackLantern, 1), ["A", "B", 'A', Block.Pumpkin, 'B', Block.Torch]);
        AddRecipe(new ItemStack(Item.ChestMinecart, 1), ["A", "B", 'A', Block.Chest, 'B', Item.Minecart]);
        AddRecipe(new ItemStack(Item.FurnaceMinecart, 1), ["A", "B", 'A', Block.Furnace, 'B', Item.Minecart]);
        AddRecipe(new ItemStack(Item.Boat, 1), ["# #", "###", '#', Block.Planks]);
        AddRecipe(new ItemStack(Item.Bucket, 1), ["# #", " # ", '#', Item.IronIngot]);
        AddRecipe(new ItemStack(Item.FlintAndSteel, 1), ["A ", " B", 'A', Item.IronIngot, 'B', Item.Flint]);
        AddRecipe(new ItemStack(Item.Bread, 1), ["###", '#', Item.Wheat]);
        AddRecipe(new ItemStack(Block.WoodenStairs, 4), ["#  ", "## ", "###", '#', Block.Planks]);
        AddRecipe(new ItemStack(Item.FishingRod, 1), ["  #", " #X", "# X", '#', Item.Stick, 'X', Item.String]);
        AddRecipe(new ItemStack(Block.CobblestoneStairs, 4), ["#  ", "## ", "###", '#', Block.Cobblestone]);
        AddRecipe(new ItemStack(Item.Painting, 1), ["###", "#X#", "###", '#', Item.Stick, 'X', Block.Wool]);
        AddRecipe(new ItemStack(Item.GoldenApple, 1), ["###", "#X#", "###", '#', Block.GoldBlock, 'X', Item.Apple]);
        AddRecipe(new ItemStack(Block.Lever, 1), ["X", "#", '#', Block.Cobblestone, 'X', Item.Stick]);
        AddRecipe(new ItemStack(Block.LitRedstoneTorch, 1), ["X", "#", '#', Item.Stick, 'X', Item.Redstone]);
        AddRecipe(new ItemStack(Item.Repeater, 1), ["#X#", "III", '#', Block.LitRedstoneTorch, 'X', Item.Redstone, 'I', Block.Stone]);
        AddRecipe(new ItemStack(Item.Clock, 1), [" # ", "#X#", " # ", '#', Item.GoldIngot, 'X', Item.Redstone]);
        AddRecipe(new ItemStack(Item.Compass, 1), [" # ", "#X#", " # ", '#', Item.IronIngot, 'X', Item.Redstone]);
        AddRecipe(new ItemStack(Item.Map, 1), ["###", "#X#", "###", '#', Item.Paper, 'X', Item.Compass]);
        AddRecipe(new ItemStack(Block.Button, 1), ["#", "#", '#', Block.Stone]);
        AddRecipe(new ItemStack(Block.StonePressurePlate, 1), ["##", '#', Block.Stone]);
        AddRecipe(new ItemStack(Block.WoodenPressurePlate, 1), ["##", '#', Block.Planks]);
        AddRecipe(new ItemStack(Block.Dispenser, 1), ["###", "#X#", "#R#", '#', Block.Cobblestone, 'X', Item.BOW, 'R', Item.Redstone]);
        AddRecipe(new ItemStack(Block.Piston, 1), ["TTT", "#X#", "#R#", '#', Block.Cobblestone, 'X', Item.IronIngot, 'R', Item.Redstone, 'T', Block.Planks]);
        AddRecipe(new ItemStack(Block.StickyPiston, 1), ["S", "P", 'S', Item.Slimeball, 'P', Block.Piston]);
        AddRecipe(new ItemStack(Item.Bed, 1), ["###", "XXX", '#', Block.Wool, 'X', Block.Planks]);
        Recipes.Sort(new RecipeSorter());
        _logger.LogInformation($"{Recipes.Count} recipes");
    }

    public void AddRecipe(ItemStack result, params object[] pattern)
    {
        string patternString = "";
        int index = 0;
        int width = 0;
        int height = 0;

        while (index < pattern.Length && (pattern[index] is string || pattern[index] is string[]))
        {
            object current = pattern[index++];
            if (current is string[] rows)
            {
                foreach (var row in rows)
                {
                    height++;
                    width = row.Length;
                    patternString += row;
                }
            }
            else if (current is string row)
            {
                height++;
                width = row.Length;
                patternString += row;
            }
        }

        var ingredients = new Dictionary<char, ItemStack?>();
        for (; index < pattern.Length; index += 2)
        {
            char key = (char)pattern[index];
            object input = pattern[index + 1];

            ItemStack? value = input switch
            {
                Item item       => new ItemStack(item),
                Block block     => new ItemStack(block, 1, -1),
                ItemStack stack => stack,
                _               => null // Thowing some Exception here would be ideal, but the original game does not do this
            };

            ingredients[key] = value;
        }

        var ingredientGrid = new ItemStack?[width * height];

        for (int i = 0; i < patternString.Length; i++)
        {
            char c = patternString[i];
            ingredients.TryGetValue(c, out var stack);
            ingredientGrid[i] = stack?.copy() ?? null;
        }

        Recipes.Add(new ShapedRecipes(width, height, ingredientGrid, result));
    }

    public void AddShapelessRecipe(ItemStack result, params object[] pattern)
    {
        List<ItemStack> stacks = [];

        foreach (var ingredient in pattern)
        {
            switch (ingredient)
            {
                case ItemStack s: stacks.Add(s.copy()); break;
                case Item i: stacks.Add(new ItemStack(i)); break;
                case Block b: stacks.Add(new ItemStack(b)); break;
                default:
                    throw new InvalidOperationException("Invalid shapeless recipy!"); // This typo is intentional to match the original game
            }
        }

        Recipes.Add(new ShapelessRecipes(result, stacks));
    }

    public ItemStack? FindMatchingRecipe(InventoryCrafting craftingInventory)
    {
        return Recipes
            .FirstOrDefault(r => r.Matches(craftingInventory))
            ?.GetCraftingResult(craftingInventory);
    }
}
