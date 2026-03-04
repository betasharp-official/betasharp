using BetaSharp.Client.Options;

namespace BetaSharp.Client.Guis;

public class GuiDebugOptions : Screen
{
    public GuiDebugOptions(Screen parent, GameOptions options)
    {
        Text = "Debug Options";
        DisplayTitle = true;

        TranslationStorage translations = TranslationStorage.Instance;

        Control container = new(Width / 2 - 155, Height / 6, 310, 188);
        for (int i = 0; i < options.DebugScreenOptions.Length; i++)
        {
            GameOption option = options.DebugScreenOptions[i];
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
                    container.AddChild(new CycleButton(x, y, cycleOpt));
                    break;
            }
        }

        Button doneButton = new(55, 168, translations.TranslateKey("gui.done"));
        doneButton.Clicked += (_, _) =>
        {
            options.SaveOptions();
            MC.OpenScreen(parent);
        };
        container.AddChild(doneButton);
        AddChild(container);
    }

    protected override void OnRender(RenderEventArgs e)
    {
        DrawDefaultBackground();
    }
}
