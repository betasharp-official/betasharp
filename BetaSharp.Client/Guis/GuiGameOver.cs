using BetaSharp.Client.Rendering.Core;

namespace BetaSharp.Client.Guis;

public class GuiGameOver : GuiScreen
{
    private const int ButtonRespawn = 1;
    private const int ButtonTitle = 2;

    public override bool PausesGame => false;

    public override void InitGui()
    {
        _controlList.Clear();
        _controlList.Add(new GuiButton(ButtonRespawn, Width / 2 - 100, Height / 4 + 72, "Respawn"));
        _controlList.Add(new GuiButton(ButtonTitle, Width / 2 - 100, Height / 4 + 96, "Title menu"));
        if (Game.session == null)
        {
            for (int i = 0; i < _controlList.Count; ++i)
            {
                GuiButton btn = _controlList[i];
                if (btn.Id == ButtonRespawn)
                {
                    btn.Enabled = false;
                    break;
                }
            }
        }

    }

    protected override void KeyTyped(char eventChar, int eventKey)
    {
    }

    protected override void ActionPerformed(GuiButton button)
    {
        switch (button.Id)
        {
            case ButtonRespawn:
                Game.player.respawn();
                Game.displayGuiScreen(null);
                break;
            case ButtonTitle:
                Game.changeWorld(null);
                Game.displayGuiScreen(new GuiMainMenu());
                break;
        }

    }

    public override void Render(int mouseX, int mouseY, float partialTicks)
    {
        DrawGradientRect(0, 0, Width, Height, Color.GameOverBackgroundDarkRed, Color.GameOverBackgroundRed);
        GLManager.GL.PushMatrix();
        GLManager.GL.Scale(2.0F, 2.0F, 2.0F);
        DrawCenteredString(FontRenderer, "Game over!", Width / 2 / 2, 30, Color.White);
        GLManager.GL.PopMatrix();
        DrawCenteredString(FontRenderer, "Score: &e" + Game.player.getScore(), Width / 2, 100, Color.White);
        base.Render(mouseX, mouseY, partialTicks);
    }
}
