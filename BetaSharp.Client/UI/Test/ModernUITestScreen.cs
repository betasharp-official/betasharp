using BetaSharp.Client.UI.Controls;
using BetaSharp.Client.UI.Layout;
using BetaSharp.Client.UI.Layout.Flexbox;

namespace BetaSharp.Client.UI.Test;

public class ModernUITestScreen : UIScreen
{
    public ModernUITestScreen(BetaSharp game) : base(game)
    {
    }

    protected override void Init()
    {

        // Root is automatically a Column layout by default
        Root.Style.AlignItems = Align.Center;
        Root.Style.JustifyContent = Justify.Center;

        Panel mainPanel = new();
        mainPanel.Style.Width = 300;
        mainPanel.Style.Height = 200;
        mainPanel.Style.FlexDirection = FlexDirection.Column;
        mainPanel.Style.AlignItems = Align.Center;
        mainPanel.Style.PaddingTop = 20;

        Label titleLabel = new()
        {
            Text = "Welcome to Modern UI"
        };
        titleLabel.Style.Height = 20;
        titleLabel.Style.MarginBottom = 30;
        titleLabel.Centered = true;

        Button startButton = new()
        {
            Text = "Start Game"
        };
        startButton.Style.MarginBottom = 10;
        startButton.OnClick += (e) =>
        {
            e.Handled = true;
            Game.displayGuiScreen(null);
            Game.setIngameFocus();
        };

        Button quitButton = new()
        {
            Text = "Quit"
        };
        quitButton.OnClick += (e) =>
        {
            e.Handled = true;
            Game.shutdown();
        };

        ScrollView scrollView = new();
        scrollView.Style.Width = 250;
        scrollView.Style.Height = 120; // Needs to be smaller than the 20 items combined (20 * 22 = 440)
        scrollView.Style.MarginBottom = 10;
        scrollView.ContentContainer.Style.AlignItems = Align.Center;
        
        for (int i = 0; i < 20; i++)
        {
            Button btn = new()
            {
                Text = "Scrollable Item " + (i + 1)
            };
            btn.Style.MarginBottom = 2; 
            
            int itemIndex = i + 1;
            btn.OnClick += (e) =>
            {
                System.Console.WriteLine($"[ScrollView] Button {itemIndex} was clicked.");
            };
            
            scrollView.AddContent(btn);
        }

        mainPanel.AddChild(titleLabel);
        mainPanel.AddChild(scrollView);
        mainPanel.AddChild(startButton);
        mainPanel.AddChild(quitButton);

        Root.AddChild(mainPanel);
    }

    public override void Uninit()
    {
    }
}
