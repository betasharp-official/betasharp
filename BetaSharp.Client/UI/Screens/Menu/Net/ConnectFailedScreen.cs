using BetaSharp.Client.Guis;
using BetaSharp.Client.UI.Controls;
using BetaSharp.Client.UI.Controls.Core;
using BetaSharp.Client.UI.Layout.Flexbox;

namespace BetaSharp.Client.UI.Screens.Menu.Net;

public class ConnectFailedScreen : UIScreen
{
    private readonly string _errorMessage;
    private readonly string _errorDetail;

    public ConnectFailedScreen(
        UIContext context,
        string messageKey,
        string detailKey,
        params object[]? formatArgs) : base(context)
    {

        TranslationStorage translations = TranslationStorage.Instance;
        _errorMessage = translations.TranslateKey(messageKey);

        if (formatArgs != null && formatArgs.Length > 0)
        {
            _errorDetail = translations.TranslateKeyFormat(detailKey, formatArgs);
        }
        else
        {
            _errorDetail = translations.TranslateKey(detailKey);
        }
    }

    protected override void Init()
    {
        Root.AddChild(new Background());
        Root.Style.AlignItems = Align.Center;
        Root.Style.JustifyContent = Justify.Center;
        Root.Style.SetPadding(20);

        Label lblError = new()
        {
            Text = _errorMessage,
            TextColor = Color.White,
            Centered = true
        };
        lblError.Style.MarginBottom = 10;
        Root.AddChild(lblError);

        Label lblDetail = new()
        {
            Text = _errorDetail,
            TextColor = Color.GrayA0,
            Centered = true
        };
        lblDetail.Style.MarginBottom = 20;
        Root.AddChild(lblDetail);

        Button btnToMenu = CreateButton();
        btnToMenu.Text = TranslationStorage.Instance.TranslateKey("gui.toMenu");
        btnToMenu.Style.Width = 150;
        btnToMenu.OnClick += (e) => Context.Navigator.Navigate(null);
        Root.AddChild(btnToMenu);
    }
}
