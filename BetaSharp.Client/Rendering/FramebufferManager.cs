using BetaSharp.Client.Options;
using BetaSharp.Client.Rendering.Core;
using Silk.NET.OpenGL;
using Framebuffer = BetaSharp.Client.Rendering.Core.Framebuffer;
using GLEnum = BetaSharp.Client.Rendering.Core.OpenGL.GLEnum;
using Shader = BetaSharp.Client.Rendering.Core.Shader;

namespace BetaSharp.Client.Rendering;

public class FramebufferManager
{
    private readonly Framebuffer _mainFbo;
    private readonly Shader _gammaShader;
    private readonly Shader _blurShader;
    private readonly uint _vao;
    private readonly uint _vbo;
    private readonly GameOptions _options;

    private uint _cloudFboId;
    private uint _cloudTexId;
    private uint _cloudDepthRboId;
    private uint _pingPongFboId;
    private uint _pingPongTexId;

    public FramebufferManager(int w, int h, GameOptions options)
    {
        _options = options;
        _mainFbo = new Framebuffer(w, h);

        string quadVert = AssetManager.Instance.getAsset("shaders/quad.vert").GetTextContent();
        _gammaShader = new Shader(quadVert,AssetManager.Instance.getAsset("shaders/gamma.frag").GetTextContent());
        _blurShader = new Shader(quadVert,AssetManager.Instance.getAsset("shaders/blur.frag").GetTextContent());

        float[] quadVertices = [
            -1.0f,  1.0f,  0.0f, 1.0f,
            -1.0f, -1.0f,  0.0f, 0.0f,
             1.0f, -1.0f,  1.0f, 0.0f,

            -1.0f,  1.0f,  0.0f, 1.0f,
             1.0f, -1.0f,  1.0f, 0.0f,
             1.0f,  1.0f,  1.0f, 1.0f
        ];

        IGL gl = GLManager.GL;
        _vao = gl.GenVertexArray();
        _vbo = gl.GenBuffer();

        gl.BindVertexArray(_vao);
        gl.BindBuffer(GLEnum.ArrayBuffer, _vbo);

        unsafe
        {
            fixed (float* ptr = quadVertices)
            {
                gl.BufferData(GLEnum.ArrayBuffer, (nuint)(quadVertices.Length * sizeof(float)), ptr, GLEnum.StaticDraw);
            }
        }

        gl.EnableVertexAttribArray(0);
        unsafe { gl.VertexAttribPointer(0, 2, GLEnum.Float, false, 4 * sizeof(float), (void*)0); }

        gl.EnableVertexAttribArray(1);
        unsafe { gl.VertexAttribPointer(1, 2, GLEnum.Float, false, 4 * sizeof(float), (void*)(2 * sizeof(float))); }

        gl.BindVertexArray(0);

        CreateCloudFbos(w, h);
    }

    /// <summary>The OpenGL texture ID of the rendered frame. Valid after <see cref="End"/> is called.</summary>
    public uint TextureId => _mainFbo.TextureId;

    public int FramebufferWidth => _mainFbo.Width;
    public int FramebufferHeight => _mainFbo.Height;

    /// <summary>
    /// When true, <see cref="End"/> clears the screen but skips blitting the FBO to it.
    /// The rendered frame is available via <see cref="TextureId"/> for ImGui display.
    /// </summary>
    public bool SkipBlit { get; set; }

    public void Begin()
    {
        _mainFbo.Bind();
        GLManager.GL.Viewport(0, 0, (uint)_mainFbo.Width, (uint)_mainFbo.Height);
        GLManager.GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
    }

    public void End()
    {
        Framebuffer.Unbind();

        IGL gl = GLManager.GL;
        gl.Viewport(0, 0, (uint)Display.getFramebufferWidth(), (uint)Display.getFramebufferHeight());

        gl.Disable(GLEnum.DepthTest);
        gl.Clear(ClearBufferMask.ColorBufferBit);

        gl.Disable(GLEnum.Blend);

        if (!SkipBlit)
        {
            // TODO: make indivdual post processing passes control their shaders.
            _gammaShader.Bind();

            float slider = _options.Gamma / 100.0f;
            float gammaValue = 0.25f + (slider * 1.5f);

            _gammaShader.SetUniform1("gamma", gammaValue);
            _gammaShader.SetUniform1("screenTexture", 0);

            gl.ActiveTexture(GLEnum.Texture0);
            gl.BindTexture(GLEnum.Texture2D, _mainFbo.TextureId);

            gl.BindVertexArray(_vao);
            gl.DrawArrays(GLEnum.Triangles, 0, 6);
            gl.BindVertexArray(0);
            gl.UseProgram(0);
        }

        gl.Enable(GLEnum.DepthTest);
    }

    /// <summary>Binds the cloud FBO for cloud rendering. Clouds rendered after this call are captured separately for blurring.</summary>
    public void BeginCloudPass()
    {
        IGL gl = GLManager.GL;

        // Copy scene depth into cloud FBO so clouds are occluded by terrain
        gl.BindFramebuffer(FramebufferTarget.ReadFramebuffer, _mainFbo.FboId);
        gl.BindFramebuffer(FramebufferTarget.DrawFramebuffer, _cloudFboId);
        gl.BlitFramebuffer(0, 0, _mainFbo.Width, _mainFbo.Height, 0, 0, _mainFbo.Width, _mainFbo.Height,
            (uint)ClearBufferMask.DepthBufferBit, BlitFramebufferFilter.Nearest);

        gl.BindFramebuffer(FramebufferTarget.Framebuffer, _cloudFboId);
        gl.Viewport(0, 0, (uint)_mainFbo.Width, (uint)_mainFbo.Height);
        gl.Clear(ClearBufferMask.ColorBufferBit);
        gl.Enable(GLEnum.DepthTest);
        gl.Disable(GLEnum.Blend);
    }

