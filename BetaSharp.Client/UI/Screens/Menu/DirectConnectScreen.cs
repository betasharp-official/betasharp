using BetaSharp.Client.Guis;
using BetaSharp.Client.Network;
using BetaSharp.Client.UI.Controls;
using BetaSharp.Client.UI.Controls.Core;
using BetaSharp.Client.UI.Layout.Flexbox;
using BetaSharp.Client.UI.Screens.Menu.Net;

namespace BetaSharp.Client.UI.Screens.Menu;

public class DirectConnectScreen(
    UIContext context,
    UIScreen parent,
    ServerData serverData,
    ClientNetworkContext networkContext) : UIScreen(context)
{
    private TextField _txfAddress = null!;

    protected override void Init()
    {
        Root.AddChild(new Background());
        Root.Style.AlignItems = Align.Center;
        Root.Style.JustifyContent = Justify.Center;
        Root.Style.SetPadding(20);

        Label title = new() { Text = "Direct Connect", TextColor = Color.White };
        title.Style.MarginBottom = 10;
        Root.AddChild(title);

        Label label = new() { Text = "Server Address", TextColor = Color.GrayA0 };
        label.Style.MarginBottom = 4;
        Root.AddChild(label);

        _txfAddress = new TextField();
        _txfAddress.Style.Width = 200;
        _txfAddress.Style.MarginBottom = 20;
        _txfAddress.Text = Context.Options.LastServer;
        Root.AddChild(_txfAddress);

        Panel buttonPanel = new();
        buttonPanel.Style.FlexDirection = FlexDirection.Row;

        Button btnJoin = CreateButton();
        btnJoin.Text = "Join Server";
        btnJoin.Style.Width = 100;
        btnJoin.Style.SetMargin(0, 4, 0, 0);
        btnJoin.OnClick += (e) =>
        {
            serverData.Ip = _txfAddress.Text;
            Context.Options.LastServer = _txfAddress.Text;
            Context.Options.SaveOptions();
            ConnectToServer(_txfAddress.Text);
        };
        buttonPanel.AddChild(btnJoin);

        Button btnCancel = CreateButton();
        btnCancel.Text = "Cancel";
        btnCancel.Style.Width = 100;
        btnCancel.OnClick += (e) => Context.Navigator.Navigate(parent);
        buttonPanel.AddChild(btnCancel);

        Root.AddChild(buttonPanel);
    }

    private void ConnectToServer(string ip)
    {
        string[] parts = ip.Split(':');
        string host = parts[0];
        int portNum = 25565;
        if (parts.Length > 1) int.TryParse(parts[1], out portNum);
        Context.Navigator.Navigate(new ConnectingScreen(Context, networkContext, host, portNum));
    }
}
