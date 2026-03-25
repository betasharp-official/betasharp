using System.Reflection;
using BetaSharp.Client.Guis.Controls;
using BetaSharp.Client.Guis.Layout;
using java.awt;
using Button = BetaSharp.Client.Guis.Controls.Button;
using Label = BetaSharp.Client.Guis.Controls.Label;
using TextField = BetaSharp.Client.Guis.Controls.TextField;

namespace BetaSharp.Client.Guis;

public class DevToolsScreen : Screen
{
    private readonly StackPanel _treeView;
    private readonly Control _propertyPanel;
    private Control? _selectedControl;
    private Screen? _inspectedScreen;

    public override bool PausesGame => false;
    protected override bool CanExitWithEscape => false;

    public DevToolsScreen()
    {
        EffectiveSize = new(800, 600);

        Control container = new(0, 0, 50, 50) { Dock = Dock.Fill, Padding = new(5), };
        _treeView = new StackPanel(0, 0)
        {
            Dock = Dock.Fill,
        };
        _treeView.Rendered += (_, _) =>
        {
            Gui.DrawRect(0, 0, _treeView.EffectiveWidth, _treeView.EffectiveHeight, 0x9000FF00);
            Minecraft.INSTANCE.fontRenderer.DrawString(
                $"Width: {_treeView.EffectiveWidth} Height: {_treeView.EffectiveHeight} X: {_treeView.EffectiveX} Y: {_treeView.EffectiveY}",
                10, 350, 0xFFFFFFFF);
        };
        Scroller scroll = new(0, 0, 50, 50) { Dock = Dock.Fill };
        SplitPanel.SetSide(scroll, Side.Start);

        _propertyPanel = new Control(0, 0, 50, 50)
        {
            Dock = Dock.Fill,
            Background = 0xC0000000
        };
        _propertyPanel.Rendered += (_, _) =>
        {
            Gui.DrawRect(0, 0, _propertyPanel.EffectiveWidth, _propertyPanel.EffectiveHeight, 0x90FF0000);
        };
        SplitPanel.SetSide(_propertyPanel, Side.End);

        SplitPanel splitter = new(0, 0, EffectiveSize.Width, EffectiveSize.Height)
        {
            Margin = new(10),
            Dock = Dock.Fill,
            Orientation = Orientation.Horizontal,
            SplitterSize = 6,
        };

        splitter.AddChild(container);
        splitter.AddChild(_propertyPanel);
        container.AddChild(scroll);
        scroll.AddChild(_treeView);

        AddChildren(splitter);
    }

    public void RefreshHierarchy(Screen? gameScreen)
    {
        if (_inspectedScreen == gameScreen && gameScreen != null)
        {
            UpdatePropertyPanel();
            return;
        }

        _inspectedScreen = gameScreen;
        _treeView.ClearChildren();

        if (gameScreen == null)
        {
            Label noScreenLabel = new(10, 10, "No screen open", 0xFFFFFFFF);
            _treeView.AddChild(noScreenLabel);
            return;
        }

        // Build tree starting from the game screen
        int yOffset = 5;
        BuildTreeRecursive(gameScreen, 0, ref yOffset);
    }

    private void BuildTreeRecursive(Control control, int depth, ref int yOffset)
    {
        string indent = new(' ', depth * 4);
        string typeName = control.GetType().Name;
        string displayText = $"{indent}{typeName}";

        if (!string.IsNullOrEmpty(control.Text))
        {
            displayText += $" \"{control.Text}\"";
        }

        var treeItem = new Button(0, 0, _treeView.EffectiveWidth, displayText)
        {
            TextAlign = Alignment.Left,
            Anchor = Anchors.Left | Anchors.Right,
        };
        treeItem.Clicked += (_, _) => SelectControl(control);

        _treeView.AddChild(treeItem);
        yOffset += 22;

        foreach (Control child in control.ChildControls)
        {
            BuildTreeRecursive(child, depth + 1, ref yOffset);
        }
    }

    private void SelectControl(Control control)
    {
        _selectedControl = control;
        UpdatePropertyPanel();
    }

    public void UpdatePropertyPanel()
    {
        _propertyPanel.ClearChildren();

        if (_selectedControl == null)
        {
            _propertyPanel.AddChild(new Label(10, 10, "Select a control to inspect", 0xFFA0A0A0));
            return;
        }

        int y = 10;
        var c = _selectedControl;

        Type? type = c.GetType();
        string typeString = "";
        while (type is not null && type != typeof(object))
        {
            typeString += $"{type.Name} : ";
            type = type.BaseType;
        }
        typeString = typeString[..^3]; // Remove trailing " : "
        if (typeString.EndsWith(" : Control"))
        {
            typeString = typeString[..^10]; // No need to show "Control" at the end of the hierarchy
        }

        type = c.GetType();
        var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly;
        while (type != null && type != typeof(object))
        {
            _propertyPanel.AddChild(new Label(10, y, $"Type: {typeString}", 0xFFFFFFFF));
            foreach (FieldInfo field in type.GetFields(flags))
            {
                y += 16;
                object? value = field.GetValue(c);
                string valueString = value switch
                {
                    null => "null",
                    string s => $"\"{s}\"",
                    _ => value.ToString() ?? "null",
                };
                Label nameLabel = new(10, y + 3, $"{field.Name}: ", 0xFFFFFFFF);
                _propertyPanel.AddChild(nameLabel);
                TextField valueField = new(nameLabel.EffectiveX + nameLabel.EffectiveWidth, y, FontRenderer, valueString)
                {
                    Anchor = Anchors.Left | Anchors.Right,
                    EffectiveHeight = 14,
                };
                _propertyPanel.AddChild(valueField);
            }
            y += 30;
            type = type.BaseType;
        }
    }

    protected override void OnRender(RenderEventArgs e)
    {
    }
}