    /// <summary>Applies a separable Gaussian blur to the captured cloud layer and composites it over the main FBO.</summary>
    public void EndCloudPass()
    {
        IGL gl = GLManager.GL;

        // Horizontal blur: cloud FBO
        gl.BindFramebuffer(FramebufferTarget.Framebuffer, _pingPongFboId);
        gl.Viewport(0, 0, (uint)_mainFbo.Width, (uint)_mainFbo.Height);
        _blurShader.Bind();
        _blurShader.SetUniform1("u_Texture", 0);
        _blurShader.SetUniform1("u_Horizontal", 1);
        gl.ActiveTexture(GLEnum.Texture0);
        gl.BindTexture(GLEnum.Texture2D, _cloudTexId);
        gl.BindVertexArray(_vao);
        gl.DrawArrays(GLEnum.Triangles, 0, 6);

        // Vertical blur -> main FBO with premultiplied blend (soft glow)
        _mainFbo.Bind();
        gl.Viewport(0, 0, (uint)_mainFbo.Width, (uint)_mainFbo.Height);
        _blurShader.SetUniform1("u_Horizontal", 0);
        //gl.Enable(GLEnum.Blend);
        gl.BlendFunc(GLEnum.SrcAlpha, GLEnum.OneMinusSrcAlpha);
        gl.DrawArrays(GLEnum.Triangles, 0, 6);

        gl.BindVertexArray(0);
        gl.Disable(GLEnum.Blend);
        gl.UseProgram(0);
        gl.Enable(GLEnum.DepthTest);
    }

    public void Resize(int width, int height)
    {
        if (width > 0 && height > 0)
        {
            _mainFbo.Resize(width, height);
            DestroyCloudFbos();
            CreateCloudFbos(width, height);
        }
    }

    private void CreateCloudFbos(int w, int h)
    {
        IGL gl = GLManager.GL;
        (_cloudFboId, _cloudTexId, _cloudDepthRboId) = CreateCloudCaptureFbo(gl, w, h);
        (_pingPongFboId, _pingPongTexId) = CreateColorOnlyFbo(gl, w, h);
    }

    private void DestroyCloudFbos()
    {
        IGL gl = GLManager.GL;
        if (_cloudFboId != 0) { gl.DeleteFramebuffer(_cloudFboId); _cloudFboId = 0; }
        if (_cloudTexId != 0) { gl.DeleteTexture(_cloudTexId); _cloudTexId = 0; }
        if (_cloudDepthRboId != 0) { gl.DeleteRenderbuffer(_cloudDepthRboId); _cloudDepthRboId = 0; }
        if (_pingPongFboId != 0) { gl.DeleteFramebuffer(_pingPongFboId); _pingPongFboId = 0; }
        if (_pingPongTexId != 0) { gl.DeleteTexture(_pingPongTexId); _pingPongTexId = 0; }
    }

    private static (uint fboId, uint texId, uint depthRboId) CreateCloudCaptureFbo(IGL gl, int w, int h)
    {
        uint fbo = gl.GenFramebuffer();
        gl.BindFramebuffer(FramebufferTarget.Framebuffer, fbo);

        uint tex = gl.GenTexture();
        gl.BindTexture(GLEnum.Texture2D, tex);
        unsafe
        {
            gl.TexImage2D(GLEnum.Texture2D, 0, (int)InternalFormat.Rgba8, (uint)w, (uint)h, 0, GLEnum.Rgba, GLEnum.UnsignedByte, null);
        }
        gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureMinFilter, (int)TextureMinFilter.Linear);
        gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureMagFilter, (int)TextureMagFilter.Linear);
        gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
        gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
        gl.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, tex, 0);

        uint[] rboArr = new uint[1];
        gl.GenRenderbuffers(rboArr);
        uint depthRbo = rboArr[0];
        gl.BindRenderbuffer(RenderbufferTarget.Renderbuffer, depthRbo);
        gl.RenderbufferStorage(RenderbufferTarget.Renderbuffer, InternalFormat.Depth24Stencil8, (uint)w, (uint)h);
        gl.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthStencilAttachment, RenderbufferTarget.Renderbuffer, depthRbo);

        gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        return (fbo, tex, depthRbo);
    }

    private static (uint fboId, uint texId) CreateColorOnlyFbo(IGL gl, int w, int h)
    {
        uint fbo = gl.GenFramebuffer();
        gl.BindFramebuffer(FramebufferTarget.Framebuffer, fbo);

        uint tex = gl.GenTexture();
        gl.BindTexture(GLEnum.Texture2D, tex);
        unsafe
        {
            gl.TexImage2D(GLEnum.Texture2D, 0, (int)InternalFormat.Rgba8, (uint)w, (uint)h, 0, GLEnum.Rgba, GLEnum.UnsignedByte, null);
        }
        gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureMinFilter, (int)TextureMinFilter.Linear);
        gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureMagFilter, (int)TextureMagFilter.Linear);
        gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
        gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureWrapT, (int)TextureWrapMode.ClampToEdge);

        gl.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, tex, 0);

        gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        return (fbo, tex);
    }
}
