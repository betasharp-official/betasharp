using BetaSharp.Client;
using BetaSharp.Client.Rendering;

namespace BetaSharp.Tests;

public class ClientStartupArgumentParserTests
{
    [Fact]
    public void Parse_NoArgs_UsesGeneratedPlayerAndDefaults()
    {
        ClientStartupOptions options = ClientStartupArgumentParser.Parse([]);

        Assert.StartsWith("Player", options.PlayerName);
        Assert.Equal("-", options.SessionToken);
        Assert.Equal(RendererBackendKind.OpenGL, options.RendererBackend);
    }

    [Fact]
    public void Parse_OnePositionalArg_UsesDefaultSession()
    {
        ClientStartupOptions options = ClientStartupArgumentParser.Parse(["Alice"]);

        Assert.Equal("Alice", options.PlayerName);
        Assert.Equal("-", options.SessionToken);
    }

    [Fact]
    public void Parse_TwoPositionalArgs_UsesPlayerAndSession()
    {
        ClientStartupOptions options = ClientStartupArgumentParser.Parse(["Alice", "token123"]);

        Assert.Equal("Alice", options.PlayerName);
        Assert.Equal("token123", options.SessionToken);
    }

    [Fact]
    public void Parse_WithRendererEqualsFormat_UsesRenderer()
    {
        ClientStartupOptions options = ClientStartupArgumentParser.Parse(["--renderer=vulkan", "Alice", "token123"]);

        Assert.Equal(RendererBackendKind.Vulkan, options.RendererBackend);
        Assert.Equal("Alice", options.PlayerName);
        Assert.Equal("token123", options.SessionToken);
    }

    [Fact]
    public void Parse_WithRendererSeparateValue_UsesRenderer()
    {
        ClientStartupOptions options = ClientStartupArgumentParser.Parse(["--renderer", "opengl", "Alice"]);

        Assert.Equal(RendererBackendKind.OpenGL, options.RendererBackend);
        Assert.Equal("Alice", options.PlayerName);
        Assert.Equal("-", options.SessionToken);
    }

    [Fact]
    public void Parse_MissingRendererValue_Throws()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            ClientStartupArgumentParser.Parse(["--renderer"]));

        Assert.Contains("Missing value for --renderer", ex.Message);
    }

    [Fact]
    public void Parse_UnknownOption_Throws()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            ClientStartupArgumentParser.Parse(["--not-a-real-option"]));

        Assert.Contains("Unknown option", ex.Message);
    }

    [Fact]
    public void Parse_UnsupportedRenderer_Throws()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            ClientStartupArgumentParser.Parse(["--renderer=metal"]));

        Assert.Contains("Unsupported renderer backend", ex.Message);
    }
}
