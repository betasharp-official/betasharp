using System.ComponentModel;
using System.Reflection;
using BetaSharp.Client.Debug;
using BetaSharp.Client.Guis;
using BetaSharp.Client.UI.Controls;
using BetaSharp.Client.UI.Controls.Core;
using BetaSharp.Client.UI.Controls.ListItems;
using BetaSharp.Client.UI.Layout.Flexbox;
using BetaSharp.Items;
using BetaSharp.NBT;

namespace BetaSharp.Client.UI.Screens.InGame;

public class EditDebugComponentScreen(BetaSharp game, DebugEditorScreen parent, DebugComponent oldComp) : UIScreen(game)
{
    private DebugComponent comp = oldComp.Duplicate();
    private Type? _compType = oldComp.GetType();
    private ScrollView _scroll = null!;
    private Button _okButton = null!;
    private Button _cancelButton = null!;

    protected override void Init()
    {
        Root.Style.SetPadding(20);
        Root.Style.AlignItems = Align.Center;

        Root.AddChild(new Background(Game.world != null ? BackgroundType.World : BackgroundType.Dirt));

        var title = new Label
        {
            Text = "Edit Debug Component",
            Scale = 1.5f,
            TextColor = Color.White
        };
        title.Style.MarginBottom = 10;

        var subtitle = new Label
        {
            Text = DebugComponents.GetName(comp.GetType()),
            Scale = 1f,
            TextColor = Color.White
        };
        subtitle.Style.MarginBottom = 10;

        Root.AddChild(title);
        Root.AddChild(subtitle);

        _scroll = new ScrollView();
        _scroll.Style.Width = 300;
        _scroll.Style.FlexGrow = 1;
        _scroll.Style.MarginBottom = 10;
        Root.AddChild(_scroll);

        AddOptionRow(_compType.GetProperty("Right")!); // make sure Right is always at the top
        foreach (PropertyInfo prop in _compType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (prop.CanRead && prop.CanWrite && prop.Name != "Right")
            {
                AddOptionRow(prop);
            }
        }

        var buttonContainer = new UIElement();
        buttonContainer.Style.FlexDirection = FlexDirection.Row;
        buttonContainer.Style.JustifyContent = Justify.Center;
        buttonContainer.Style.Width = 320;
        Root.AddChild(buttonContainer);

        var okButton = new Button
        {
            Text = "OK"
        };
        okButton.Style.Width = 100;
        okButton.Style.SetMargin(2);
        okButton.OnClick += (_) =>
        {
            foreach (UIElement row in _scroll.ContentContainer.Children)
            {
                if (row.Tag is PropertyInfo prop)
                {
                    object? value = prop.GetValue(comp);
                    prop.SetValue(oldComp, value);
                }
            }

            Game.displayGuiScreen(parent);
        };
        buttonContainer.AddChild(okButton);

        var cancelButton = new Button
        {
            Text = "Cancel"
        };
        cancelButton.Style.Width = 100;
        cancelButton.Style.SetMargin(2);
        cancelButton.OnClick += (_) => Game.displayGuiScreen(parent);
        buttonContainer.AddChild(cancelButton);
    }

    private void AddOptionRow(PropertyInfo prop)
    {
        Type typ = prop.PropertyType;

        UIElement row = new UIElement();
        row.Tag = prop; // store property info for later use
        row.Style.Width = 300;
        row.Style.JustifyContent = Justify.SpaceBetween;
        row.Style.FlexDirection = FlexDirection.Row;
        row.Style.AlignItems = Align.Center;
        row.Style.MarginBottom = 4;

        DisplayNameAttribute? displayNameAttribute = prop.GetCustomAttribute<DisplayNameAttribute>();
        string name = displayNameAttribute != null ? displayNameAttribute.DisplayName : prop.Name;

        row.AddChild(new Label
        {
            Text = name,
            TextColor = Color.White
        });

        if (typ == typeof(bool))
        {
            Color trueColor = Color.FromRgb(0x00BB00);
            Color falseColor = Color.FromRgb(0xBB0000);

            Button btn = new Button
            {
                Text = prop.GetValue(comp)?.ToString() ?? "False",
                TextColor = (bool)(prop.GetValue(comp) ?? false) ? trueColor : falseColor
            };
            btn.Style.Width = 60;
            btn.OnClick += (_) =>
            {
                Console.WriteLine($"Toggling {prop.Name}");
                bool current = (bool)(prop.GetValue(comp) ?? false);
                Console.WriteLine($"Current value: {current}");
                prop.SetValue(comp, !current);
                btn.Text = (!current).ToString();
                btn.TextColor = !current ? trueColor : falseColor;
                bool news = (bool)(prop.GetValue(comp) ?? false);
                Console.WriteLine($"New value: {news}");
            };

            row.AddChild(btn);
            _scroll.AddContent(row);
        }

        // add extra seperator if the property is Right
        if (prop.Name == "Right")
        {
            // add separator
            var separator = new UIElement();
            separator.Style.Width = 294;
            separator.Style.Height = 1;
            separator.Style.BackgroundColor = Color.GrayAA;
            separator.Style.MarginTop = 4;
            separator.Style.MarginBottom = 8;
            _scroll.AddContent(separator);
        }
    }

    public override void Render(int mouseX, int mouseY, float partialTicks)
    {
        base.Render(mouseX, mouseY, partialTicks);

        // Tooltip rendering
        if (Root.HitTest(MouseX, MouseY) is UIElement hovered)
        {
            PropertyInfo prop;
            if (hovered.Tag is PropertyInfo p)
            {
                prop = p;
            }
            else if (hovered is Label && hovered.Parent?.Tag is PropertyInfo p2)
            {
                prop = p2;
            }
            else
            {
                return; // no property info found, no tooltip to show
            }

            DescriptionAttribute? descriptionAttribute = prop.GetCustomAttribute<DescriptionAttribute>();
            string desc = descriptionAttribute != null ? descriptionAttribute.Description : prop.Name;
            if (desc.Length > 0)
            {
                const int maxWidth = 150;
                int textWidth = Math.Min(Game.fontRenderer.GetStringWidth(desc), maxWidth);
                int textHeight = Game.fontRenderer.GetStringHeight(desc, maxWidth);
                float tx = MouseX + 12;
                float ty = MouseY - 12;

                Renderer.Begin();
                Renderer.DrawGradientRect(tx - 3, ty - 3, textWidth + 6, textHeight + 6, Color.BlackAlphaC0, Color.BlackAlphaC0);
                Renderer.DrawTextWrapped(desc, tx, ty, maxWidth, Color.White);
                Renderer.End();
            }
        }
    }
}
