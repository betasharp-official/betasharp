namespace BetaSharp.Client;

public sealed record BetaSharpRuntimeOptions
{
    public static BetaSharpRuntimeOptions Default { get; } = new();

    /// <summary>
    /// Forces the ImGui debug workspace to start enabled without requiring an input toggle.
    /// </summary>
    public bool ForceDebugUi { get; init; }

    /// <summary>
    /// Prevents the game shutdown path from terminating the host process.
    /// Useful for in-process integration tests.
    /// </summary>
    public bool SuppressProcessExit { get; init; }
}
