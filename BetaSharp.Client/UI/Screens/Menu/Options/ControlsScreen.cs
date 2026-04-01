using BetaSharp.Client.Options;
using BetaSharp.Client.UI.Controls.Core;
using BetaSharp.Client.UI.Layout.Flexbox;

namespace BetaSharp.Client.UI.Screens.Menu.Options;

public class ControlsScreen : BaseOptionsScreen
{
    private int _selectedKey = -1;

    public ControlsScreen(UIContext context, UIScreen? parent)
        : base(context, parent, "controls.title")
    {
        TitleText = "Controls";
    }

    protected override IEnumerable<GameOption> GetOptions() => [];

    protected override UIElement CreateContent()
    {
        Panel list = CreateVerticalList();

        // Mouse Settings at top
        UIElement sensitivity = CreateControlForOption(Options.MouseSensitivityOption);
        sensitivity.Style.Width = 310;
        sensitivity.Style.MarginBottom = 4;
        list.AddChild(sensitivity);

        UIElement invert = CreateControlForOption(Options.InvertMouseOption);
        invert.Style.Width = 310;
        invert.Style.MarginBottom = 10;
        list.AddChild(invert);

        // Keybinds List
        for (int i = 0; i < Options.KeyBindings.Length; i++)
        {
            int index = i;
            Panel row = new();
            row.Style.FlexDirection = FlexDirection.Row;
            row.Style.AlignItems = Align.Center;
            row.Style.Width = 310;
            row.Style.SetMargin(2);

            Label label = new() { Text = Options.GetKeyBindingDescription(i) };
            label.Style.FlexGrow = 1;
            row.AddChild(label);

            string btnText = _selectedKey == index ? "> ??? <" : Options.GetOptionDisplayString(index);
            Button btn = CreateButton();
            btn.Text = btnText;
            btn.Style.Width = 80;
            btn.OnClick += (e) =>
            {
                _selectedKey = index;
                Refresh();
            };
            row.AddChild(btn);

            list.AddChild(row);
        }

        return list;
    }

    private void Refresh()
    {
        Root.Children.Clear();
        Init();
    }

    public override void KeyTyped(int key, char character)
    {
        if (_selectedKey >= 0)
        {
            Options.SetKeyBinding(_selectedKey, key);
            _selectedKey = -1;
            Refresh();
        }
        else
        {
            base.KeyTyped(key, character);
        }
    }
}
