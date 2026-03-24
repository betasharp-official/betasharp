using BetaSharp.Client.Options;

namespace BetaSharp.Client.Guis;

public class GuiOptions : GuiScreen
{
    private const int ButtonVideoSettings = 101;
    private const int ButtonAudioSettings = 102;
    private const int ButtonControls = 100;
    private const int ButtonDebugSettings = 103;
    private const int ButtonDone = 200;

    private readonly GuiScreen _parentScreen;
    private readonly GameOptions _options;

    protected string _screenTitle = "Options";

    public GuiOptions(GuiScreen parentScreen, GameOptions gameOptions)
    {
        _parentScreen = parentScreen;
        _options = gameOptions;
    }

    public override void InitGui()
    {
        TranslationStorage translations = TranslationStorage.Instance;
        _screenTitle = translations.TranslateKey("options.title");
        int rowIndex = 0;

        foreach (GameOption option in _options.MainScreenOptions)
        {
            int xPos = Width / 2 - 155 + (rowIndex % 2 * 160);
            int yPos = Height / 6 + 24 * (rowIndex >> 1);
            int id = rowIndex;

            if (option is FloatOption floatOpt)
            {
                _controlList.Add(new GuiSlider(id, xPos, yPos, floatOpt, option.GetDisplayString(translations), floatOpt.Value));
            }
            else
            {
                _controlList.Add(new GuiSmallButton(id, xPos, yPos, option, option.GetDisplayString(translations)));
            }

            ++rowIndex;
        }

        _controlList.Add(new GuiSmallButton(ButtonVideoSettings, Width / 2 - 155, Height / 6 + 48 + 24, translations.TranslateKey("options.video")));
        _controlList.Add(new GuiSmallButton(ButtonDebugSettings, Width / 2 + 5, Height / 6 + 48 + 24, "Debug Options..."));
        _controlList.Add(new GuiSmallButton(ButtonAudioSettings, Width / 2 - 155, Height / 6 + 72 + 24, "Audio Settings"));
        _controlList.Add(new GuiSmallButton(ButtonControls, Width / 2 + 5, Height / 6 + 72 + 24, translations.TranslateKey("options.controls")));

        _controlList.Add(new GuiButton(ButtonDone, Width / 2 - 100, Height / 6 + 168, translations.TranslateKey("gui.done")));
    }

    protected override void ActionPerformed(GuiButton button)
    {
        if (!button.Enabled) return;

        if (button is GuiSmallButton smallBtn && smallBtn.Option != null)
        {
            smallBtn.ClickOption();
            button.DisplayString = smallBtn.Option.GetDisplayString(TranslationStorage.Instance);
        }

        switch (button.Id)
        {
            case ButtonVideoSettings:
                Game.options.SaveOptions();
                Game.displayGuiScreen(new GuiVideoSettings(this, _options));
                break;
            case ButtonAudioSettings:
                Game.options.SaveOptions();
                Game.displayGuiScreen(new GuiAudio(this, _options));
                break;
            case ButtonDebugSettings:
                Game.options.SaveOptions();
                Game.displayGuiScreen(new GuiDebugOptions(this, _options));
                break;
            case ButtonControls:
                Game.options.SaveOptions();
                Game.displayGuiScreen(new GuiAllControls(this, _options));
                break;
            case ButtonDone:
                Game.options.SaveOptions();
                Game.displayGuiScreen(_parentScreen);
                break;
        }

    }

    public override void Render(int mouseX, int mouseY, float partialTicks)
    {
        DrawDefaultBackground();
        DrawCenteredString(FontRenderer, _screenTitle, Width / 2, 20, Color.White);

        base.Render(mouseX, mouseY, partialTicks);
    }
}
