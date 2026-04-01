using BetaSharp.Client.Guis;

namespace BetaSharp.Client.Debug;

public class DebugRowData
{
    public string? Text { get; }
    public Color? TextColor { get; }
    public bool IsSpacer => Text == null;

    public DebugRowData(string text, Color? textColor = null)
    {
        Text = text;
        TextColor = textColor;
    }

    private DebugRowData() { }

    public static DebugRowData Spacer() => new();
}
