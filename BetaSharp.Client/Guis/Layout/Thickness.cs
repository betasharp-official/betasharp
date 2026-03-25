namespace BetaSharp.Client.Guis.Layout;

public struct Thickness
{
    public static readonly Thickness Empty = new();
    public int Left;
    public int Top;
    public int Right;
    public int Bottom;
    public int Horizontal => Left + Right;
    public int Vertical => Top + Bottom;

    public Thickness(int left, int top, int right, int bottom)
    {
        Left = left;
        Top = top;
        Right = right;
        Bottom = bottom;
    }

    public Thickness(int horizontal, int vertical)
    {
        Left = Right = horizontal;
        Top = Bottom = vertical;
    }

    public Thickness(int all)
    {
        Left = Top = Right = Bottom = all;
    }
}
