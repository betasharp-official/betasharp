using Microsoft.Extensions.Logging;
using Silk.NET.OpenGL;
using GLEnum = BetaSharp.Client.Rendering.Core.OpenGL.GLEnum;

namespace BetaSharp.Client.Rendering.Core.Textures;

public class GLTexture : IDisposable
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

    public void SetFilter(TextureMinFilter min, TextureMagFilter mag)
    {
        Bind();
        GLManager.GL.TexParameter(GLEnum.Texture2D, GLEnum.TextureMinFilter, (int)min);
        GLManager.GL.TexParameter(GLEnum.Texture2D, GLEnum.TextureMagFilter, (int)mag);
    }

    public void SetWrap(TextureWrapMode s, TextureWrapMode t)
    {
        Bind();
        GLManager.GL.TexParameter(GLEnum.Texture2D, GLEnum.TextureWrapS, (int)s);
        GLManager.GL.TexParameter(GLEnum.Texture2D, GLEnum.TextureWrapT, (int)t);
    }

    public void SetMaxLevel(int level)
    {
        Bind();
        GLManager.GL.TexParameter(GLEnum.Texture2D, GLEnum.TextureMaxLevel, level);
    }

    public unsafe void Upload(int width, int height, byte* ptr, int level = 0, PixelFormat format = PixelFormat.Rgba, InternalFormat internalFormat = InternalFormat.Rgba)
    {
        if (level == 0)
        {
            Width = width;
            Height = height;
        }
        Bind();
        GLManager.GL.TexImage2D(TextureTarget.Texture2D, level, internalFormat, (uint)width, (uint)height, 0, format, PixelType.UnsignedByte, ptr);
    }

    public unsafe void UploadSubImage(int x, int y, int width, int height, byte* ptr, int level = 0, PixelFormat format = PixelFormat.Rgba)
    {
        Bind();
        GLManager.GL.TexSubImage2D(GLEnum.Texture2D, level, x, y, (uint)width, (uint)height, (GLEnum)format, (GLEnum)PixelType.UnsignedByte, ptr);
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
            s_logger.LogWarning("Leaked Texture ID: {Id}, Source: {Source}, Created At: {CreatedAt}", entry.Key, entry.Value.Source, entry.Value.CreatedAt);
        }
    }
}
