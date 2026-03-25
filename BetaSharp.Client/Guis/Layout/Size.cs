namespace BetaSharp.Client.Guis.Layout;

public struct Size
{
    public static readonly Size Empty = new();
    public int Width;
    public int Height;

    public Size(int width, int height)
    {
        Width = width;
        Height = height;
    }
}
