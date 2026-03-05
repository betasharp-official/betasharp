namespace BetaSharp.Client.Input;

public class MouseHelper
{
    public int DeltaX { get; private set; }
    public int DeltaY { get; private set; }

    public MouseHelper()
    {
    }

    public void grabMouseCursor()
    {
        Mouse.setGrabbed(true);
        DeltaX = 0;
        DeltaY = 0;
    }

    public void ungrabMouseCursor()
    {
        Mouse.setGrabbed(false);
        Mouse.setCursorPosition(Display.getWidth() / 2, Display.getHeight() / 2);
    }

    public void mouseXYChange()
    {
        DeltaX = Mouse.getDX();
        DeltaY = Mouse.getDY();
    }
}