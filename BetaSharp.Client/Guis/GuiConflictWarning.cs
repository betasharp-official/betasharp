namespace BetaSharp.Client.Guis;

public class GuiConflictWarning : Screen
{
    public GuiConflictWarning()
    {
        Button backButton = new(Width / 2 - 100, Height / 4 + 120 + 12, "Back to title screen");
        backButton.Clicked += (_, _) => MC.OpenScreen(new GuiMainMenu());
        AddChild(backButton);
    }

    protected override void OnRender(RenderEventArgs e)
    {
        DrawDefaultBackground();
        Gui.DrawCenteredString(FontRenderer, "Level save conflict", Width / 2, Height / 4 - 60 + 20, 0xFFFFFF);
        Gui.DrawString(FontRenderer, "Minecraft detected a conflict in the level save data.", Width / 2 - 140, Height / 4 - 60 + 60 + 0, 0xA0A0A0);
        Gui.DrawString(FontRenderer, "This could be caused by two copies of the game", Width / 2 - 140, Height / 4 - 60 + 60 + 18, 0xA0A0A0);
        Gui.DrawString(FontRenderer, "accessing the same level.", Width / 2 - 140, Height / 4 - 60 + 60 + 27, 0xA0A0A0);
        Gui.DrawString(FontRenderer, "To prevent level corruption, the current game has quit.", Width / 2 - 140, Height / 4 - 60 + 60 + 45, 0xA0A0A0);
    }
}
