namespace BetaSharp.Client.Rendering.Core.Textures;

/// <summary>
/// Backend-owned texture transfer boundary.
/// Shared rendering code submits raw pixel uploads through this service
/// instead of directly issuing backend-specific texture transfer calls.
/// </summary>
public interface ITextureUploadService
{
    void Upload(
        ITextureResource texture,
        int width,
        int height,
        ReadOnlySpan<byte> pixelData,
        int level = 0,
        TextureDataFormat format = TextureDataFormat.Rgba,
        TextureStorageFormat internalFormat = TextureStorageFormat.Rgba);

    void UploadSubImage(
        ITextureResource texture,
        int x,
        int y,
        int width,
        int height,
        ReadOnlySpan<byte> pixelData,
        int level = 0,
        TextureDataFormat format = TextureDataFormat.Rgba);
}
