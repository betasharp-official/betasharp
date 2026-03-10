using Microsoft.Extensions.Logging;
using Silk.NET.OpenGL;

namespace BetaSharp.Client.Rendering.Core;

public class Framebuffer : IDisposable
{
    private readonly ILogger<Framebuffer> _logger = Log.Instance.For<Framebuffer>();

    public uint FboId { get; private set; }
    public uint TextureId { get; private set; }
    public uint RenderBufferId { get; private set; }

    public int Width { get; private set; }
    public int Height { get; private set; }

    public Framebuffer(int width, int height)
    {
        CreateFramebuffer(width, height);
    }

    public void CreateFramebuffer(int width, int height)
    {
        Width = width;
        Height = height;

        IGL gl = GLManager.GL;

        FboId = gl.GenFramebuffer();
        gl.BindFramebuffer(FramebufferTarget.Framebuffer, FboId);

        TextureId = gl.GenTexture();
        gl.BindTexture((OpenGL.GLEnum)GLEnum.Texture2D, TextureId);

        unsafe
        {
            gl.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.Rgb, (uint)width, (uint)height, 0, PixelFormat.Rgb, PixelType.UnsignedByte, null);
        }
        gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
        gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
        gl.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, TextureId, 0);

        uint[] rboArray = new uint[1];
        gl.GenRenderbuffers(rboArray);
        RenderBufferId = rboArray[0];

        gl.BindRenderbuffer(RenderbufferTarget.Renderbuffer, RenderBufferId);
        gl.RenderbufferStorage(RenderbufferTarget.Renderbuffer, InternalFormat.Depth24Stencil8, (uint)width, (uint)height);
        gl.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthStencilAttachment, RenderbufferTarget.Renderbuffer, RenderBufferId);

        GLEnum status = gl.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
        if (status != GLEnum.FramebufferComplete)
        {
            _logger.LogError("Framebuffer is not complete! Status: {}", status);
        }

        gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
    }

    public void Bind()
    {
        GLManager.GL.BindFramebuffer(FramebufferTarget.Framebuffer, FboId);
    }

    public static void Unbind()
    {
        GLManager.GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
    }

    public void Resize(int width, int height)
    {
        if (Width == width && Height == height) return;

        Dispose();
        CreateFramebuffer(width, height);
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        if (FboId != 0)
        {
            GLManager.GL.DeleteFramebuffer(FboId);
            FboId = 0;
        }
        if (TextureId != 0)
        {
            GLManager.GL.DeleteTexture(TextureId);
            TextureId = 0;
        }
        if (RenderBufferId != 0)
        {
            GLManager.GL.DeleteRenderbuffer(RenderBufferId);
            RenderBufferId = 0;
        }
    }
}
