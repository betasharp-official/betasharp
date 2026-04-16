using System.Threading;

namespace BetaSharp.Client.Rendering.Core.Textures;

internal sealed class NoOpTextureResource : ITextureResource
{
    private static int s_nextId;
    private readonly Action _onDispose;
    private bool _disposed;

    public uint Id { get; }
    public string Source { get; }
    public int Width { get; private set; }
    public int Height { get; private set; }

    public NoOpTextureResource(string source, Action onDispose)
    {
        Id = (uint)Interlocked.Increment(ref s_nextId);
        Source = source;
        _onDispose = onDispose;
    }

    public void Bind()
    {
    }

    public void SetFilter(TextureMinificationFilter min, TextureMagnificationFilter mag)
    {
    }

    public void SetWrap(TextureAddressMode s, TextureAddressMode t)
    {
    }

    public void SetMaxLevel(int level)
    {
    }

    public unsafe void Upload(
        int width,
        int height,
        byte* ptr,
        int level = 0,
        TextureDataFormat format = TextureDataFormat.Rgba,
        TextureStorageFormat internalFormat = TextureStorageFormat.Rgba)
    {
        if (level == 0)
        {
            Width = width;
            Height = height;
        }
    }

    public unsafe void UploadSubImage(
        int x,
        int y,
        int width,
        int height,
        byte* ptr,
        int level = 0,
        TextureDataFormat format = TextureDataFormat.Rgba)
    {
        if (level == 0 && Width == 0 && Height == 0)
        {
            Width = width;
            Height = height;
        }
    }

    public void SetAnisotropicFilter(float level)
    {
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _onDispose();
        GC.SuppressFinalize(this);
    }
}
