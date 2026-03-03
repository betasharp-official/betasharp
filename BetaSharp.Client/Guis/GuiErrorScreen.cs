namespace BetaSharp.Client.Guis;

public class GuiErrorScreen : GuiScreen
{
    public override void UpdateScreen()
    {
    }

    public override void InitGui()
    {
    }

    protected override void ActionPerformed(GuiButton btt)
    {
    }

    protected override void KeyTyped(char eventChar, int eventKey)
    {
    }

    public override void Render(int mouseX, int mouseY, float partialTicks)
    {
        DrawDefaultBackground();
        DrawCenteredString(FontRenderer, "Out of memory!", Width / 2, Height / 4 - 60 + 20, Color.White);
        DrawString(FontRenderer, "Minecraft has run out of memory.", Width / 2 - 140, Height / 4 - 60 + 60 + 0, Color.GrayA0);
        DrawString(FontRenderer, "This could be caused by a bug in the Game or by the", Width / 2 - 140, Height / 4 - 60 + 60 + 18, Color.GrayA0);
        DrawString(FontRenderer, "Java Virtual Machine not being allocated enough", Width / 2 - 140, Height / 4 - 60 + 60 + 27, Color.GrayA0);
        DrawString(FontRenderer, "memory. If you are playing in a web browser, try", Width / 2 - 140, Height / 4 - 60 + 60 + 36, Color.GrayA0);
        DrawString(FontRenderer, "downloading the Game and playing it offline.", Width / 2 - 140, Height / 4 - 60 + 60 + 45, Color.GrayA0);
        DrawString(FontRenderer, "To prevent level corruption, the current Game has quit.", Width / 2 - 140, Height / 4 - 60 + 60 + 63, Color.GrayA0);
        DrawString(FontRenderer, "Please restart the Game.", Width / 2 - 140, Height / 4 - 60 + 60 + 81, Color.GrayA0);
        base.Render(mouseX, mouseY, partialTicks);
    }
}
