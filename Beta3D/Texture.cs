namespace Beta3D;

public abstract class Texture : IDisposable
{
    [Flags]
    public enum TextureUsage
    {
        CopyDst = 1,
        CopySrc = 2,
        TextureBinding = 4,
        RenderAttachment = 8,
        CubemapCompatible = 16
    }

    public int DepthOrLayers { get; }
    public int MipLevels { get; }
    public TextureFormat Format { get; }
    public TextureUsage Usage { get; }
    public string Label { get; init; }

    public abstract bool IsDisposed { get; }

    private readonly int _width;
    private readonly int _height;

    public Texture(TextureUsage usage, string label, TextureFormat format, int width, int height, int depthOrLayers, int mipLevels)
    {
        Usage = usage;
        Label = label;
        Format = format;
        _width = width;
        _height = height;
        DepthOrLayers = depthOrLayers;
        MipLevels = mipLevels;
    }

    public int GetWidth(int mipLevel) => _width >> mipLevel;
    public int GetHeight(int mipLevel) => _height >> mipLevel;

    public abstract void Dispose();
}
