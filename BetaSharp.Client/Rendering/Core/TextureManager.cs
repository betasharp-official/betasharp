using BetaSharp.Client.Options;
using BetaSharp.Client.Resource.Pack;
using BetaSharp.Client.Textures;
using Silk.NET.OpenGL.Legacy;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Processing;
using static BetaSharp.Client.Textures.TextureAtlasMipmapGenerator;
using Microsoft.Extensions.Logging;

namespace BetaSharp.Client.Rendering.Core;

public class TextureManager
{
    private readonly ILogger _logger = Log.Instance.For<TextureManager>();
    private readonly Dictionary<string, int> _textures = [];
    private readonly Dictionary<string, int[]> _colors = [];
    private readonly Dictionary<int, Image<Rgba32>> _images = [];
    private readonly List<DynamicTexture> _dynamicTextures = [];
    private readonly Dictionary<string, int> _atlasTileSizes = [];
    private readonly GameOptions _gameOptions;
    private bool _clamp;
    private bool _blur;
    private readonly TexturePacks _texturePacks;
    private readonly Image<Rgba32> _missingTextureImage = new(64, 64);

    public TextureManager(TexturePacks texturePacks, GameOptions options)
    {
        _texturePacks = texturePacks;
        _gameOptions = options;
        _missingTextureImage.Mutate(ctx =>
        {
            ctx.BackgroundColor(Color.Magenta);
            ctx.Fill(Color.Black, new RectangleF(0, 0, 32, 32));
            ctx.Fill(Color.Black, new RectangleF(32, 32, 32, 32));
        });
    }

