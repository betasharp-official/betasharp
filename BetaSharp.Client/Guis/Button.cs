using BetaSharp.Client.Rendering;
using BetaSharp.Client.Rendering.Core;
using Silk.NET.OpenGL.Legacy;

namespace BetaSharp.Client.Guis;

public class Button : Control
{
    public override bool Focusable => true;
    public Button(int x, int y, string text) : this(x, y, 200, 20, text) { }
    public Button(int x, int y, int width, int height, string text) : base(x, y, width, height)
    {
        Text = text;
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
        // Left half of button
        DrawTextureRegion(X, Y, 0, 46 + buttonTexture, Width / 2, Height);
        // Right half of button
        DrawTextureRegion(X + Width / 2, Y, 200 - Width / 2, 46 + buttonTexture, Width / 2, Height);

        if (!Enabled)
        {
            Gui.DrawCenteredString(font, Text, X + Width / 2, Y + (Height - 8) / 2, 0xA0A0A0);
        }
        else if (hovered)
        {
            Gui.DrawCenteredString(font, Text, X + Width / 2, Y + (Height - 8) / 2, 0xFFFFA0);
        }
        else
        {
            Gui.DrawCenteredString(font, Text, X + Width / 2, Y + (Height - 8) / 2, 0xE0E0E0);
        }
    }
}
