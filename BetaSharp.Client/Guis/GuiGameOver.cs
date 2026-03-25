using BetaSharp.Client.Guis.Controls;
using BetaSharp.Client.Rendering.Core;

namespace BetaSharp.Client.Guis;

public class GuiGameOver : Screen
{
    public override bool PausesGame => false;

    public GuiGameOver()
    {
        int buttonLeft = EffectiveWidth / 2 - 100;
        int buttonTop = EffectiveHeight / 4 + 72;
        Button respawnButton = new(buttonLeft, buttonTop, "Respawn") { Enabled = MC.session != null };
        Button titleButton = new(buttonLeft, buttonTop + 24, "Title menu") { Enabled = MC.session != null };
        respawnButton.Clicked += (_, _) =>
        {
            MC.player.respawn();
            MC.OpenScreen(null);
        };
        titleButton.Clicked += (_, _) =>
        {
            MC.changeWorld(null);
            MC.OpenScreen(new GuiMainMenu());
        };
        AddChildren(respawnButton, titleButton);
    }

    protected override void OnRender(RenderEventArgs e)
    {
        Gui.DrawGradientRect(0, 0, EffectiveWidth, EffectiveHeight, 0x60500000, 0xA0803030);
        GLManager.GL.PushMatrix();
        GLManager.GL.Scale(2.0F, 2.0F, 2.0F);
        Gui.DrawCenteredString(FontRenderer, "Game over!", EffectiveWidth / 2 / 2, 30, 0xFFFFFF);
        GLManager.GL.PopMatrix();
        Gui.DrawCenteredString(FontRenderer, "Score: &e" + MC.player.getScore(), EffectiveWidth / 2, 100, 0xFFFFFF);
    }
}
