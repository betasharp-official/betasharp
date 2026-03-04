using BetaSharp.Client.Options;

namespace BetaSharp.Client.Guis;

public class GuiOptions : Screen
{
    public GuiOptions(Screen parentScreen, GameOptions options)
    {
        TranslationStorage translations = TranslationStorage.Instance;
        Text = translations.TranslateKey("options.title");
        DisplayTitle = true;

        int buttonLeft = Width / 2 - 100;
        int topY = Height / 6;

        Control container = new(buttonLeft - 55, topY, 310, 188);
        for (int i = 0; i < options.MainScreenOptions.Length; i++)
        {
            GameOption option = options.MainScreenOptions[i];
            int x = (i % 2 * 160);
            int y = (24 * (i / 2));

            switch (option)
            {
                case FloatOption floatOpt:
                    container.AddChild(new OptionsSlider(x, y, floatOpt));
                    break;
                case BoolOption boolOpt:
                    container.AddChild(new ToggleButton(x, y, boolOpt));
                    break;
                case CycleOption cycleOpt:
                    container.AddChild(new CycleButton(x, y, cycleOpt));
                    break;
            }
        }

        Button videoSettingsButton = new(0, 72, 150, 20, translations.TranslateKey("options.video"));
        Button debugSettingsButton = new(160, 72, 150, 20, "Debug Settings...");
        Button audioSettingsButton = new(0, 96, 150, 20, "Audio Settings");
        Button controlsButton = new(160, 96, 150, 20, translations.TranslateKey("options.controls"));
        Button doneButton = new(55, 168, translations.TranslateKey("gui.done"));
        videoSettingsButton.Clicked += (_, _) =>
        {
            MC.options.SaveOptions();
            MC.OpenScreen(new GuiVideoSettings(this, options));
        };
        debugSettingsButton.Clicked += (_, _) =>
        {
            MC.options.SaveOptions();
            MC.OpenScreen(new GuiDebugOptions(this, options));
        };
        audioSettingsButton.Clicked += (_, _) =>
        {
            MC.options.SaveOptions();
            MC.OpenScreen(new GuiAudio(this, options));
        };
        controlsButton.Clicked += (_, _) =>
        {
            MC.options.SaveOptions();
            MC.OpenScreen(new GuiControls(this, options));
        };
        doneButton.Clicked += (_, _) =>
        {
            MC.options.SaveOptions();
            MC.OpenScreen(parentScreen);
        };
        container.AddChildren(videoSettingsButton, debugSettingsButton, audioSettingsButton, controlsButton, doneButton);
        AddChild(container);
    }

    protected override void OnRender(RenderEventArgs e)
    {
        DrawDefaultBackground();
    }
}
