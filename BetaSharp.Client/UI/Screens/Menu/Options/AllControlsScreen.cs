using BetaSharp.Client.Options;
using BetaSharp.Client.UI.Controls.Core;

namespace BetaSharp.Client.UI.Screens.Menu.Options;

public class AllControlsScreen : BaseOptionsScreen
{
    public AllControlsScreen(UIScreen? parent, GameOptions options)
        : base(parent, options, "options.controls")
    {
        TitleText = "Controls";
    }

    protected override IEnumerable<GameOption> GetOptions() => [];

    protected override UIElement CreateContent()
    {
        Panel list = CreateVerticalList();

        Button btnKeyboard = new() { Text = "Keyboard Controls..." };
        btnKeyboard.Style.Width = 310;
        btnKeyboard.Style.MarginBottom = 4;
        btnKeyboard.OnClick += (e) =>
        {
            Game.displayGuiScreen(new ControlsScreen(this, Options));
        };
        list.AddChild(btnKeyboard);

        Button btnController = new() { Text = "Controller Settings..." };
        btnController.Style.Width = 310;
        btnController.OnClick += (e) =>
        {
            Game.displayGuiScreen(new ControllerControlsScreen(this, Options));
        };
        list.AddChild(btnController);

        return list;
    }
}
