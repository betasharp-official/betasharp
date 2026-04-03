namespace BetaSharp.Client;

/// <summary>
/// Bridges ImGui keyboard-capture state to game code that doesn't directly depend on ImGui.
/// </summary>
internal static class ImGuiInput
{
    /// <summary>
    /// True when an ImGui widget (e.g. the debug console input) has keyboard focus.
    /// Game screens should skip key-down processing while this is set.
    /// </summary>
    public static bool CapturingKeyboard { get; internal set; }
}
