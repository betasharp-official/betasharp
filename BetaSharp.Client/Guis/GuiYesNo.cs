using BetaSharp.Client.Guis.Controls;
using BetaSharp.Client.Guis.Layout;

namespace BetaSharp.Client.Guis;

public class GuiYesNo : Screen
{
    private readonly string _message1;
    private readonly string _message2;

    public GuiYesNo(Screen parentScreen, string message1, string message2, string confirmButtonText, string cancelButtonText, int worldNumber)
    {
        _message1 = message1;
        _message2 = message2;

        Control container = new(EffectiveWidth / 2 - 155, EffectiveHeight / 2 - 50, 310, 100)
            { VerticalCenteringBehavior = CenteringBehavior.Middle };
        Label messageLabel1 = new(0, 0, container.EffectiveWidth, 20, message1, 0xFFFFFF)
            { TextAlign = Alignment.Top };
        Label messageLabel2 = new(0, 20, container.EffectiveWidth, 20, message2, 0xFFFFFF)
            { TextAlign = Alignment.Top };
        Button confirmButton = new(0, 80, confirmButtonText) { EffectiveSize = new(150, 20) };
        Button cancelButton = new(160, 80, cancelButtonText) { EffectiveSize = new(150, 20) };

        confirmButton.Clicked += (_, _) => parentScreen.DeleteWorld(true, worldNumber);
        cancelButton.Clicked += (_, _) => parentScreen.DeleteWorld(false, worldNumber);

        container.AddChildren(messageLabel1, messageLabel2, confirmButton, cancelButton);
        AddChild(container);
    }

    protected override void OnRender(RenderEventArgs e)
    {
        DrawDefaultBackground();
    }
}
