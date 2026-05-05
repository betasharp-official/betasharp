using BetaSharp.Client.Input;
using BetaSharp.Client.Options;
using BetaSharp.Client.UI.Controls.Core;

namespace BetaSharp.Client.UI.Screens.Menu.Options;

public class ControllerControlsScreen : BaseOptionsScreen
{
    public ControllerControlsScreen(UIContext context, UIScreen? parent)
        : base(context, parent, "Controller Settings")
    {
        TitleText = "Controller Settings";
    }

    protected override List<OptionSection> GetOptions() => [];

    protected override UIElement CreateContent()
    {
        Panel list = CreateTwoColumnList();

        // Sensitivity
        UIElement sens = CreateControlForOption(Options.ControllerSensitivityOption);
        sens.Style.Width = ButtonSize;
        sens.Style.MarginLeft = ButtonPadding;
        sens.Style.MarginRight = ButtonPadding;
        sens.Style.MarginBottom = 4;
        list.AddChild(sens);

        // Controller Type
        UIElement type = CreateControlForOption(Options.ControllerTypeOption);
        type.Style.Width = ButtonSize;
        type.Style.MarginLeft = ButtonPadding;
        type.Style.MarginRight = ButtonPadding;
        type.Style.MarginBottom = 4;
        list.AddChild(type);

        // Edit Bindings Button
        Button btnBindings = CreateButton();
        btnBindings.Text = "Edit Bindings...";
        btnBindings.Style.Width = ButtonSize;
        btnBindings.Style.MarginLeft = ButtonPadding;
        btnBindings.Style.MarginRight = ButtonPadding;
        btnBindings.Style.MarginBottom = 4;
        btnBindings.OnClick += (e) =>
        {
            Context.Navigator.Navigate(new ControllerBindingsScreen(Context, this));
        };
        list.AddChild(btnBindings);

        // Reset Button
        Button btnReset = CreateButton();
        btnReset.Style.Width = ButtonSize;
        btnReset.Style.MarginLeft = ButtonPadding;
        btnReset.Style.MarginRight = ButtonPadding;
        btnReset.Style.MarginBottom = 4;
        btnReset.Text = "Reset Bindings...";
        btnReset.OnClick += (e) =>
        {
            foreach (ControllerBinding cb in Options.ControllerBindings)
                cb.Button = cb.DefaultButton;
            Options.SaveOptions();
        };
        list.AddChild(btnReset);

        return list;
    }
}
