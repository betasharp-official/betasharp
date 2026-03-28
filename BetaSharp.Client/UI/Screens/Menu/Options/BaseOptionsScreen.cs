using BetaSharp.Client.Guis;
using BetaSharp.Client.Options;
using BetaSharp.Client.UI.Controls;
using BetaSharp.Client.UI.Controls.Core;
using BetaSharp.Client.UI.Layout.Flexbox;

namespace BetaSharp.Client.UI.Screens.Menu.Options;

public abstract class BaseOptionsScreen(UIScreen? parent, GameOptions options, string titleKey) : UIScreen(parent?.Game ?? BetaSharp.Instance)
{
    protected readonly UIScreen? Parent = parent;
    protected readonly GameOptions Options = options;
    protected string TitleText = TranslationStorage.Instance.TranslateKey(titleKey);

    protected override void Init()
    {
        Root.Style.AlignItems = Align.Center;
        Root.Style.JustifyContent = Justify.FlexStart;

        Root.AddChild(new Background(Game.world != null ? BackgroundType.World : BackgroundType.Dirt));

        Label title = new()
        {
            Text = TitleText,
            TextColor = Color.White,
            Centered = true
        };
        title.Style.MarginTop = 20;
        title.Style.MarginBottom = 12;
        Root.AddChild(title);

        ScrollView scroll = new();
        scroll.Style.Width = 340;
        scroll.Style.FlexGrow = 1;
        scroll.Style.MarginBottom = 10;

        UIElement content = CreateContent();
        scroll.AddContent(content);
        Root.AddChild(scroll);

        Button btnDone = new() { Text = TranslationStorage.Instance.TranslateKey("gui.done") };
        btnDone.Style.MarginBottom = 20;
        btnDone.OnClick += (e) => OnDone();
        Root.AddChild(btnDone);
    }

    protected virtual UIElement CreateContent()
    {
        Panel list = CreateVerticalList();

        foreach (GameOption option in GetOptions())
        {
            UIElement control = CreateControlForOption(option);
            control.Style.MarginTop = 2;
            control.Style.MarginBottom = 2;
            control.Style.Width = 310;
            list.AddChild(control);
        }

        return list;
    }

    protected static Panel CreateVerticalList()
    {
        Panel list = new();
        list.Style.FlexDirection = FlexDirection.Column;
        list.Style.AlignItems = Align.Center;
        list.Style.Width = 330;
        return list;
    }

    protected abstract IEnumerable<GameOption> GetOptions();

    protected virtual void OnDone()
    {
        Options.SaveOptions();
        if (Parent != null)
        {
            Game.displayGuiScreen(Parent);
        }
        else
        {
            Game.displayGuiScreen(null);
        }
    }

    protected virtual UIElement CreateControlForOption(GameOption option)
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
