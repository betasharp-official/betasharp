namespace BetaSharp.Recipes;

public interface ICraftingRegistry
{
    public string Name { get; }
    public int Count { get; }
    public void Clear();
    public void BuildRecipe(RecipeDefinition def);
}
