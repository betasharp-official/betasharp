using BetaSharp.Client.Guis;

namespace BetaSharp.Client.UI;

//TODO: REMOVE THIS ONCE LEGACY GUIS ARE PORTED
public class UIScreenAdapter : GuiScreen
{
    public UIScreen Screen { get; }
    public override bool PausesGame => Screen.PausesGame;

    public UIScreenAdapter(UIScreen screen)
    {
        Screen = screen;
    }

    public override void InitGui()
    {
        Screen.Game = Game;
        Screen.Initialize();
        Input.Keyboard.enableRepeatEvents(true);
    }

    public override void OnGuiClosed()
    {
        Screen.Uninit();
        Input.Keyboard.enableRepeatEvents(false);
    }

    public override void Render(int mouseX, int mouseY, float partialTicks)
    {
        Screen.Render(mouseX, mouseY, partialTicks);
    }

    public override void UpdateScreen()
    {
        Screen.Update(1.0f);
    }

    public override void HandleMouseInput()
    {
        Screen.HandleMouseInput();
    }

    public override void HandleKeyboardInput()
    {
        Screen.HandleKeyboardInput();
    }
}