    public int[] GetColors(string path)
    {
        if (_colors.TryGetValue(path, out int[]? cachedColors)) return cachedColors;
        try
        {
            using var img = LoadImageFromResource(path);
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

    public int Load(Image<Rgba32> image)
    {
        uint newId = GLManager.GL.GenTexture();
        Load(image, (int)newId, false);
        _images[(int)newId] = image;
        return (int)newId;
    }

    public void Load(Image<Rgba32> image, int textureName)
    {
        Load(image, textureName, false);
    }

    public int GetTextureId(string path, bool forceMipmaps = false)
    {
        if (_textures.TryGetValue(path, out int id)) return id;

        uint newId = GLManager.GL.GenTexture();
        try
        {
            using var img = LoadImageFromResource(path);

            _atlasTileSizes[path] = img.Width / 16;

            Load(img, (int)newId, path.Contains("terrain.png") || forceMipmaps);
            _textures[path] = (int)newId;
            return (int)newId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get texture id for path {Path}", path);
            Load(_missingTextureImage, (int)newId);
            _textures[path] = (int)newId;
            return (int)newId;
        }

    }

    public unsafe void Load(Image<Rgba32> image, int textureName, bool isTerrain)
    {
        GLManager.GL.BindTexture(GLEnum.Texture2D, (uint)textureName);

        if (isTerrain)
        {
            int tileSize = image.Width / 16;
            Image<Rgba32>[] mips = GenerateMipmaps(image, tileSize);
            int mipCount = _gameOptions.UseMipmaps ? mips.Length : 1;

            for (int level = 0; level < mipCount; level++)
            {
                var mip = mips[level];
                byte[] pixels = new byte[mip.Width * mip.Height * 4];
                mip.CopyPixelDataTo(pixels);
                fixed (byte* ptr = pixels)
                {
                    GLManager.GL.TexImage2D(
                        TextureTarget.Texture2D,
                        level,
                        InternalFormat.Rgba8,
                        (uint)mip.Width,
                        (uint)mip.Height,
                        0,
                        PixelFormat.Rgba,
                        PixelType.UnsignedByte,
                        ptr
                    );
                }
                if (level > 0) mip.Dispose();
            }

            GLManager.GL.TexParameter(
                TextureTarget.Texture2D,
                TextureParameterName.TextureMinFilter,
                (int)(_gameOptions.UseMipmaps ?
                    TextureMinFilter.NearestMipmapNearest :
                    TextureMinFilter.Nearest)
                );
            GLManager.GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
            GLManager.GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMaxLevel, mipCount - 1);

            if (GLManager.GL.IsExtensionPresent("GL_EXT_texture_filter_anisotropic"))
            {
                float aniso = _gameOptions.AnisotropicLevel == 0 ? 1.0f : (float)Math.Pow(2, _gameOptions.AnisotropicLevel);
                aniso = Math.Clamp(aniso, 1.0f, GameOptions.MaxAnisotropy);

                GLManager.GL.TexParameter(GLEnum.Texture2D, GLEnum.TextureMaxAnisotropy, aniso);
            }
            return;
        }

        GLManager.GL.TexParameter(GLEnum.Texture2D, GLEnum.TextureMinFilter,
            (int)(_blur ?
                    GLEnum.Linear :
                    GLEnum.Nearest)
            );
        GLManager.GL.TexParameter(GLEnum.Texture2D, GLEnum.TextureMagFilter,
            (int)(_blur ?
                    GLEnum.Linear :
                    GLEnum.Nearest)
            );
        GLManager.GL.TexParameter(GLEnum.Texture2D, GLEnum.TextureWrapS,
            (int)(_clamp ?
                    GLEnum.Clamp :
                    GLEnum.Repeat)
            );
        GLManager.GL.TexParameter(GLEnum.Texture2D, GLEnum.TextureWrapT,
            (int)(_clamp ?
                    GLEnum.Clamp :
                    GLEnum.Repeat)
            );


        byte[] rawPixels = new byte[image.Width * image.Height * 4];
        image.CopyPixelDataTo(rawPixels);
        fixed (byte* ptr = rawPixels)
        {
            GLManager.GL.TexImage2D(GLEnum.Texture2D, 0, (int)GLEnum.Rgba, (uint)image.Width, (uint)image.Height, 0, GLEnum.Rgba, GLEnum.UnsignedByte, ptr);
        }

        _clamp = false;
        _blur = false;
    }

    public void BindTexture(int id)
    {
        if (id >= 0) GLManager.GL.BindTexture(GLEnum.Texture2D, (uint)id);
    }

    private Image<Rgba32> Rescale(Image<Rgba32> image)
    {
        int scale = image.Width / 16;
        var rescaled = new Image<Rgba32>(16, image.Height * scale);
        rescaled.Mutate(ctx =>
        {
            for (int i = 0; i < scale; i++)
            {
                using var frame = image.Clone(x => x.Crop(new SixLabors.ImageSharp.Rectangle(i * 16, 0, 16, image.Height)));
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
                var row = accessor.GetRowSpan(y);
                for (int x = 0; x < accessor.Width; x++)
                {
                    var p = row[x];
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
            using var s = pack.GetResourceAsStream(path[2..]);
            return s == null ? _missingTextureImage.Clone() : Rescale(Image.Load<Rgba32>(s));
        }

        string cleanPath = path;
        if (path.StartsWith("%clamp%")) { _clamp = true; cleanPath = path[7..]; }
        else if (path.StartsWith("%blur%")) { _blur = true; cleanPath = path[6..]; }

        using var stream = pack.GetResourceAsStream(cleanPath);
        var img = stream == null ? _missingTextureImage.Clone() : Image.Load<Rgba32>(stream);

        return img;
    }


    public unsafe void Bind(int[] packedARGB, int width, int height, int var4)
    {
        //TODO: this is potentially wrong but shouldn't crash

        GLManager.GL.BindTexture(GLEnum.Texture2D, (uint)var4);

        GLManager.GL.TexParameter(GLEnum.Texture2D, GLEnum.TextureMinFilter,
        (int)(_blur ?
            GLEnum.Linear :
            GLEnum.Nearest
        ));
        GLManager.GL.TexParameter(GLEnum.Texture2D, GLEnum.TextureMagFilter,
        (int)(_blur ?
            GLEnum.Linear :
            GLEnum.Nearest
        ));
        GLManager.GL.TexParameter(GLEnum.Texture2D, GLEnum.TextureWrapS,
        (int)(_clamp ?
            GLEnum.ClampToEdge :
            GLEnum.Repeat
        ));
        GLManager.GL.TexParameter(GLEnum.Texture2D, GLEnum.TextureWrapT,
        (int)(_clamp ?
            GLEnum.ClampToEdge :
            GLEnum.Repeat
        ));

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
            GLManager.GL.TexSubImage2D(GLEnum.Texture2D, 0, 0, 0, (uint)width, (uint)height, GLEnum.Rgba, GLEnum.UnsignedByte, ptr);
        }
    }

    public void Delete(int id)
    {
        if (_images.Remove(id, out var img)) img.Dispose();
        GLManager.GL.DeleteTexture((uint)id);
    }


    public void AddDynamicTexture(DynamicTexture t)
    {
        _dynamicTextures.Add(t);
        t.tick();
    }

    public void Reload()
    {
        _textures.Clear();
        foreach (var entry in _images) Load(entry.Value, entry.Key);
        foreach (var key in new List<string>(_textures.Keys)) GetTextureId(key);
        foreach (var key in new List<string>(_colors.Keys)) GetColors(key);
    }

    public unsafe void Tick()
    {
        foreach (var texture in _dynamicTextures)
        {
            texture.tick();

            string atlasPath = texture.atlas == DynamicTexture.FXImage.Terrain ? "/terrain.png" : "/gui/items.png";
            BindTexture(texture.copyTo > 0 ? texture.copyTo : GetTextureId(atlasPath));

            int targetTileSize = _atlasTileSizes.TryGetValue(atlasPath, out int size) ? size : 16;

            int fxSize = (int)Math.Sqrt(texture.pixels.Length / 4);

            texture.replicate = targetTileSize / fxSize;
            if (texture.replicate < 1) texture.replicate = 1;

            fixed (byte* ptr = texture.pixels)
            {
                for (int x = 0; x < texture.replicate; x++)
                {
                    for (int y = 0; y < texture.replicate; y++)
                    {
                        GLManager.GL.TexSubImage2D(GLEnum.Texture2D, 0,
                           (texture.sprite % 16) * targetTileSize + (x * fxSize),
                           (texture.sprite / 16) * targetTileSize + (y * fxSize),
                           (uint)fxSize, (uint)fxSize, GLEnum.Rgba, GLEnum.UnsignedByte, ptr);
                    }
                }
            }

            if (texture.atlas == DynamicTexture.FXImage.Terrain && _gameOptions.UseMipmaps)
                UpdateTileMipmaps(texture.sprite, texture.pixels, fxSize);
        }
    }

    private unsafe void UpdateTileMipmaps(int tileIndex, byte[] tileData, int tileSize)
    {
        if (!_gameOptions.UseMipmaps) return;

        int maxMipLevels = (int)Math.Log2(tileSize) + 1;
        byte[] currentData = tileData;
        int currentSize = tileSize;

        for (int mipLevel = 1; mipLevel < maxMipLevels; mipLevel++)
        {
            int newSize = currentSize >> 1;
            if (newSize <= 0) break;

            byte[] downsampled = new byte[newSize * newSize * 4];

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

            int mipTileX = tileIndex % 16 * newSize;
            int mipTileY = tileIndex / 16 * newSize;

            fixed (byte* ptr = downsampled)
            {
                GLManager.GL.TexSubImage2D(GLEnum.Texture2D, mipLevel,
                    mipTileX, mipTileY,
                    (uint)newSize, (uint)newSize,
                    GLEnum.Rgba, GLEnum.UnsignedByte, ptr);
            }

            currentData = downsampled;
            currentSize = newSize;
        }
    }
}