namespace BetaSharp.Client.Guis.Layout;

public struct Rect
{
    public static readonly Rect Empty = new();
    public int Top;
    public int Left;
    public int Width;
    public int Height;

    public int Right => Left + Width;
    public int Bottom => Top + Height;

    public Rect(int left, int top, int width, int height)
    {
        Left = left;
        Top = top;
        Width = width;
        Height = height;
    }
}
