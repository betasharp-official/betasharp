using System.Text.Json.Serialization;
using BetaSharp.Registries.Data;

namespace BetaSharp.Recipes;

public class RecipeDefinition : DataAsset
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "shaped";

    [JsonPropertyName("pattern")]
    public string[]? Pattern { get; set; }

    /// <summary>
    /// Maps pattern characters to ingredient strings.
    /// Format: "Name" (any damage) or "Name:damage" (specific damage).
    /// Examples: "IronIngot", "Coal:1" (charcoal), "dye:4" (lapis)
    /// </summary>
    [JsonPropertyName("key")]
    public Dictionary<string, string>? Key { get; set; }

    /// <summary>Shapeless ingredient strings, same format as key values.</summary>
    [JsonPropertyName("ingredients")]
    public string[]? Ingredients { get; set; }

    /// <summary>Smelting input item/block: "Name" or "Name:damage". Only used when type is "smelting".</summary>
    [JsonPropertyName("input")]
    public string? Input { get; set; }

    [JsonPropertyName("result")]
    public ResultRef Result { get; set; } = new();
}

public class ResultRef
{
    /// <summary>Item/block name with optional damage/meta: "leather" or "wool:4"</summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("count")]
    public int Count { get; set; } = 1;
}
