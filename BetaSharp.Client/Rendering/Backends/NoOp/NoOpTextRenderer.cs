using SixLabors.Fonts;

namespace BetaSharp.Client.Rendering.Backends;

internal sealed class NoOpTextRenderer : ITextRenderer
{
    public void DrawStringWithShadow(
        ReadOnlySpan<char> text,
        int x,
        int y,
        Guis.Color color,
        HorizontalAlignment align = HorizontalAlignment.Left)
    {
    }

    public void DrawString(
        ReadOnlySpan<char> text,
        int x,
        int y,
        Guis.Color color,
        HorizontalAlignment align = HorizontalAlignment.Left)
    {
    }

    public void DrawStringWrapped(
        ReadOnlySpan<char> text,
        int x,
        int y,
        int maxWidth,
        Guis.Color color,
        HorizontalAlignment align = HorizontalAlignment.Left)
    {
    }

    public int GetStringWidth(ReadOnlySpan<char> text)
    {
        int visibleChars = 0;
        for (int i = 0; i < text.Length; i++)
        {
            if (text[i] == '\u00A7' && i + 1 < text.Length)
            {
                i++;
                continue;
            }

            visibleChars++;
        }

        return visibleChars * 6;
    }

    public int GetStringHeight(ReadOnlySpan<char> text, int maxWidth)
    {
        return 0;
    }
}
