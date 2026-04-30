using BetaSharp.Client.Options;
using BetaSharp.Client.UI.Controls.Core;

namespace BetaSharp.Client.UI.Screens.Menu.Options;

public class AllControlsScreen : BaseOptionsScreen
{
    public AllControlsScreen(UIContext context, UIScreen? parent)
        : base(context, parent, "options.controls")
    {
        TitleText = TranslationStorage.Instance.TranslateKey("options.controls");
    }

    protected override List<OptionSection> GetOptions() => [];

    protected override UIElement CreateContent()
    {
        TranslationStorage translationStorage = TranslationStorage.Instance;

        Panel list = CreateTwoColumnList();

        Button btnKeyboard = CreateButton();
        btnKeyboard.Text = translationStorage.TranslateKey("options.keyboardControls");
        btnKeyboard.Style.Width = TwoButtonSize;
        btnKeyboard.Style.MarginBottom = 4;
        btnKeyboard.OnClick += (e) =>
        {
            Context.Navigator.Navigate(new ControlsScreen(Context, this));
        };
        list.AddChild(btnKeyboard);

        Button btnController = CreateButton();
        btnController.Text = translationStorage.TranslateKey("options.controllerSettings");
        btnController.Style.Width = TwoButtonSize;
        btnController.OnClick += (e) =>
        {
            Context.Navigator.Navigate(new ControllerControlsScreen(Context, this));
        };
        list.AddChild(btnController);

        return list;
    }
}
