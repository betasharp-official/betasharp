namespace BetaSharp.Client.Rendering.Core.Textures;

public interface ITextureResource : IDisposable
{
    uint Id { get; }
    string Source { get; }
    int Width { get; }
    int Height { get; }

    void Bind();
    void SetFilter(TextureMinificationFilter min, TextureMagnificationFilter mag);
    void SetWrap(TextureAddressMode s, TextureAddressMode t);
    void SetMaxLevel(int level);

    unsafe void Upload(
        int width,
        int height,
        byte* ptr,
        int level = 0,
        TextureDataFormat format = TextureDataFormat.Rgba,
        TextureStorageFormat internalFormat = TextureStorageFormat.Rgba);

    unsafe void UploadSubImage(
        int x,
        int y,
        int width,
        int height,
        byte* ptr,
        int level = 0,
        TextureDataFormat format = TextureDataFormat.Rgba);

    void SetAnisotropicFilter(float level);
}
