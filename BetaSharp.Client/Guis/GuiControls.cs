using BetaSharp.Client.Guis.Controls;
using BetaSharp.Client.Options;

namespace BetaSharp.Client.Guis;

public class GuiControls : Screen
{
    private readonly GameOptions _options;

    public GuiControls(Screen parentScreen, GameOptions options)
    {
        _options = options;

        TranslationStorage translations = TranslationStorage.Instance;
        int leftX = GetLeftColumnX();

        Control container = new(leftX, EffectiveHeight / 6, 310, 188);

        for (int i = 0; i < _options.KeyBindings.Length; ++i)
        {
            container.AddChild(new ControlsButton(i % 2 * 160, 24 * (i >> 1), _options.KeyBindings[i]));
        }

        OptionsSlider sensitivitySlider = new(150, 130, _options.MouseSensitivityOption)
            { EffectiveSize = new(125, 20) };
        ToggleButton invertMouseButton = new(0, 130, _options.InvertMouseOption)
            { EffectiveSize = new(125, 20) };
        Button doneButton = new(55, 168, translations.TranslateKey("gui.done"));
        doneButton.Clicked += (_, _) =>
        {
            _options.SaveOptions();
            MC.OpenScreen(parentScreen);
        };

        container.AddChildren(sensitivitySlider, invertMouseButton, doneButton);
        AddChild(container);
    }

    protected override void OnRender(RenderEventArgs e)
    {
        DrawDefaultBackground();
        int leftX = GetLeftColumnX();

        for (int i = 0; i < _options.KeyBindings.Length; ++i)
        {
            Gui.DrawString(FontRenderer, _options.GetKeyBindingDescription(i), leftX + i % 2 * 160 + 70 + 6, EffectiveHeight / 6 + 24 * (i >> 1) + 7, 0xFFFFFFFF);
        }
    }

    private int GetLeftColumnX() => EffectiveWidth / 2 - 155;
}
