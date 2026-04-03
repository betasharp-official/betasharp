namespace BetaSharp.Client.UI;

public class UIEvent
{
    public bool Handled { get; set; } = false;
    public UIElement? Target { get; set; }
}

public enum MouseButton
{
    Left = 0,
    Right = 1,
    Middle = 2,
    Unknown = -1
}

public class UIMouseEvent : UIEvent
{
    public int MouseX { get; set; }
    public int MouseY { get; set; }
    public MouseButton Button { get; set; }
    public int ScrollDelta { get; set; }
}

public class UIKeyEvent : UIEvent
{
    public int KeyCode { get; set; }
    public char KeyChar { get; set; }
    public bool IsDown { get; set; }
}
