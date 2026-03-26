using BetaSharp.Client.Input;
using BetaSharp.Client.Options;
using Silk.NET.GLFW;

namespace BetaSharp.Client.Guis;

public class GuiControllerBindings : GuiScreen
{
    private const int ButtonDone = 200;

    private int _listeningIndex = -1;

    private readonly GuiScreen _parentScreen;
    private readonly GameOptions _options;

    private readonly bool[] _buttonSnapshot = new bool[15];

    public GuiControllerBindings(GuiScreen parentScreen, GameOptions options)
    {
        _parentScreen = parentScreen;
        _options = options;
    }

    private int LeftColumnX => Width / 2 - 155;

    public override void InitGui()
    {
        int leftX = LeftColumnX;

        for (int i = 0; i < _options.ControllerBindings.Length; ++i)
        {
            int col = i % 2;
            int row = i / 2;
            _controlList.Add(new GuiSmallButton(i, leftX + col * 160 + 80, Height / 6 + 24 * row, 70, 20,
                _options.ControllerBindings[i].GetButtonName()));
        }

        _controlList.Add(new GuiButton(ButtonDone, Width / 2 - 100, Height / 6 + 168,
            TranslationStorage.Instance.TranslateKey("gui.done")));
    }

    protected override void ActionPerformed(GuiButton button)
    {
        for (int i = 0; i < _options.ControllerBindings.Length; ++i)
            _controlList[i].DisplayString = _options.ControllerBindings[i].GetButtonName();

        if (button.Id == ButtonDone)
        {
            _listeningIndex = -1;
            Game.options.SaveOptions();
            Game.displayGuiScreen(_parentScreen);
            return;
        }

        if (button.Id >= 0 && button.Id < _options.ControllerBindings.Length)
        {
            _listeningIndex = button.Id;
            TakeButtonSnapshot();
            button.DisplayString = "> ??? <";
        }
    }

    public override void HandleControllerInput()
    {
        if (_listeningIndex >= 0)
        {
            return;
        }
        base.HandleControllerInput();
    }

    public override void UpdateScreen()
    {
        if (_listeningIndex < 0) return;

        if (Controller.IsButtonDown(GamepadButton.Back))
        {
            CancelListening();
            return;
        }

        for (int i = 0; i < 15; ++i)
        {
            bool isDown = Controller.IsButtonDown((GamepadButton)i);

            if (!isDown && _buttonSnapshot[i])
            {
                _buttonSnapshot[i] = false;
                continue;
            }

            if (isDown && !_buttonSnapshot[i])
            {
                GamepadButton pressed = (GamepadButton)i;
                if (pressed == GamepadButton.Back) continue;

                _options.ControllerBindings[_listeningIndex].Button = pressed;
                _options.SaveOptions();
                _listeningIndex = -1;
                Controller.ClearEvents();
                InitGui();
                return;
            }
        }
    }

    protected override void KeyTyped(char eventChar, int eventKey)
    {
        if (_listeningIndex >= 0)
        {
            if (eventKey == Keyboard.KEY_ESCAPE) CancelListening();
            return;
        }

        if (eventKey == Keyboard.KEY_ESCAPE || eventKey == Keyboard.KEY_NONE)
        {
            Game.options.SaveOptions();
            Game.displayGuiScreen(_parentScreen);
        }
        else
        {
            base.KeyTyped(eventChar, eventKey);
        }
    }

    private void CancelListening()
    {
        int idx = _listeningIndex;
        _listeningIndex = -1;
        if (idx >= 0 && idx < _controlList.Count)
            _controlList[idx].DisplayString = _options.ControllerBindings[idx].GetButtonName();
    }

    private void TakeButtonSnapshot()
    {
        for (int i = 0; i < 15; ++i)
            _buttonSnapshot[i] = Controller.IsButtonDown((GamepadButton)i);
    }

    public override void Render(int mouseX, int mouseY, float partialTicks)
    {
        DrawDefaultBackground();
        DrawCenteredString(FontRenderer, "Button Bindings", Width / 2, 20, Color.White);

        int leftX = LeftColumnX;
        for (int i = 0; i < _options.ControllerBindings.Length; ++i)
        {
            int col = i % 2;
            int row = i / 2;
            DrawString(FontRenderer,
                _options.ControllerBindings[i].Description,
                leftX + col * 160 + 2,
                Height / 6 + 24 * row + 7,
                Color.White);
        }

        if (_listeningIndex >= 0)
        {
            DrawCenteredString(FontRenderer,
                "Press a button  |  [Back] or [Esc] to cancel",
                Width / 2, Height - 30,
                Color.FromArgb(0xFFFFAA00));
        }

        base.Render(mouseX, mouseY, partialTicks);
    }
}
