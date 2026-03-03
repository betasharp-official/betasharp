using BetaSharp.Client.Options;

namespace BetaSharp.Client.Guis;

public class GuiControls : GuiScreen
{

    private readonly GuiScreen _parentScreen;
    protected string _screenTitle = "Controls";
    private readonly GameOptions _options;
    private int _selectedKey = -1;
    private const int ButtonDone = 200;
    private const int SensitivityId = 300;
    private const int InvertMouseId = 301;

    public GuiControls(GuiScreen parentScreen, GameOptions options)
    {
        _parentScreen = parentScreen;
        _options = options;
    }

    private int getLeftColumnX()
    {
        return Width / 2 - 155;
    }

    public override void InitGui()
    {
        TranslationStorage translations = TranslationStorage.Instance;
        int leftX = getLeftColumnX();

        for (int i = 0; i < _options.KeyBindings.Length; ++i)
        {
            _controlList.Add(new GuiSmallButton(i, leftX + i % 2 * 160, Height / 6 + 24 * (i >> 1), 70, 20, _options.GetOptionDisplayString(i)));
        }

        _controlList.Add(new GuiSlider(SensitivityId, Width / 2 + 5, Height / 6 + 130, _options.MouseSensitivityOption, _options.MouseSensitivityOption.GetDisplayString(translations), _options.MouseSensitivityOption.Value).Size(125, 20));
        _controlList.Add(new GuiSmallButton(InvertMouseId, Width / 2 - 155, Height / 6 + 130, _options.InvertMouseOption, _options.InvertMouseOption.GetDisplayString(translations)).Size(125, 20));

        _controlList.Add(new GuiButton(ButtonDone, Width / 2 - 100, Height / 6 + 168, translations.TranslateKey("gui.done")));
        _screenTitle = translations.TranslateKey("controls.title");
    }

    protected override void ActionPerformed(GuiButton button)
    {
        for (int i = 0; i < _options.KeyBindings.Length; ++i)
        {
            _controlList[i].DisplayString = _options.GetOptionDisplayString(i);
        }

        switch (button.Id)
        {
            case ButtonDone:
                Game.displayGuiScreen(_parentScreen);
                break;
            case InvertMouseId:
                _options.InvertMouseOption.Toggle();
                button.DisplayString = _options.InvertMouseOption.GetDisplayString(TranslationStorage.Instance);
                _options.SaveOptions();
                break;
            default:
                if (button.Id >= 0 && button.Id < _options.KeyBindings.Length)
                {
                    _selectedKey = button.Id;
                    button.DisplayString = "> " + _options.GetOptionDisplayString(button.Id) + " <";
                }
                break;
        }

    }

    protected override void KeyTyped(char eventChar, int eventKey)
    {
        if (_selectedKey >= 0)
        {
            _options.SetKeyBinding(_selectedKey, eventKey);
            _controlList[_selectedKey].DisplayString = _options.GetOptionDisplayString(_selectedKey);
            _selectedKey = -1;
        }
        else
        {
            base.KeyTyped(eventChar, eventKey);
        }

    }

    public override void Render(int mouseX, int mouseY, float partialTicks)
    {
        DrawDefaultBackground();
        DrawCenteredString(FontRenderer, _screenTitle, Width / 2, 20, Color.White);
        int leftX = getLeftColumnX();

        for (int i = 0; i < _options.KeyBindings.Length; ++i)
        {
            DrawString(FontRenderer, _options.GetKeyBindingDescription(i), leftX + i % 2 * 160 + 70 + 6, Height / 6 + 24 * (i >> 1) + 7, Color.White);
        }

        base.Render(mouseX, mouseY, partialTicks);
    }
}
