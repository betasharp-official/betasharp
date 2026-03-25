using BetaSharp.Client.Guis;

namespace BetaSharp.Client.UI;

public class UIScreenAdapter : GuiScreen
{
    private UIScreen _screen;

    public UIScreenAdapter(UIScreen screen)
    {
        _screen = screen;
    }

    public override void InitGui()
    {
        _screen.Game = Game;
        Input.Keyboard.enableRepeatEvents(true);
    }

    public override void OnGuiClosed()
    {
        _screen.Uninit();
        Input.Keyboard.enableRepeatEvents(false);
    }

    public override void Render(int mouseX, int mouseY, float partialTicks)
    {
        _screen.Render(mouseX, mouseY, partialTicks);
    }

    public override void UpdateScreen()
    {
        _screen.Update(1.0f);
    }

    public override void HandleMouseInput()
    {
        _screen.HandleMouseInput();
    }

    public override void HandleKeyboardInput()
    {
        _screen.HandleKeyboardInput();
    }
}
