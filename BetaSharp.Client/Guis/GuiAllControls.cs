using BetaSharp.Client.Options;

namespace BetaSharp.Client.Guis;

public class GuiAllControls : GuiScreen
{
    private const int ButtonKeyboard   = 100;
    private const int ButtonController = 101;
    private const int ButtonDone       = 200;

    private readonly GuiScreen _parentScreen;
    private readonly GameOptions _options;

    public GuiAllControls(GuiScreen parentScreen, GameOptions options)
    {
        _parentScreen = parentScreen;
        _options      = options;
    }

    public override void InitGui()
    {
        _controlList.Add(new GuiButton(ButtonKeyboard,   Width / 2 - 100, Height / 6 + 36,  "Keyboard Controls..."));
        _controlList.Add(new GuiButton(ButtonController, Width / 2 - 100, Height / 6 + 62,  "Controller Controls..."));
        _controlList.Add(new GuiButton(ButtonDone,       Width / 2 - 100, Height / 6 + 168, "Done"));
    }

    protected override void ActionPerformed(GuiButton button)
    {
        switch (button.Id)
        {
            case ButtonKeyboard:
                Game.displayGuiScreen(new GuiControls(this, _options));
                break;
            case ButtonController:
                Game.displayGuiScreen(new GuiControllerControls(this, _options));
                break;
            case ButtonDone:
                Game.options.SaveOptions();
                Game.displayGuiScreen(_parentScreen);
                break;
        }
    }

    protected override void KeyTyped(char eventChar, int eventKey)
    {
        if (eventKey == Input.Keyboard.KEY_ESCAPE || eventKey == Input.Keyboard.KEY_NONE)
        {
            Game.options.SaveOptions();
            Game.displayGuiScreen(_parentScreen);
        }
    }

    public override void Render(int mouseX, int mouseY, float partialTicks)
    {
        DrawDefaultBackground();
        DrawCenteredString(FontRenderer, "Controls", Width / 2, 20, Color.White);
        base.Render(mouseX, mouseY, partialTicks);
    }
}
