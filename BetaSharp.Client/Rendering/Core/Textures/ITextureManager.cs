using BetaSharp.Client.DynamicTexture;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace BetaSharp.Client.Rendering.Core.Textures;

public interface ITextureManager : IDisposable
{
    int ActiveTextureCount { get; }

    int[] GetColors(string path);
    int GetAtlasTileSize(string path);

    TextureHandle Load(Image<Rgba32> image);
    TextureHandle GetTextureId(string path);
    void BindTexture(TextureHandle? handle);
    void Bind(int[] packedARGB, int width, int height, ITextureResource texture);

    void Delete(TextureHandle handle);

    void UploadSubImage(
        TextureHandle handle,
        int x,
        int y,
        int width,
        int height,
        ReadOnlySpan<byte> pixelData,
        int level = 0,
        TextureDataFormat format = TextureDataFormat.Rgba);

    void AddDynamicTexture(DynamicTexture texture);
    void Reload();
    void Tick();
}
