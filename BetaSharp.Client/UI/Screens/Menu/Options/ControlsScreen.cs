using BetaSharp.Client.Input;
using BetaSharp.Client.Options;
using BetaSharp.Client.UI.Controls.Core;
using BetaSharp.Client.UI.Layout.Flexbox;

namespace BetaSharp.Client.UI.Screens.Menu.Options;

public class ControlsScreen : BaseOptionsScreen
{
    private KeyBinding? _selectedKey = null;
    protected override int MaxWidth { get; } = 300;

    public ControlsScreen(UIContext context, UIScreen? parent)
        : base(context, parent, "controls.title")
    {
        TitleText = "Controls";
    }

    protected override List<OptionSection> GetOptions() => [];

    protected override UIElement CreateContent()
    {
        Panel list = CreateTwoColumnList();

        // Mouse Settings at top
        UIElement sensitivity = CreateControlForOption(Options.MouseSensitivityOption);
        sensitivity.Style.Width = ButtonSize;
        sensitivity.Style.MarginLeft = ButtonPadding;
        sensitivity.Style.MarginRight = ButtonPadding;
        sensitivity.Style.MarginBottom = 10;
        list.AddChild(sensitivity);

        UIElement invert = CreateControlForOption(Options.InvertMouseOption);
        invert.Style.Width = ButtonSize;
        invert.Style.MarginLeft = ButtonPadding;
        invert.Style.MarginRight = ButtonPadding;
        invert.Style.MarginBottom = 10;
        list.AddChild(invert);

        // Keybinds List
        bool first = true;
        foreach (GameOptions.KeyBindingGroup group in Options.KeyBindingGroups)
        {
            list.AddChild(CreateSectionHeader(group.Title, first));

            for (int i = 0; i < group.Bindings.Length; i++)
            {
                KeyBinding bind = group.Bindings[i];

                int index = i;
                Panel row = new();
                row.Style.FlexDirection = FlexDirection.Row;
                row.Style.AlignItems = Align.Center;
                row.Style.Width = TwoButtonSize;
                row.Style.SetMargin(2);

                Label label = new() { Text = Options.GetKeyBindingDescription(bind) };
                label.Style.FlexGrow = 1;
                row.AddChild(label);

                string btnText = ReferenceEquals(_selectedKey, bind) ? "> ??? <" : Options.GetOptionDisplayString(bind);
                Button btn = CreateButton();
                btn.Text = btnText;
                btn.Style.Width = 80;
                btn.OnClick += (e) =>
                {
                    _selectedKey = bind;
                    Refresh();
                };
                row.AddChild(btn);

                list.AddChild(row);
            }

            first = false;
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
        if (_selectedKey is not null)
        {
            Options.SetKeyBinding(_selectedKey, key);
            _selectedKey = null;
            Refresh();
        }
        else
        {
            base.KeyTyped(key, character);
        }
    }
}
