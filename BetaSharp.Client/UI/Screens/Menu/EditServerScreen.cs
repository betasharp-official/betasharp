using BetaSharp.Client.Guis;
using BetaSharp.Client.Network;
using BetaSharp.Client.UI.Controls;
using BetaSharp.Client.UI.Controls.Core;
using BetaSharp.Client.UI.Layout.Flexbox;

namespace BetaSharp.Client.UI.Screens.Menu;

public class EditServerScreen(
    UIContext context,
    MultiplayerScreen parent,
    ServerData serverData,
    bool isEditing) : UIScreen(context)
{
    private TextField _txfName = null!;
    private TextField _txfAddress = null!;

    protected override void Init()
    {
        Root.AddChild(new Background());
        Root.Style.AlignItems = Align.Center;
        Root.Style.JustifyContent = Justify.Center;
        Root.Style.SetPadding(20);

        Label title = new() { Text = isEditing ? "Edit Server Info" : "Add Server Info", TextColor = Color.White };
        title.Style.MarginBottom = 10;
        Root.AddChild(title);

        Label lName = new() { Text = "Server Name", TextColor = Color.GrayA0 };
        lName.Style.MarginBottom = 4;
        Root.AddChild(lName);

        _txfName = new TextField();
        _txfName.Style.Width = 200;
        _txfName.Style.MarginBottom = 10;
        _txfName.Text = serverData.Name;
        Root.AddChild(_txfName);

        Label lAddr = new() { Text = "Server Address", TextColor = Color.GrayA0 };
        lAddr.Style.MarginBottom = 4;
        Root.AddChild(lAddr);

        _txfAddress = new TextField();
        _txfAddress.Style.Width = 200;
        _txfAddress.Style.MarginBottom = 20;
        _txfAddress.Text = serverData.Ip;
        Root.AddChild(_txfAddress);

        Panel buttonPanel = new();
        buttonPanel.Style.FlexDirection = FlexDirection.Row;

        Button btnDone = CreateButton();
        btnDone.Text = "Done";
        btnDone.Style.Width = 100;
        btnDone.Style.SetMargin(0, 4, 0, 0);
        btnDone.OnClick += (e) =>
        {
            serverData.Name = _txfName.Text;
            serverData.Ip = _txfAddress.Text;
            parent.ConfirmEdit(serverData, isEditing);
            Context.Navigator.Navigate(parent);
        };
        buttonPanel.AddChild(btnDone);

        Button btnCancel = CreateButton();
        btnCancel.Text = "Cancel";
        btnCancel.Style.Width = 100;
        btnCancel.OnClick += (e) => Context.Navigator.Navigate(parent);
        buttonPanel.AddChild(btnCancel);

        Root.AddChild(buttonPanel);
    }
}
