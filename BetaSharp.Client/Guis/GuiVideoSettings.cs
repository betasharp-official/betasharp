using BetaSharp.Client.Guis.Controls;
using BetaSharp.Client.Options;

namespace BetaSharp.Client.Guis;

public class GuiVideoSettings : Screen
{
    private readonly Screen _parentScreen;
    private readonly GameOptions _options;

    public GuiVideoSettings(Screen parent, GameOptions options)
    {
        _parentScreen = parent;
        _options = options;
        DisplayTitle = true;

        TranslationStorage translations = TranslationStorage.Instance;
        Text = translations.TranslateKey("options.videoTitle");
        DisplayTitle = true;

        Control container = new(EffectiveWidth / 2 - 155, EffectiveHeight / 6, 310, 188);

        for (int i = 0; i < _options.VideoScreenOptions.Length; i++)
        {
            GameOption option = _options.VideoScreenOptions[i];
            int x = (i % 2) * 160;
            int y = 24 * (i / 2);

            switch (option)
            {
                case FloatOption floatOpt:
                    container.AddChild(new OptionsSlider(x, y, floatOpt));
                    break;
                case BoolOption boolOpt:
                    container.AddChild(new ToggleButton(x, y, boolOpt));
                    break;
                case CycleOption cycleOpt:
                    var cycleButton = new CycleButton(x, y, cycleOpt);
                    container.AddChild(cycleButton);
                    if (option == _options.GuiScaleOption)
                    {
                        cycleButton.Clicked += (_, _) =>
                        {
                            ScaledResolution scaled = new(MC.options, MC.displayWidth, MC.displayHeight);
                            int scaledWidth = scaled.ScaledWidth;
                            int scaledHeight = scaled.ScaledHeight;
                            SetWorldAndResolution(MC, scaledWidth, scaledHeight);
                        };
                    }
                    break;
            }
        }

        Button doneButton = new(55, 168, translations.TranslateKey("gui.done"));
        doneButton.Clicked += (_, _) =>
        {
            MC.options.SaveOptions();
            MC.OpenScreen(_parentScreen);
        };

        container.AddChild(doneButton);
        AddChild(container);
    }

    protected override void OnRender(RenderEventArgs e)
    {
        DrawDefaultBackground();
    }
}
