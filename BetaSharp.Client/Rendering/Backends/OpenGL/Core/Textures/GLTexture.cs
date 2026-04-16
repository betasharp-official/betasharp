using Microsoft.Extensions.Logging;
using Silk.NET.OpenGL;
using GLEnum = BetaSharp.Client.Rendering.Core.OpenGL.GLEnum;

namespace BetaSharp.Client.Rendering.Core.Textures;

public class GLTexture : ITextureResource
{
    private static readonly ILogger s_logger = Log.Instance.For<GLTexture>();
    private static readonly Dictionary<uint, (string Source, DateTime CreatedAt)> s_activeTextures = [];

    public uint Id { get; private set; }
    public string Source { get; }
    public int Width { get; private set; }
    public int Height { get; private set; }
    public static int ActiveTextureCount => s_activeTextures.Count;

    public GLTexture(string source)
    {
        Source = source;
        Id = GLManager.GL.GenTexture();
        s_activeTextures.Add(Id, (source, DateTime.Now));
    }

    public void Bind()
    {
        if (Id != 0)
        {
            TextureStats.NotifyBind();
            GLManager.GL.BindTexture(GLEnum.Texture2D, Id);
        }
    }

    public void SetFilter(TextureMinificationFilter min, TextureMagnificationFilter mag)
    {
        Bind();
        GLManager.GL.TexParameter(GLEnum.Texture2D, GLEnum.TextureMinFilter, (int)MapMinFilter(min));
        GLManager.GL.TexParameter(GLEnum.Texture2D, GLEnum.TextureMagFilter, (int)MapMagFilter(mag));
    }

    public void SetWrap(TextureAddressMode s, TextureAddressMode t)
    {
        Bind();
        GLManager.GL.TexParameter(GLEnum.Texture2D, GLEnum.TextureWrapS, (int)MapAddressMode(s));
        GLManager.GL.TexParameter(GLEnum.Texture2D, GLEnum.TextureWrapT, (int)MapAddressMode(t));
    }

    public void SetMaxLevel(int level)
    {
        Bind();
        GLManager.GL.TexParameter(GLEnum.Texture2D, GLEnum.TextureMaxLevel, level);
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

        Bind();
        GLManager.GL.TexImage2D(
            TextureTarget.Texture2D,
            level,
            MapStorageFormat(internalFormat),
            (uint)width,
            (uint)height,
            0,
            MapDataFormat(format),
            PixelType.UnsignedByte,
            ptr);
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
        Bind();
        GLManager.GL.TexSubImage2D(
            GLEnum.Texture2D,
            level,
            x,
            y,
            (uint)width,
            (uint)height,
            (GLEnum)MapDataFormat(format),
            (GLEnum)PixelType.UnsignedByte,
            ptr);
    }

    public void SetAnisotropicFilter(float level)
    {
        if (GLManager.GL.IsExtensionPresent("GL_EXT_texture_filter_anisotropic"))
        {
            Bind();
            GLManager.GL.TexParameter(GLEnum.Texture2D, (GLEnum)0x84FE, level); // GL_TEXTURE_MAX_ANISOTROPY_EXT
        }
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);

        if (Id != 0)
        {
            GLManager.GL.DeleteTexture(Id);
            s_activeTextures.Remove(Id, out _);
            Id = 0;
        }
    }

    public static void LogLeakReport()
    {
        if (s_activeTextures.Count == 0) return;

        s_logger.LogWarning("Found {Count} leaked OpenGL textures on shutdown!", s_activeTextures.Count);
        foreach (KeyValuePair<uint, (string Source, DateTime CreatedAt)> entry in s_activeTextures)
        {
            s_logger.LogWarning("Leaked Texture ID: {Id}, Source: {Source}, Created At: {CreatedAt}", entry.Key,
                entry.Value.Source, entry.Value.CreatedAt);
        }
    }

    private static TextureMinFilter MapMinFilter(TextureMinificationFilter filter)
    {
        return filter switch
        {
            TextureMinificationFilter.Nearest => TextureMinFilter.Nearest,
            TextureMinificationFilter.Linear => TextureMinFilter.Linear,
            TextureMinificationFilter.NearestMipmapNearest => TextureMinFilter.NearestMipmapNearest,
            _ => TextureMinFilter.Nearest
        };
    }

    private static TextureMagFilter MapMagFilter(TextureMagnificationFilter filter)
    {
        return filter switch
        {
            TextureMagnificationFilter.Nearest => TextureMagFilter.Nearest,
            TextureMagnificationFilter.Linear => TextureMagFilter.Linear,
            _ => TextureMagFilter.Nearest
        };
    }

    private static TextureWrapMode MapAddressMode(TextureAddressMode mode)
    {
        return mode switch
        {
            TextureAddressMode.Repeat => TextureWrapMode.Repeat,
            TextureAddressMode.ClampToEdge => TextureWrapMode.ClampToEdge,
            _ => TextureWrapMode.Repeat
        };
    }

    private static PixelFormat MapDataFormat(TextureDataFormat format)
    {
        return format switch
        {
            TextureDataFormat.Rgba => PixelFormat.Rgba,
            TextureDataFormat.Rgb => PixelFormat.Rgb,
            _ => PixelFormat.Rgba
        };
    }

    private static InternalFormat MapStorageFormat(TextureStorageFormat format)
    {
        return format switch
        {
            // Use a sized internal format in core-profile contexts to avoid incomplete textures.
            TextureStorageFormat.Rgba => InternalFormat.Rgba8,
            TextureStorageFormat.Rgba8 => InternalFormat.Rgba8,
            _ => InternalFormat.Rgba8
        };
    }
}
