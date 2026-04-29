namespace BetaSharp.Recipes;

public class OverlappingRecipeException(string item, string type) : Exception
{
    public string Item { get; } = item;
    public string RecipeType { get; } = type;

    public override string Message => $"Overlapping {RecipeType} recipe entry for: {Item}";
}
