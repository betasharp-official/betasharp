using BetaSharp.Client.Input;
using BetaSharp.Client.Options;

namespace BetaSharp.Client.Guis;

public class GuiControllerControls : GuiScreen
{
    private const int ButtonBindings = 100;
    private const int SensitivityId = 300;
    private const int ControllerTypeId = 301;
    private const int ResetDefaultsId = 302;
    private const int ButtonDone = 200;

    private readonly GuiScreen _parentScreen;
    private readonly GameOptions _options;

    public GuiControllerControls(GuiScreen parentScreen, GameOptions options)
    {
        _parentScreen = parentScreen;
        _options = options;
    }

    public override void InitGui()
    {
        TranslationStorage translations = TranslationStorage.Instance;
        int cx = Width / 2;

        _controlList.Add(new GuiSlider(SensitivityId, cx - 155, Height / 6 + 24,
            _options.ControllerSensitivityOption,
            _options.ControllerSensitivityOption.GetDisplayString(translations),
            _options.ControllerSensitivityOption.Value).Size(150, 20));

        _controlList.Add(new GuiSmallButton(ControllerTypeId, cx + 5, Height / 6 + 24,
            _options.ControllerTypeOption,
            _options.ControllerTypeOption.GetDisplayString(translations)).Size(150, 20));

        _controlList.Add(new GuiButton(ButtonBindings, cx - 155, Height / 6 + 54, 150, 20, "Edit Bindings..."));

        _controlList.Add(new GuiSmallButton(ResetDefaultsId, cx + 5, Height / 6 + 54, 150, 20, "Reset Bindings"));

        _controlList.Add(new GuiButton(ButtonDone, cx - 100, Height / 6 + 168, translations.TranslateKey("gui.done")));
    }

    protected override void ActionPerformed(GuiButton button)
    {
        switch (button.Id)
        {
            case ButtonBindings:
                Game.displayGuiScreen(new GuiControllerBindings(this, _options));
                return;

            case ControllerTypeId:
                _options.ControllerTypeOption.Cycle();
                button.DisplayString = _options.ControllerTypeOption.GetDisplayString(TranslationStorage.Instance);
                _options.SaveOptions();
                return;

            case ResetDefaultsId:
                foreach (ControllerBinding cb in _options.ControllerBindings)
                    cb.Button = cb.DefaultButton;
                _options.SaveOptions();
                return;

            case ButtonDone:
                Game.options.SaveOptions();
                Game.displayGuiScreen(_parentScreen);
                return;
        }
    }

    protected override void KeyTyped(char eventChar, int eventKey)
    {
        if (eventKey == Keyboard.KEY_ESCAPE || eventKey == Keyboard.KEY_NONE)
        {
            Game.options.SaveOptions();
            Game.displayGuiScreen(_parentScreen);
        }
    }

    public override void Render(int mouseX, int mouseY, float partialTicks)
    {
        DrawDefaultBackground();
        DrawCenteredString(FontRenderer, "Controller Settings", Width / 2, 20, Color.White);
        base.Render(mouseX, mouseY, partialTicks);
    }
}
