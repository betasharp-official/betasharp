using Silk.NET.GLFW;

namespace BetaSharp.Client.Input;

public class ControllerBinding
{
    public string ActionKey { get; }

    public string Description { get; }

    public GamepadButton Button { get; set; }

    public GamepadButton DefaultButton { get; }

    public ControllerBinding(string actionKey, string description, GamepadButton defaultButton)
    {
        ActionKey = actionKey;
        Description = description;
        DefaultButton = defaultButton;
        Button = defaultButton;
    }

    public string GetButtonName() => Button switch
    {
        _ when (int)Button < 0 => "Unbound",
        GamepadButton.A => "A",
        GamepadButton.B => "B",
        GamepadButton.X => "X",
        GamepadButton.Y => "Y",
        GamepadButton.LeftBumper => "LB",
        GamepadButton.RightBumper => "RB",
        GamepadButton.Back => "Back",
        GamepadButton.Start => "Start",
        GamepadButton.LeftStick => "L3",
        GamepadButton.RightStick => "R3",
        GamepadButton.DPadUp => "DPad Up",
        GamepadButton.DPadDown => "DPad Down",
        GamepadButton.DPadLeft => "DPad Left",
        GamepadButton.DPadRight => "DPad Right",
        _ => ((int)Button).ToString()
    };
}
