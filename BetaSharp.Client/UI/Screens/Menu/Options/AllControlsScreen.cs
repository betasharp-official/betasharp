using BetaSharp.Client.Options;
using BetaSharp.Client.UI.Controls.Core;

namespace BetaSharp.Client.UI.Screens.Menu.Options;

public class AllControlsScreen : BaseOptionsScreen
{
    public AllControlsScreen(UIContext context, UIScreen? parent)
        : base(context, parent, "options.controls")
    {
        TitleText = "Controls";
    }

    protected override List<OptionSection> GetOptions() => [];

    protected override UIElement CreateContent()
    {
        Panel list = CreateTwoColumnList();

        Button btnKeyboard = CreateButton();
        btnKeyboard.Text = "Keyboard Controls...";
        btnKeyboard.Style.Width = TwoButtonSize;
        btnKeyboard.Style.MarginBottom = 4;
        btnKeyboard.OnClick += (e) =>
        {
            Context.Navigator.Navigate(new ControlsScreen(Context, this));
        };
        list.AddChild(btnKeyboard);

        Button btnController = CreateButton();
        btnController.Text = "Controller Settings...";
        btnController.Style.Width = TwoButtonSize;
        btnController.OnClick += (e) =>
        {
            Context.Navigator.Navigate(new ControllerControlsScreen(Context, this));
        };
        list.AddChild(btnController);

        return list;
    }
}
