namespace BetaSharp.Client.Guis;

public class GuiYesNo : Screen
{
    private readonly string _message1;
    private readonly string _message2;

    public GuiYesNo(Screen parentScreen, string message1, string message2, string confirmButtonText, string cancelButtonText, int worldNumber)
    {
        _message1 = message1;
        _message2 = message2;
        Button confirmButton = new(Width / 2 - 155, Height / 6 + 96, confirmButtonText) { Size = new(150, 20) };
        Button cancelButton = new(Width / 2 - 155 + 160, Height / 6 + 96, cancelButtonText) { Size = new(150, 20) };
        confirmButton.Clicked += (_, _) => parentScreen.DeleteWorld(true, worldNumber);
        cancelButton.Clicked += (_, _) => parentScreen.DeleteWorld(false, worldNumber);
        AddChildren(confirmButton, cancelButton);
    }

    protected override void OnRender(RenderEventArgs e)
    {
        DrawDefaultBackground();
        Gui.DrawCenteredString(FontRenderer, _message1, Width / 2, 70, 0xFFFFFF);
        Gui.DrawCenteredString(FontRenderer, _message2, Width / 2, 90, 0xFFFFFF);
    }
}
