using BetaSharp.Client.Guis;
using BetaSharp.Client.Options;
using BetaSharp.Client.UI.Controls;
using BetaSharp.Client.UI.Layout.Flexbox;

namespace BetaSharp.Client.UI.Screens;

public class OptionsScreen : UIScreen
{
    private readonly UIScreen? _parent;
    private readonly GameOptions _options;
    private string _screenTitle = "Options";

    public OptionsScreen(UIScreen? parent, GameOptions options) : base(parent?.Game ?? BetaSharp.Instance)
    {
        _parent = parent;
        _options = options;
    }

    protected override void Init()
    {
        TranslationStorage translations = TranslationStorage.Instance;
        _screenTitle = translations.TranslateKey("options.title");

        Root.Style.AlignItems = Align.Center;
        Root.Style.JustifyContent = Justify.FlexStart;

        Root.AddChild(new Background(Game.world != null ? BackgroundType.World : BackgroundType.Dirt));

        Label title = new()
        {
            Text = _screenTitle,
            TextColor = Color.White,
            Centered = true
        };
        title.Style.MarginTop = 20;
        title.Style.MarginBottom = 12;
        Root.AddChild(title);

        Panel grid = new();
        grid.Style.FlexDirection = FlexDirection.Row;
        grid.Style.FlexWrap = Wrap.Wrap;
        grid.Style.JustifyContent = Justify.Center;
        grid.Style.Width = 340;
        grid.Style.MarginTop = 20;

        foreach (GameOption option in _options.MainScreenOptions)
        {
            UIElement control = CreateControlForOption(option);
            control.Style.SetMargin(2);
            control.Style.Width = 150;
            grid.AddChild(control);
        }
        Root.AddChild(grid);

        Panel subMenuGrid = new();
        subMenuGrid.Style.FlexDirection = FlexDirection.Row;
        subMenuGrid.Style.FlexWrap = Wrap.Wrap;
        subMenuGrid.Style.JustifyContent = Justify.Center;
        subMenuGrid.Style.Width = 340;
        subMenuGrid.Style.MarginTop = 10;

        Button btnVideo = new() { Text = translations.TranslateKey("options.video") };
        btnVideo.Style.SetMargin(2);
        btnVideo.Style.Width = 150;
        btnVideo.OnClick += (e) =>
        {
            _options.SaveOptions();
            Game.displayGuiScreen(new GuiVideoSettings(new UIScreenAdapter(this), _options));
        };
        subMenuGrid.AddChild(btnVideo);

        Button btnDebug = new() { Text = "Debug Options..." };
        btnDebug.Style.SetMargin(2);
        btnDebug.Style.Width = 150;
        btnDebug.OnClick += (e) =>
        {
            _options.SaveOptions();
            Game.displayGuiScreen(new GuiDebugOptions(new UIScreenAdapter(this), _options));
        };
        subMenuGrid.AddChild(btnDebug);

        Button btnAudio = new() { Text = "Audio Settings" };
        btnAudio.Style.SetMargin(2);
        btnAudio.Style.Width = 150;
        btnAudio.OnClick += (e) =>
        {
            _options.SaveOptions();
            Game.displayGuiScreen(new GuiAudio(new UIScreenAdapter(this), _options));
        };
        subMenuGrid.AddChild(btnAudio);

        Button btnControls = new() { Text = translations.TranslateKey("options.controls") };
        btnControls.Style.SetMargin(2);
        btnControls.Style.Width = 150;
        btnControls.OnClick += (e) =>
        {
            _options.SaveOptions();
            Game.displayGuiScreen(new GuiAllControls(new UIScreenAdapter(this), _options));
        };
        subMenuGrid.AddChild(btnControls);

        Root.AddChild(subMenuGrid);

        Panel spacer = new();
        spacer.Style.FlexGrow = 1;
        Root.AddChild(spacer);

        Button btnDone = new() { Text = translations.TranslateKey("gui.done") };
        btnDone.Style.MarginBottom = 20;
        btnDone.OnClick += (e) =>
        {
            _options.SaveOptions();
            if (_parent != null)
            {
                Game.displayGuiScreen(new UIScreenAdapter(_parent));
            }
            else
            {
                Game.displayGuiScreen(null);
            }
        };
        Root.AddChild(btnDone);
    }

    private static UIElement CreateControlForOption(GameOption option)
    {
        TranslationStorage translations = TranslationStorage.Instance;

        if (option is FloatOption floatOpt)
        {
            Slider slider = new()
            {
                Value = floatOpt.Value,
                Text = option.GetDisplayString(translations)
            };
            slider.OnValueChanged += (v) =>
            {
                floatOpt.Value = v;
                slider.Text = option.GetDisplayString(translations);
            };
            return slider;
        }
        else
        {
            Button btn = new() { Text = option.GetDisplayString(translations) };
            btn.OnClick += (e) =>
            {
                if (option is BoolOption boolOpt) boolOpt.Toggle();
                else if (option is CycleOption cycleOpt) cycleOpt.Cycle();

                btn.Text = option.GetDisplayString(translations);
            };
            return btn;
        }
    }
}
