using Silk.NET.Maths;

namespace BetaSharp.Client.Input;

public class MouseHelper
{
    public int DeltaX { get; private set; }
    public int DeltaY { get; private set; }

    /// <summary>
    /// Returns the window pixel position the cursor should warp to when ungrabbed.
    /// </summary>
    public required Func<Vector2D<int>> GetUngrabCenter { get; set; }

    public void GrabMouseCursor()
    {
        Mouse.setGrabbed(true);
        DeltaX = 0;
        DeltaY = 0;
    }

    public void UngrabMouseCursor()
    {
        Mouse.setGrabbed(false);
        Vector2D<int> center = GetUngrabCenter();
        Mouse.setCursorPosition(center.X, center.Y);
    }

    public void MouseXYChange()
    {
        DeltaX = Mouse.getDX();
        DeltaY = Mouse.getDY();
    }
}
