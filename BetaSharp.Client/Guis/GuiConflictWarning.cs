using BetaSharp.Client.Guis.Controls;

namespace BetaSharp.Client.Guis;

public class GuiConflictWarning : Screen
{
    public GuiConflictWarning()
    {
        Button backButton = new(EffectiveWidth / 2 - 100, EffectiveHeight / 4 + 120 + 12, "Back to title screen");
        backButton.Clicked += (_, _) => MC.OpenScreen(new GuiMainMenu());
        AddChild(backButton);
    }

    protected override void OnRender(RenderEventArgs e)
    {
        DrawDefaultBackground();
        Gui.DrawCenteredString(FontRenderer, "Level save conflict", EffectiveWidth / 2, EffectiveHeight / 4 - 60 + 20, 0xFFFFFF);
        Gui.DrawString(FontRenderer, "Minecraft detected a conflict in the level save data.", EffectiveWidth / 2 - 140, EffectiveHeight / 4 - 60 + 60 + 0, 0xA0A0A0);
        Gui.DrawString(FontRenderer, "This could be caused by two copies of the game", EffectiveWidth / 2 - 140, EffectiveHeight / 4 - 60 + 60 + 18, 0xA0A0A0);
        Gui.DrawString(FontRenderer, "accessing the same level.", EffectiveWidth / 2 - 140, EffectiveHeight / 4 - 60 + 60 + 27, 0xA0A0A0);
        Gui.DrawString(FontRenderer, "To prevent level corruption, the current game has quit.", EffectiveWidth / 2 - 140, EffectiveHeight / 4 - 60 + 60 + 45, 0xA0A0A0);
    }
}
