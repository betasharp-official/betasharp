using BetaSharp.Client.Input;
using BetaSharp.Client.Options;
using BetaSharp.Client.UI.Controls.Core;
using BetaSharp.Client.UI.Layout.Flexbox;
using Silk.NET.GLFW;

namespace BetaSharp.Client.UI.Screens.Menu.Options;

public class ControllerBindingsScreen(UIContext context, UIScreen? parent)
    : BaseOptionsScreen(context, parent, "Button Bindings")
{
    private int _listeningIndex = -1;
    private readonly ControllerListener _listener = new();

    protected override List<OptionSection> GetOptions() => [];

    protected override void Init()
    {
        TitleText = "Button Bindings";
        base.Init();
    }

    protected override UIElement CreateContent()
    {
        Panel list = CreateTwoColumnList();

        for (int i = 0; i < Options.ControllerBindings.Length; i++)
        {
            int index = i;
            ControllerBinding bind = Options.ControllerBindings[i];

            Panel row = new();
            row.Style.FlexDirection = FlexDirection.Row;
            row.Style.AlignItems = Align.Center;
            row.Style.Width = TwoButtonSize;
            row.Style.SetMargin(2);

            Label label = new() { Text = bind.Description };
            label.Style.FlexGrow = 1;
            row.AddChild(label);

            string btnText = _listeningIndex == i ? "> ??? <" : bind.GetButtonName();
            Button btn = CreateButton();
            btn.Text = btnText;
            btn.Style.Width = 80;
            btn.OnClick += (e) =>
            {
                _listeningIndex = index;
                _listener.StartListening(OnButtonPressed);
                Refresh();
            };
            row.AddChild(btn);

            list.AddChild(row);
        }

        return list;
    }

    private void OnButtonPressed(GamepadButton button)
    {
        if (button != GamepadButton.Back && _listeningIndex >= 0)
        {
            Options.ControllerBindings[_listeningIndex].Button = button;
            Options.SaveOptions();
        }

        _listeningIndex = -1;
        Controller.ClearEvents();
        Refresh();
    }

    private void Refresh()
    {
        Root.Children.Clear();
        Init();
    }

    public override void Update(float partialTicks)
    {
        base.Update(partialTicks);
        _listener.Update();
    }

    public override void KeyTyped(int key, char character)
    {
        if (_listener.IsListening)
        {
            if (key == Keyboard.KEY_ESCAPE)
            {
                _listener.StopListening();
                _listeningIndex = -1;
                Refresh();
            }
            return;
        }
        base.KeyTyped(key, character);
    }
}
