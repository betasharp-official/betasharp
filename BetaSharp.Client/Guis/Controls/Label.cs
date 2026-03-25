using BetaSharp.Client.Guis.Layout;
using BetaSharp.Client.Rendering;

namespace BetaSharp.Client.Guis.Controls;

public class Label : Control
{
    public Alignment TextAlign { get; init; } = Alignment.TopLeft;
    private bool _autoSize = true;
    public Label(int x, int y, string text, uint textColor) : this(x, y, 0, 0, text, textColor)
    {
        _autoSize = true;
        Text = text; // Must be set after autoSize is enabled or else OnTextChanged won't set the size
        EffectiveSize = new(
            Minecraft.INSTANCE.fontRenderer.GetStringWidth(text) + 1, // +1 for shadow
            8);
    }
    public Label(int x, int y, int width, int height, string text, uint textColor) : base(x, y, width, height)
    {
        _autoSize = false; // Must be set first or else OnTextChanged will overwrite the size
        Text = text;
        Foreground = textColor;
    }

    public override bool ContainsPoint(int x, int y) => false; // Click-through

    protected override void OnTextChanged(TextEventArgs e)
    {
        if (_autoSize)
        {
            var mc = Minecraft.INSTANCE;
            TextRenderer font = mc.fontRenderer;
            EffectiveSize = new(font.GetStringWidth(Text), 8);
        }
    }

    protected override void OnRender(RenderEventArgs e)
    {
        var mc = Minecraft.INSTANCE;
        TextRenderer font = mc.fontRenderer;

        int textX = 0;
        int textY = 0;
        if (!_autoSize)
        {
            int textWidth = Math.Min(EffectiveWidth, font.GetStringWidth(Text));
            switch (TextAlign)
            {
                case Alignment.TopLeft:
                    textX = 0;
                    textY = 0;
                    break;
                case Alignment.Top:
                    textX = 0 + (EffectiveWidth - textWidth) / 2;
                    textY = 0;
                    break;
                case Alignment.TopRight:
                    textX = 0 + EffectiveWidth - textWidth;
                    textY = 0;
                    break;
                case Alignment.Left:
                    textX = 0;
                    textY = 0 + (EffectiveHeight - 8) / 2;
                    break;
                case Alignment.Center:
                    textX = 0 + (EffectiveWidth - textWidth) / 2;
                    textY = 0 + (EffectiveHeight - 8) / 2;
                    break;
                case Alignment.Right:
                    textX = 0 + EffectiveWidth - textWidth;
                    textY = 0 + (EffectiveHeight - 8) / 2;
                    break;
                case Alignment.BottomLeft:
                    textX = 0;
                    textY = 0 + EffectiveHeight - 8;
                    break;
                case Alignment.Bottom:
                    textX = 0 + (EffectiveWidth - textWidth) / 2;
                    textY = 0 + EffectiveHeight - 8;
                    break;
                case Alignment.BottomRight:
                    textX = 0 + EffectiveWidth - textWidth;
                    textY = 0 + EffectiveHeight - 8;
                    break;
            }
        }

        font.DrawStringWrappedWithShadow(Text, textX, textY, EffectiveWidth, Foreground);
    }
}
