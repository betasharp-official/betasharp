namespace BetaSharp.Client.Guis.Layout;

public struct Point
{
    public static readonly Point Empty = new();
    public int X;
    public int Y;

    public Point(int x, int y)
    {
        X = x;
        Y = y;
    }
}
