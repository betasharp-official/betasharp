using BetaSharp.Client.Guis;
using BetaSharp.Client.Network;
using BetaSharp.Client.Threading;
using BetaSharp.Client.UI.Controls;
using BetaSharp.Client.UI.Controls.Core;
using BetaSharp.Client.UI.Layout.Flexbox;

namespace BetaSharp.Client.UI.Screens.Menu.Net;

public class ConnectingScreen : UIScreen
{
    public ClientNetworkHandler? ClientHandler { get; set; }
    public bool IsCancelled { get; private set; }
    public override bool PausesGame => false;

    public ConnectingScreen(
        UIContext context,
        ClientNetworkContext networkContext,
        string host,
        int port) : base(context)
    {
        networkContext.WorldHost.ChangeWorld(null);
        new ThreadConnectToServer(this, networkContext, host, port).Start();
    }

    public ConnectingScreen(UIContext context, ClientNetworkHandler clientHandler) : base(context)
    {
        ClientHandler = clientHandler;
    }

    protected override void Init()
    {
        Root.AddChild(new Background());
        Root.Style.AlignItems = Align.Center;
        Root.Style.JustifyContent = Justify.Center;
        Root.Style.SetPadding(20);

        Label lblStatus = new()
        {
            Text = GetStatusText(),
            TextColor = Color.White,
            Centered = true
        };
        lblStatus.Style.MarginBottom = 20;
        Root.AddChild(lblStatus);

        if (ClientHandler != null)
        {
            Label lblDetail = new()
            {
                Text = ClientHandler.StatusMessage,
                TextColor = Color.GrayA0,
                Centered = true
            };
            lblDetail.Style.MarginBottom = 10;
            Root.AddChild(lblDetail);
        }

        Button btnCancel = CreateButton();
        btnCancel.Text = TranslationStorage.Instance.TranslateKey("gui.cancel");
        btnCancel.OnClick += (e) => Cancel();
        Root.AddChild(btnCancel);
    }

    private string GetStatusText()
    {
        TranslationStorage translations = TranslationStorage.Instance;
        return ClientHandler == null
            ? translations.TranslateKey("connect.connecting")
            : translations.TranslateKey("connect.authorizing");
    }

    public void Cancel()
    {
        IsCancelled = true;
        ClientHandler?.Disconnect();
        Context.Navigator.Navigate(null);
    }

    public override void Update(float partialTicks)
    {
        base.Update(partialTicks);
        ClientHandler?.Tick();

        if (Root.Children.Count >= 2 && Root.Children[1] is Label lblStatus)
        {
            string newText = GetStatusText();
            if (lblStatus.Text != newText)
            {
                lblStatus.Text = newText;
            }
        }
    }
}
