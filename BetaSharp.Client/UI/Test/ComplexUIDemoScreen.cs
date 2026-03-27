using BetaSharp.Client.Guis;
using BetaSharp.Client.UI.Controls.Core;
using BetaSharp.Client.UI.Layout.Flexbox;

namespace BetaSharp.Client.UI.Test;

public class ComplexUIDemoScreen : UIScreen
{
    public ComplexUIDemoScreen(BetaSharp game) : base(game) { }

    protected override void Init()
    {
        Root.Style.FlexDirection = FlexDirection.Column;
        Root.Style.AlignItems = Align.Center; // Align all primary sections (Header, Flow, Footer) down the center horizontally

        // --- 1. Header Array ---
        Panel header = new();
        header.Style.BackgroundColor = Color.BackgroundBlackAlpha;
        header.Style.Height = 50;
        header.Style.Width = 350;
        header.Style.MarginTop = 15;
        header.Style.FlexDirection = FlexDirection.Row;
        header.Style.JustifyContent = Justify.Center;
        header.Style.AlignItems = Align.Center;

        Label title = new()
        {
            Text = "Play Multiplayer"
        };
        header.AddChild(title);

        Root.AddChild(header);


        // --- 2. Dynamic Adaptive Server Viewport (FLEX GROW!) ---
        ScrollView scrollView = new();

        // This causes it to completely fill all vertical free-space between the Header and the Action Footer dynamically!
        scrollView.Style.FlexGrow = 1.0f;

        scrollView.Style.MarginTop = 10;
        scrollView.Style.MarginBottom = 10;
        scrollView.Style.Width = 350;
        scrollView.ContentContainer.Style.AlignItems = Align.Center;

        for (int i = 0; i < 20; i++)
        {
            Button btn = new();
            btn.Text = "Minecraft Server " + (i + 1);
            btn.Style.Width = 300; // Leave room for 10px scrollbar and standard margins
            btn.Style.MarginBottom = 4;
            scrollView.AddContent(btn);
        }

        Root.AddChild(scrollView);


        // --- 3. Footer Actions Toolbar ---
        Panel footer = new();
        footer.Style.BackgroundColor = Color.BackgroundBlackAlpha;
        footer.Style.Height = 50;
        footer.Style.Width = 350;
        footer.Style.MarginBottom = 15;
        footer.Style.FlexDirection = FlexDirection.Row;

        // Push Join and Cancel buttons apart using FlexJustify.SpaceBetween!
        footer.Style.JustifyContent = Justify.SpaceBetween;
        footer.Style.AlignItems = Align.FlexStart;

        Button btnJoin = new();
        btnJoin.Text = "Join Server";
        btnJoin.Style.Width = 160;

        Button btnCancel = new();
        btnCancel.Text = "Cancel";
        btnCancel.Style.Width = 160;

        footer.AddChild(btnJoin);
        footer.AddChild(btnCancel);

        Root.AddChild(footer);
    }
}
