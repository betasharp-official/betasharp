using System.Text.Json;
using BetaSharp.Registries.Data;

namespace BetaSharp.Tests;

public class DataAssetBugTests
{
    // -------------------------------------------------------------------------
    // Bug 1: InvalidOperationException when lazy asset's JSON deserializes to null
    //
    // Root cause: when the JSON content is "null", Deserialize returns null and
    // the lazy resolver throws InvalidOperationException instead of succeeding.
    // -------------------------------------------------------------------------

    [Fact]
    public void Lazy_asset_with_null_json_content_throws_instead_of_recursing()
    {
        var id = new ResourceLocation(Namespace.BetaSharp, "test");
        var holder = DataAssetLoader<GameMode>.CreateLazyHolder("null", id);

        Assert.Throws<InvalidOperationException>(() => _ = holder.Value);
    }

    // -------------------------------------------------------------------------
    // Bug 2: InvalidOperationException is thrown when lazy JSON content is invalid
    //
    // File I/O now happens at load time, not resolve time, so there is no file
    // handle to leak. The resolver just parses a pre-read string and wraps any
    // JsonException in an InvalidOperationException for context.
    // -------------------------------------------------------------------------

    [Fact]
    public void Lazy_asset_with_invalid_json_content_throws_invalid_operation()
    {
        var id = new ResourceLocation(Namespace.BetaSharp, "test");
        var holder = DataAssetLoader<GameMode>.CreateLazyHolder("{not valid json", id);

        Assert.Throws<InvalidOperationException>(() => _ = holder.Value);
    }
}
