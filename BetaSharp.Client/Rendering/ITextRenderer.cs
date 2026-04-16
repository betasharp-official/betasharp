using SixLabors.Fonts;

namespace BetaSharp.Client.Rendering;

/// <summary>
/// Backend-owned text rendering surface used by UI and world overlays.
/// Implementations may draw immediately, batch, or no-op depending on active backend/runtime stage.
/// </summary>
public interface ITextRenderer
{
    void DrawStringWithShadow(
        ReadOnlySpan<char> text,
        int x,
        int y,
        Guis.Color color,
        HorizontalAlignment align = HorizontalAlignment.Left);

    void DrawString(
        ReadOnlySpan<char> text,
        int x,
        int y,
        Guis.Color color,
        HorizontalAlignment align = HorizontalAlignment.Left);

    void DrawStringWrapped(
        ReadOnlySpan<char> text,
        int x,
        int y,
        int maxWidth,
        Guis.Color color,
        HorizontalAlignment align = HorizontalAlignment.Left);

    int GetStringWidth(ReadOnlySpan<char> text);
    int GetStringHeight(ReadOnlySpan<char> text, int maxWidth);
}
