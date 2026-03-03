using BetaSharp.Client.Options;
using BetaSharp.Client.Resource.Pack;
using Silk.NET.OpenGL.Legacy;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Processing;
using static BetaSharp.Client.Rendering.Core.Textures.TextureAtlasMipmapGenerator;
using Microsoft.Extensions.Logging;
using System.Buffers;

namespace BetaSharp.Client.Rendering.Core.Textures;

public class TextureManager : IDisposable
{
    private readonly ILogger _logger = Log.Instance.For<TextureManager>();
    private readonly Dictionary<string, TextureHandle> _textures = [];
    private readonly Dictionary<string, int[]> _colors = [];
    private readonly Dictionary<uint, (Image<Rgba32> Image, TextureHandle Handle)> _images = [];
    private readonly List<DynamicTexture> _dynamicTextures = [];
    private readonly Dictionary<string, int> _atlasTileSizes = [];
    private TextureHandle? _terrainHandle;
    private TextureHandle? _itemsHandle;
    private readonly GameOptions _gameOptions;
    private bool _clamp;
    private bool _blur;
    private readonly TexturePacks _texturePacks;
    private readonly BetaSharp _game;
    private readonly Image<Rgba32> _missingTextureImage = new(256, 256);

    public TextureManager(BetaSharp game, TexturePacks texturePacks, GameOptions options)
    {
        _game = game;
        _texturePacks = texturePacks;
        _gameOptions = options;
        _missingTextureImage.Mutate(ctx =>
        {
            ctx.BackgroundColor(Color.Magenta);
            ctx.Fill(Color.Black, new RectangleF(0, 0, 128, 128));
            ctx.Fill(Color.Black, new RectangleF(128, 128, 128, 128));
        });
    }

    public int[] GetColors(string path)
    {
        if (_colors.TryGetValue(path, out int[]? cachedColors)) return cachedColors;
        try
        {
            using Image<Rgba32> img = LoadImageFromResource(path);
            int[] result = ReadColorsFromImage(img);
            _colors[path] = result;
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get colors from image {Path}", path);
            int[] fallback = ReadColorsFromImage(_missingTextureImage);
            _colors[path] = fallback;
            return fallback;
        }

    }

    public int GetAtlasTileSize(string path)
    {
        if (_atlasTileSizes.TryGetValue(path, out int size)) return size;
        return 16;
    }

    public TextureHandle Load(Image<Rgba32> image)
    {
        var texture = new GLTexture("Image_Direct");
        Load(image, texture, false);
        var handle = new TextureHandle(this, texture);
        _images[texture.Id] = (image, handle);
        return handle;
    }

    public TextureHandle GetTextureId(string path)
    {
        if (_textures.TryGetValue(path, out TextureHandle? handle)) return handle;

        var texture = new GLTexture(path);
        handle = new TextureHandle(this, texture);
        _textures[path] = handle;

        try
        {
            using Image<Rgba32> img = LoadImageFromResource(path);

            _atlasTileSizes[path] = img.Width / 16;

            Load(img, texture, path.Contains("terrain.png"));
            return handle;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get texture id for path {Path}", path);
            Load(_missingTextureImage, texture, false);
            return handle;
        }

    }

    public unsafe void Load(Image<Rgba32> image, GLTexture texture, bool isTerrain)
    {
        texture.Bind();

        if (isTerrain)
        {
            int tileSize = image.Width / 16;
            Image<Rgba32>[] mips = GenerateMipmaps(image, tileSize);
            int mipCount = _gameOptions.UseMipmaps ? mips.Length : 1;

            for (int level = 0; level < mipCount; level++)
            {
                Image<Rgba32> mip = mips[level];
                byte[] pixels = new byte[mip.Width * mip.Height * 4];
                mip.CopyPixelDataTo(pixels);
                fixed (byte* ptr = pixels)
                {
                    texture.Upload(mip.Width, mip.Height, ptr, level, PixelFormat.Rgba, InternalFormat.Rgba8);
                }
                if (level > 0) mip.Dispose();
            }

            texture.SetFilter(_gameOptions.UseMipmaps ? TextureMinFilter.NearestMipmapNearest : TextureMinFilter.Nearest, TextureMagFilter.Nearest);
            texture.SetMaxLevel(mipCount - 1);

            float aniso = _gameOptions.AnisotropicLevel == 0 ? 1.0f : (float)Math.Pow(2, _gameOptions.AnisotropicLevel);
            aniso = Math.Clamp(aniso, 1.0f, GameOptions.MaxAnisotropy);

            texture.SetAnisotropicFilter(aniso);

            return;
        }

        texture.SetFilter(_blur ? TextureMinFilter.Linear : TextureMinFilter.Nearest, _blur ? TextureMagFilter.Linear : TextureMagFilter.Nearest);
        texture.SetWrap(_clamp ? TextureWrapMode.ClampToEdge : TextureWrapMode.Repeat, _clamp ? TextureWrapMode.ClampToEdge : TextureWrapMode.Repeat);

        byte[] rawPixels = new byte[image.Width * image.Height * 4];
        image.CopyPixelDataTo(rawPixels);
        fixed (byte* ptr = rawPixels)
        {
            texture.Upload(image.Width, image.Height, ptr, 0, PixelFormat.Rgba, InternalFormat.Rgba);
        }

        _clamp = false;
        _blur = false;
    }

