namespace BetaSharp.Client.Guis;

public class MouseEventArgs : EventArgs
{
    public int X { get; }
    public int Y { get; }
    public float PixelX { get; }
    public float PixelY { get; }
    public int Button { get; }
    public int Clicks { get; }
    public bool Pressed { get; }
    public bool Handled { get; set; }

    public MouseEventArgs(int x, int y, float pixelX, float pixelY, int button, int clicks, bool pressed)
    {
        X = x;
        Y = y;
        PixelX = pixelX;
        PixelY = pixelY;
        Button = button;
        Clicks = clicks;
        Pressed = pressed;
        Handled = false;
    }
}

public class KeyboardEventArgs : EventArgs
{
    public int Key { get; }
    public char KeyChar { get; }
    public bool IsKeyDown { get; }
    public bool IsRepeat { get; }
    public bool Handled { get; set; }

    public KeyboardEventArgs(int key, char keyChar, bool isKeyDown, bool isRepeat)
    {
        Key = key;
        KeyChar = keyChar;
        IsKeyDown = isKeyDown;
        IsRepeat = isRepeat;
        Handled = false;
    }
}

public class RenderEventArgs : EventArgs
{
    public int MouseX { get; }
    public int MouseY { get; }
    public float TickDelta { get; }

    public RenderEventArgs(int mouseX, int mouseY, float tickDelta)
    {
        MouseX = mouseX;
        MouseY = mouseY;
        TickDelta = tickDelta;
    }
}

public class FocusEventArgs : EventArgs
{
    public bool Focused { get; }
    /// <summary>
    /// The control that lost focus (if Focused is true)
    /// or gained focus (if Focused is false), or null if
    /// no control was involved.
    /// </summary>
    public Control? OtherControl { get; }

    public FocusEventArgs(bool focused, Control? otherControl)
    {
        Focused = focused;
        OtherControl = otherControl;
    }
}

public class TextEventArgs : EventArgs
{
    public string Text { get; }

    public TextEventArgs(string text)
    {
        Text = text;
    }
}
