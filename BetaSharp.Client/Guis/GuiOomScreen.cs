namespace BetaSharp.Client.Guis;

public class GuiOomScreen : Screen
{
    protected override void OnRender(RenderEventArgs e)
    {
        DrawDefaultBackground();
        int center = Width / 2;
        int textLeft = center - 140;
        int y = Height / 4 - 60 + 20;
        Gui.DrawCenteredString(FontRenderer, "Out of memory!", textLeft, y, 0xFFFFFF);
        y = Height / 4 - 60 + 60;
        Gui.DrawCenteredString(FontRenderer, "Minecraft has run out of memory.", textLeft, y, 0xA0A0A0);
        Gui.DrawCenteredString(FontRenderer, "This could be caused by a bug in the game or by your", textLeft, y + 18, 0xA0A0A0);
        Gui.DrawCenteredString(FontRenderer, "system not having enough memory.", textLeft, y + 27, 0xA0A0A0);
        Gui.DrawCenteredString(FontRenderer, "To prevent level corruption, the current game has quit.", textLeft, y + 45, 0xA0A0A0);
        Gui.DrawCenteredString(FontRenderer, "Please restart the game.", textLeft, y + 63, 0xA0A0A0);
    }
}