    public void BindTexture(TextureHandle? handle)
    {
        handle?.Bind();
    }

    private Image<Rgba32> Rescale(Image<Rgba32> image)
    {
        int scale = image.Width / 16;
        var rescaled = new Image<Rgba32>(16, image.Height * scale);
        rescaled.Mutate(ctx =>
        {
            for (int i = 0; i < scale; i++)
            {
                using Image<Rgba32> frame = image.Clone(x => x.Crop(new SixLabors.ImageSharp.Rectangle(i * 16, 0, 16, image.Height)));
                ctx.DrawImage(frame, new SixLabors.ImageSharp.Point(0, i * image.Height), 1f);
            }
        });
        return rescaled;
    }

    private int[] ReadColorsFromImage(Image<Rgba32> image)
    {
        int[] argb = new int[image.Width * image.Height];
        image.ProcessPixelRows(accessor =>
        {
            for (int y = 0; y < accessor.Height; y++)
            {
                Span<Rgba32> row = accessor.GetRowSpan(y);
                for (int x = 0; x < accessor.Width; x++)
                {
                    Rgba32 p = row[x];
                    argb[y * accessor.Width + x] = (p.A << 24) | (p.R << 16) | (p.G << 8) | p.B;
                }
            }
        });
        return argb;
    }


    private Image<Rgba32> LoadImageFromResource(string path)
    {
        TexturePack pack = _texturePacks.SelectedTexturePack;

        if (path.StartsWith("##"))
        {
            using Stream? s = pack.GetResourceAsStream(path[2..]);
            return s == null ? _missingTextureImage.Clone() : Rescale(Image.Load<Rgba32>(s));
        }

        string cleanPath = path;
        if (path.StartsWith("%clamp%")) { _clamp = true; cleanPath = path[7..]; }
        else if (path.StartsWith("%blur%")) { _blur = true; cleanPath = path[6..]; }

        using Stream? stream = pack.GetResourceAsStream(cleanPath);
        Image<Rgba32> img = stream == null ? _missingTextureImage.Clone() : Image.Load<Rgba32>(stream);

        return img;
    }


    public unsafe void Bind(int[] packedARGB, int width, int height, GLTexture texture)
    {
        //TODO: this is potentially wrong but shouldn't crash

        texture.Bind();

        texture.SetFilter(_blur ? TextureMinFilter.Linear : TextureMinFilter.Nearest, _blur ? TextureMagFilter.Linear : TextureMagFilter.Nearest);
        texture.SetWrap(_clamp ? TextureWrapMode.ClampToEdge : TextureWrapMode.Repeat, _clamp ? TextureWrapMode.ClampToEdge : TextureWrapMode.Repeat);

        byte[] unpackedRGBA = new byte[width * height * 4];

        for (int i = 0; i < packedARGB.Length; ++i)
        {
            int a = packedARGB[i] >> 24 & 255;
            int r = packedARGB[i] >> 16 & 255;
            int g = packedARGB[i] >> 8 & 255;
            int b = packedARGB[i] & 255;

            unpackedRGBA[i * 4 + 0] = (byte)r;
            unpackedRGBA[i * 4 + 1] = (byte)g;
            unpackedRGBA[i * 4 + 2] = (byte)b;
            unpackedRGBA[i * 4 + 3] = (byte)a;
        }

        fixed (byte* ptr = unpackedRGBA)
        {
            texture.UploadSubImage(0, 0, width, height, ptr, 0, PixelFormat.Rgba);
        }
    }

