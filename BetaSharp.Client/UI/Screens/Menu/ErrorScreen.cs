using BetaSharp.Client.Guis;
using BetaSharp.Client.UI.Controls;
using BetaSharp.Client.UI.Controls.Core;
using BetaSharp.Client.UI.Layout.Flexbox;

namespace BetaSharp.Client.UI.Screens.Menu;

public class ErrorScreen(
    UIContext context,
    string title,
    params string[] messages) : UIScreen(context)
{
    private readonly List<string> _messages = [.. messages];

    protected override void Init()
    {
        Root.AddChild(new Background());
        Root.Style.AlignItems = Align.Center;
        Root.Style.JustifyContent = Justify.Center;
        Root.Style.SetPadding(20);

        Label lblTitle = new()
        {
            Text = title,
            TextColor = Color.White,
            Centered = true
        };
        lblTitle.Style.MarginBottom = 20;
        Root.AddChild(lblTitle);

        Panel messageContainer = new();
        messageContainer.Style.AlignItems = Align.Center;
        messageContainer.Style.MarginBottom = 20;

        foreach (string msg in _messages)
        {
            Label lblMsg = new()
            {
                Text = msg,
                TextColor = Color.GrayA0,
                Centered = true
            };
            lblMsg.Style.MarginBottom = 2;
            messageContainer.AddChild(lblMsg);
        }
        Root.AddChild(messageContainer);

        Button btnRestart = CreateButton();
        btnRestart.Text = "Please restart the Game.";
        btnRestart.Enabled = false;

        Root.AddChild(btnRestart);
    }
}
