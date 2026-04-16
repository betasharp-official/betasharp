namespace BetaSharp.Client.Rendering.Presentation;

/// <summary>
/// Backend-neutral representation of a render target image that can be drawn by ImGui.
/// </summary>
public readonly record struct PresentationViewportImage(ulong ImGuiTextureId, bool FlipY)
{
    public static PresentationViewportImage Empty { get; } = new(0, false);

    public bool IsAvailable => ImGuiTextureId != 0;
}