    public void Delete(GLTexture texture)
    {
        KeyValuePair<string, TextureHandle> textureEntry = _textures.FirstOrDefault(x => x.Value.Texture == texture);
        if (textureEntry.Key != null) _textures.Remove(textureEntry.Key);

        _images.Remove(texture.Id);
        texture.Dispose();
    }

    public void Delete(TextureHandle handle)
    {
        if (handle.Texture != null) Delete(handle.Texture);
    }


    public void AddDynamicTexture(DynamicTexture t)
    {
        _dynamicTextures.Add(t);
        t.Setup(_game);
        t.tick();

        _terrainHandle = null;
        _itemsHandle = null;
    }

    public void Reload()
    {
        _atlasTileSizes.Clear();
        foreach (KeyValuePair<string, TextureHandle> entry in _textures)
        {
            entry.Value.Texture?.Dispose();

            var newTexture = new GLTexture(entry.Key);
            entry.Value.Texture = newTexture;

            try
            {
                using Image<Rgba32> img = LoadImageFromResource(entry.Key);
                _atlasTileSizes[entry.Key] = img.Width / 16;
                Load(img, newTexture, entry.Key.Contains("terrain.png"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to reload texture {Path}", entry.Key);
                _atlasTileSizes[entry.Key] = _missingTextureImage.Width / 16;
                Load(_missingTextureImage, newTexture, false);
            }
        }

        var oldImages = new Dictionary<uint, (Image<Rgba32> Image, TextureHandle Handle)>(_images);
        _images.Clear();
        foreach (KeyValuePair<uint, (Image<Rgba32> Image, TextureHandle Handle)> entry in oldImages)
        {
            entry.Value.Handle.Texture?.Dispose();

            var newTexture = new GLTexture(entry.Value.Handle.Texture?.Source ?? "Image_Direct_Reload");
            entry.Value.Handle.Texture = newTexture;
            Load(entry.Value.Image, newTexture, false);
            _images[newTexture.Id] = entry.Value;
        }

        foreach (string key in new List<string>(_colors.Keys)) GetColors(key);

        foreach (DynamicTexture dynamicTexture in _dynamicTextures)
        {
            dynamicTexture.Setup(_game);
        }

        _terrainHandle = null;
        _itemsHandle = null;
    }

    public unsafe void Tick()
    {
        _terrainHandle ??= _textures.FirstOrDefault(x => x.Key.EndsWith("/terrain.png")).Value
                           ?? GetTextureId("/terrain.png");
        _itemsHandle ??= _textures.FirstOrDefault(x => x.Key.EndsWith("/gui/items.png")).Value
                         ?? GetTextureId("/gui/items.png");

        foreach (DynamicTexture texture in _dynamicTextures)
        {
            texture.tick();

            TextureHandle atlasHandle = texture.Atlas == DynamicTexture.FxImage.Terrain
                ? _terrainHandle
                : _itemsHandle;

            GLTexture? atlasTexture = atlasHandle?.Texture;
            if (atlasTexture == null) continue;

            int targetTileSize = atlasTexture.Width / 16;

            int tileX = (texture.Sprite % 16) * targetTileSize;
            int tileY = (texture.Sprite / 16) * targetTileSize;

            int fxSize = (int)Math.Sqrt(texture.Pixels.Length / 4);
            int scale = targetTileSize / fxSize;
            if (scale < 1) scale = 1;

            byte[] uploadPixels = texture.Pixels;
            int uploadSize = fxSize;
            byte[]? rentedArray = null;

            try
            {
                if (scale > 1)
                {
                    uploadSize = fxSize * scale;
                    rentedArray = System.Buffers.ArrayPool<byte>.Shared.Rent(uploadSize * uploadSize * 4);
                    UpscaleNearestNeighbor(texture.Pixels, rentedArray, fxSize, uploadSize, scale);
                    uploadPixels = rentedArray;
                }

                int finalReplicate = texture.Replicate;

                fixed (byte* ptr = uploadPixels)
                {
                    for (int x = 0; x < finalReplicate; x++)
                    {
                        for (int y = 0; y < finalReplicate; y++)
                        {
                            atlasTexture.UploadSubImage(
                               tileX + (x * uploadSize),
                               tileY + (y * uploadSize),
                               uploadSize, uploadSize, ptr, 0, PixelFormat.Rgba);
                        }
                    }
                }

                if (texture.Atlas == DynamicTexture.FxImage.Terrain && _gameOptions.UseMipmaps)
                {
                    for (int x = 0; x < finalReplicate; x++)
                    {
                        for (int y = 0; y < finalReplicate; y++)
                        {
                            UpdateTileMipmaps(tileX + (x * uploadSize), tileY + (y * uploadSize), uploadSize, targetTileSize, uploadPixels, atlasTexture);
                        }
                    }
                }
            }
            finally
            {
                if (rentedArray != null)
                {
                    System.Buffers.ArrayPool<byte>.Shared.Return(rentedArray);
                }
            }
        }
    }

    private static void UpscaleNearestNeighbor(byte[] src, byte[] dst, int srcSize, int dstSize, int scale)
    {
        ReadOnlySpan<byte> srcSpan = src;
        Span<byte> dstSpan = dst;

        for (int y = 0; y < dstSize; y++)
        {
            int srcY = y / scale;
            for (int x = 0; x < dstSize; x++)
            {
                int srcX = x / scale;
                int srcIdx = (srcY * srcSize + srcX) * 4;
                int dstIdx = (y * dstSize + x) * 4;

                dstSpan[dstIdx] = srcSpan[srcIdx];
                dstSpan[dstIdx + 1] = srcSpan[srcIdx + 1];
                dstSpan[dstIdx + 2] = srcSpan[srcIdx + 2];
                dstSpan[dstIdx + 3] = srcSpan[srcIdx + 3];
            }
        }
    }

    private unsafe void UpdateTileMipmaps(int baseX, int baseY, int dataSize, int targetTileSize, byte[] tileData, GLTexture texture)
    {
        int maxMipLevels = (int)Math.Log2(targetTileSize) + 1;
        byte[] currentData = tileData;
        int currentSize = dataSize;

        for (int mipLevel = 1; mipLevel < maxMipLevels; mipLevel++)
        {
            int newSize = currentSize >> 1;
            if (newSize < 1) newSize = 1;

            byte[] downsampled = ArrayPool<byte>.Shared.Rent(newSize * newSize * 4);

            try
            {
                if (currentSize > 1)
                {
                    for (int y = 0; y < newSize; y++)
                    {
                        for (int x = 0; x < newSize; x++)
                        {
                            int src0 = ((y * 2) * currentSize + (x * 2)) * 4;
                            int src1 = ((y * 2) * currentSize + (x * 2 + 1)) * 4;
                            int src2 = ((y * 2 + 1) * currentSize + (x * 2)) * 4;
                            int src3 = ((y * 2 + 1) * currentSize + (x * 2 + 1)) * 4;

                            int dst = (y * newSize + x) * 4;

                            downsampled[dst] = (byte)((currentData[src0] + currentData[src1] + currentData[src2] + currentData[src3]) >> 2);
                            downsampled[dst + 1] = (byte)((currentData[src0 + 1] + currentData[src1 + 1] + currentData[src2 + 1] + currentData[src3 + 1]) >> 2);
                            downsampled[dst + 2] = (byte)((currentData[src0 + 2] + currentData[src1 + 2] + currentData[src2 + 2] + currentData[src3 + 2]) >> 2);
                            downsampled[dst + 3] = (byte)((currentData[src0 + 3] + currentData[src1 + 3] + currentData[src2 + 3] + currentData[src3 + 3]) >> 2);
                        }
                    }
                }
                else
                {
                    for (int i = 0; i < 4; i++) downsampled[i] = currentData[i];
                }

                int mipX = baseX >> mipLevel;
                int mipY = baseY >> mipLevel;

                fixed (byte* ptr = downsampled)
                {
                    texture.UploadSubImage(mipX, mipY, newSize, newSize, ptr, mipLevel, PixelFormat.Rgba);
                }

                if (mipLevel > 1)
                {
                    ArrayPool<byte>.Shared.Return(currentData);
                }

                currentData = downsampled;
                currentSize = newSize;
            }
            catch
            {
                ArrayPool<byte>.Shared.Return(downsampled);
                throw;
            }
        }

        if (currentData != tileData)
        {
            ArrayPool<byte>.Shared.Return(currentData);
        }
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);

        foreach (TextureHandle handle in _textures.Values)
        {
            handle.Texture?.Dispose();
        }
        _textures.Clear();

        foreach ((Image<Rgba32> Image, TextureHandle Handle) entry in _images.Values)
        {
            entry.Handle.Texture?.Dispose();
            entry.Image.Dispose();
        }
        _images.Clear();

        _missingTextureImage.Dispose();
        _colors.Clear();
        _dynamicTextures.Clear();
    }
}
