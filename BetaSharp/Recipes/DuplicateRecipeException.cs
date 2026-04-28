namespace BetaSharp.Recipes;

public class DuplicateRecipeException(ResourceLocation recipeKey, string type) : Exception
{
    public ResourceLocation RecipeKey { get; } = recipeKey;
    public string RecipeType { get; } = type;

    public override string Message => $"Duplicate {RecipeType} recipe entry: {RecipeKey}";
}
