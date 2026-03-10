using BetaSharp.Client.Input;
using BetaSharp.Client.Options;

namespace BetaSharp.Client.Guis;

public class GuiVideoSettings : GuiScreen
{

    private readonly GuiScreen _parentScreen;
    protected string _screenTitle = "Video Settings";
    private readonly GameOptions _gameOptions;

    public GuiVideoSettings(GuiScreen parent, GameOptions options)
    {
        _parentScreen = parent;
        _gameOptions = options;
    }

    public override void InitGui()
    {
        TranslationStorage translations = TranslationStorage.Instance;
        _screenTitle = translations.TranslateKey("options.videoTitle");
        int optionIndex = 0;

        foreach (GameOption option in _gameOptions.VideoScreenOptions)
        {
            int x = Width / 2 - 155 + (optionIndex % 2) * 160;
            int y = Height / 6 + 24 * (optionIndex / 2);
            int id = optionIndex;

            if (option is FloatOption floatOpt)
            {
                _controlList.Add(new GuiSlider(id, x, y, floatOpt, option.GetDisplayString(translations), floatOpt.Value));
            }
            else
            {
                _controlList.Add(new GuiSmallButton(id, x, y, option, option.GetDisplayString(translations)));
            }

            optionIndex++;
        }

        _controlList.Add(new GuiButton(200, Width / 2 - 100, Height / 6 + 168, translations.TranslateKey("gui.done")));
    }

    protected override void ActionPerformed(GuiButton btn)
    {
        if (btn.Enabled)
        {
            if (btn is GuiSmallButton smallBtn && smallBtn.Option != null)
            {
                smallBtn.ClickOption();
                btn.DisplayString = smallBtn.Option.GetDisplayString(TranslationStorage.Instance);
            }

            if (btn.Id == 200)
            {
                Game.options.SaveOptions();
                Game.displayGuiScreen(_parentScreen);
            }

            if (btn is GuiSmallButton { Option: CycleOption } guiScaleBtn
                && guiScaleBtn.Option == _gameOptions.GuiScaleOption)
            {
                ScaledResolution scaled = new(Game.options, Game.displayWidth, Game.displayHeight);
                int scaledWidth = scaled.ScaledWidth;
                int scaledHeight = scaled.ScaledHeight;
                SetWorldAndResolution(Game, scaledWidth, scaledHeight);
            }
        }
    }

    public override void Render(int mouseX, int mouseY, float partialTicks)
    {
        DrawDefaultBackground();
        DrawCenteredString(FontRenderer, _screenTitle, Width / 2, 20, Color.White);
        base.Render(mouseX, mouseY, partialTicks);
    }

    protected override void KeyTyped(char eventChar, int eventKey)
    {
        if (eventKey == Keyboard.KEY_ESCAPE || eventKey == Keyboard.KEY_NONE)
        {
            Game.options.SaveOptions();
            Game.displayGuiScreen(_parentScreen);
        }
    }
}
