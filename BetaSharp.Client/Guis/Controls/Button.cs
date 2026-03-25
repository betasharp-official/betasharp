using BetaSharp.Client.Guis.Layout;
using BetaSharp.Client.Rendering;
using BetaSharp.Client.Rendering.Core;

namespace BetaSharp.Client.Guis.Controls;

public class Button : Control
{
    public override bool Focusable => true;
    public Alignment TextAlign { get; init; } = Alignment.Center;
    public Button(int x, int y, string text) : this(x, y, 200, text) { }
    public Button(int x, int y, int width, string text) : base(x, y, width, 20)
    {
        Text = text;
        Foreground = 0xFFE0E0E0;
    }

    protected override void OnClick(MouseEventArgs e)
    {
        Minecraft.INSTANCE.sndManager.PlaySoundFX("random.click", 1.0F, 1.0F);
    }

    protected override void OnRender(RenderEventArgs e)
    {
        var mc = Minecraft.INSTANCE;
        TextRenderer font = mc.fontRenderer;

        mc.textureManager.BindTexture(mc.textureManager.GetTextureId("/gui/gui.png"));
        GLManager.GL.Color4(1.0F, 1.0F, 1.0F, 1.0F);

        bool hovered = PointInBounds(e.MouseX, e.MouseY);
        int buttonTexture = Enabled ? (hovered ? 40 : 20) : 0;

        // Left border of button
        DrawTextureRegion(0, 0, 0, 46 + buttonTexture, 2, EffectiveHeight);

        for (int x = 2; x < EffectiveWidth - 2; x += 196)
        {
            int segmentWidth = Math.Min(196, EffectiveWidth - 2 - x);
            // Middle segments of button
            DrawTextureRegion(x, 0, 2, 46 + buttonTexture, segmentWidth, EffectiveHeight);
        }

        // Right border of button
        DrawTextureRegion(EffectiveWidth - 2, 0, 200 - 2, 46 + buttonTexture, 2, EffectiveHeight);

        uint color = Enabled ? (hovered ? 0xFFFFA0u : Foreground) : 0xA0A0A0u;

        int textX = 0;
        int textY = 0;
        int width = EffectiveWidth - 2; // padding for left and right borders
        int textWidth = Math.Min(width, font.GetStringWidth(Text));
        switch (TextAlign)
        {
            case Alignment.TopLeft:
                textX = 1 + 4;
                textY = 0;
                break;
            case Alignment.Top:
                textX = 1 + (width - textWidth) / 2;
                textY = 0;
                break;
            case Alignment.TopRight:
                textX = 1 + width - textWidth;
                textY = 0;
                break;
            case Alignment.Left:
                textX = 1 + 4;
                textY = 0 + (EffectiveHeight - 8) / 2;
                break;
            case Alignment.Center:
                textX = 1 + (width - textWidth) / 2;
                textY = 0 + (EffectiveHeight - 8) / 2;
                break;
            case Alignment.Right:
                textX = 1 + width - textWidth;
                textY = 0 + (EffectiveHeight - 8) / 2;
                break;
            case Alignment.BottomLeft:
                textX = 1 + 4;
                textY = 0 + EffectiveHeight - 8;
                break;
            case Alignment.Bottom:
                textX = 1 + (width - textWidth) / 2;
                textY = 0 + EffectiveHeight - 8;
                break;
            case Alignment.BottomRight:
                textX = 1 + width - textWidth;
                textY = 0 + EffectiveHeight - 8;
                break;
        }

        font.DrawStringWrappedWithShadow(Text, textX, textY, EffectiveWidth, color);

        /*if (!Enabled)
        {
            Gui.DrawCenteredString(font, Text, Width / 2, (Height - 8) / 2, 0xA0A0A0);
        }
        else if (hovered)
        {
            Gui.DrawCenteredString(font, Text, Width / 2, (Height - 8) / 2, 0xFFFFA0);
        }
        else
        {
            Gui.DrawCenteredString(font, Text, Width / 2, (Height - 8) / 2, 0xE0E0E0);
        }*/
    }
}
