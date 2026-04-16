using BetaSharp.Client.Diagnostics;
using BetaSharp.Client.Options;
using BetaSharp.Client.Rendering.Core;
using BetaSharp.Client.Rendering.Core.OpenGL;
using BetaSharp.Client.Rendering.Core.Textures;
using Microsoft.Extensions.Logging;
using Silk.NET.OpenGL;
using LegacyGLEnum = BetaSharp.Client.Rendering.Core.OpenGL.GLEnum;

namespace BetaSharp.Client.Rendering.Backends.OpenGL;

internal sealed class OpenGlRenderBackendBootstrap : IRenderBackendBootstrap
{
    public RendererBackendKind Kind => RendererBackendKind.OpenGL;
    public RendererBackendCapabilities Capabilities => RendererBackendCapabilities.For(Kind);
    public IRendererFactory RendererFactory => OpenGlRendererFactory.Instance;

    public void InitializeGraphicsContext(DebugTelemetry telemetry)
    {
        GL silkGl = GL.GetApi(Display.getWindow());
        (float r, float g, float b) = Display.GetInitialBackgroundColor();
        silkGl.ClearColor(r, g, b, 1.0f);
        silkGl.Enable(EnableCap.Multisample);
        GLManager.Init(silkGl);

        LegacyGL? legacyGl = GLManager.GL as LegacyGL;
        telemetry.CaptureSystemInfo(ReadGraphicsApiSnapshot(legacyGl));
    }

    public void ConfigureDefaultRenderState(GameOptions options, ILogger logger)
    {
        bool anisotropicFiltering = GLManager.GL.IsExtensionPresent("GL_EXT_texture_filter_anisotropic");
        logger.LogInformation("Anisotropic Filtering Supported: {AnisotropicFiltering}", anisotropicFiltering);

        if (anisotropicFiltering)
        {
            GLManager.GL.GetFloat(LegacyGLEnum.MaxTextureMaxAnisotropy, out float maxAnisotropy);
            GameOptions.MaxAnisotropy = maxAnisotropy;
            logger.LogInformation("Max Anisotropy: {MaxAnisotropy}", maxAnisotropy);
        }
        else
        {
            GameOptions.MaxAnisotropy = 1.0f;
        }

        GLManager.GL.Enable(LegacyGLEnum.Texture2D);
        GLManager.GL.ShadeModel(LegacyGLEnum.Smooth);
        GLManager.GL.ClearDepth(1.0D);
        GLManager.GL.Enable(LegacyGLEnum.DepthTest);
        GLManager.GL.DepthFunc(LegacyGLEnum.Lequal);
        GLManager.GL.Enable(LegacyGLEnum.AlphaTest);
        GLManager.GL.AlphaFunc(LegacyGLEnum.Greater, 0.1F);
        GLManager.GL.CullFace(LegacyGLEnum.Back);
        GLManager.GL.MatrixMode(LegacyGLEnum.Projection);
        GLManager.GL.LoadIdentity();
        GLManager.GL.MatrixMode(LegacyGLEnum.Modelview);
    }

    public void ConfigurePresentationMode(GameOptions options)
    {
        SetVSyncEnabled(options.VSync);
    }

    public void SetVSyncEnabled(bool enabled)
    {
        Display.getGlfw().SwapInterval(enabled ? 1 : 0);
    }

    public void SetMainViewport(int width, int height)
    {
        GLManager.GL.Viewport(0, 0, (uint)width, (uint)height);
    }

    public void PrepareFrameRenderState()
    {
        GLManager.GL.Enable(LegacyGLEnum.Texture2D);
    }

    public void UpdateDynamicTextures(ITextureManager textureManager, bool isGamePaused)
    {
        textureManager.BindTexture(textureManager.GetTextureId("/terrain.png"));
        if (!isGamePaused)
        {
            textureManager.Tick();
        }
    }

    public void CheckBackendErrors(string location, ILogger logger)
    {
        LegacyGLEnum glError = GLManager.GL.GetError();
        if (glError != 0)
        {
            logger.LogError("#### GL ERROR ####");
            logger.LogError("@ {Location}", location);
            logger.LogError("> {GlError}", glError);
        }
    }

    public void UpdateWindow(bool processMessages)
    {
        Display.update(processMessages);
    }

    public bool TryCaptureScreenshot(int framebufferWidth, int framebufferHeight, out byte[] rgbPixels)
    {
        rgbPixels = new byte[framebufferWidth * framebufferHeight * 3];
        GLManager.GL.PixelStore(PixelStoreParameter.PackAlignment, 1);

        unsafe
        {
            fixed (byte* pixels = rgbPixels)
            {
                GLManager.GL.ReadPixels(
                    0,
                    0,
                    (uint)framebufferWidth,
                    (uint)framebufferHeight,
                    PixelFormat.Rgb,
                    PixelType.UnsignedByte,
                    pixels);
            }
        }

        return true;
    }

    public void CleanupRenderResources()
    {
        GLAllocation.deleteTexturesAndDisplayLists();
    }

    public void LogRenderResourceReport()
    {
        GLTexture.LogLeakReport();
    }

    public void RenderStartupScreen(GameOptions options, int displayWidth, int displayHeight, int framebufferWidth, int framebufferHeight, int splashTextureId)
    {
        GLManager.GL.Viewport(0, 0, (uint)framebufferWidth, (uint)framebufferHeight);

        GLManager.GL.ClearColor(1f, 1f, 1f, 1f);
        GLManager.GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        GLManager.GL.MatrixMode(LegacyGLEnum.Projection);
        GLManager.GL.LoadIdentity();
        GLManager.GL.Ortho(0.0, displayWidth, displayHeight, 0.0, -1.0, 1.0);

        GLManager.GL.MatrixMode(LegacyGLEnum.Modelview);
        GLManager.GL.LoadIdentity();

        GLManager.GL.Disable(LegacyGLEnum.DepthTest);
        GLManager.GL.Disable(LegacyGLEnum.CullFace);
        GLManager.GL.Disable(LegacyGLEnum.Lighting);
        GLManager.GL.Enable(LegacyGLEnum.Texture2D);
        GLManager.GL.Enable(LegacyGLEnum.Blend);
        GLManager.GL.BlendFunc(LegacyGLEnum.SrcAlpha, LegacyGLEnum.OneMinusSrcAlpha);
        GLManager.GL.Color4(1f, 1f, 1f, 1f);

        GLManager.GL.BindTexture(LegacyGLEnum.Texture2D, (uint)splashTextureId);

        const float logoWidth = 256f;
        const float logoHeight = 256f;

        float x = (displayWidth - logoWidth) * 0.5f;
        float y = (displayHeight - logoHeight) * 0.5f;

        Tessellator tess = Tessellator.instance;
        tess.startDrawingQuads();
        tess.setColorOpaque_F(1f, 1f, 1f);
        tess.addVertexWithUV(x,             y + logoHeight, 0.0, 0.0, 1.0);
        tess.addVertexWithUV(x + logoWidth, y + logoHeight, 0.0, 1.0, 1.0);
        tess.addVertexWithUV(x + logoWidth, y,              0.0, 1.0, 0.0);
        tess.addVertexWithUV(x,             y,              0.0, 0.0, 0.0);
        tess.draw();

        GLManager.GL.Disable(LegacyGLEnum.Blend);
    }

    private static DebugGraphicsApiSnapshot ReadGraphicsApiSnapshot(LegacyGL? gl)
    {
        if (gl == null)
        {
            return DebugGraphicsApiSnapshot.Empty;
        }

        string apiVersion = TryGetGlString(gl, StringName.Version);
        return new DebugGraphicsApiSnapshot(
            GpuName: TryGetGlString(gl, StringName.Renderer),
            GpuVram: TryGetGpuVram(gl),
            ApiVersion: apiVersion,
            ShaderLanguageVersion: TryGetGlString(gl, StringName.ShadingLanguageVersion),
            DriverVersion: DebugTelemetry.UnknownValue);
    }

    private static string TryGetGlString(LegacyGL gl, StringName name)
    {
        try
        {
            string? value = gl.SilkGL.GetStringS(name);
            return string.IsNullOrWhiteSpace(value) ? DebugTelemetry.UnknownValue : value.Trim();
        }
        catch
        {
            return DebugTelemetry.UnknownValue;
        }
    }

    private static string TryGetGpuVram(LegacyGL gl)
    {
        try
        {
            if (!gl.IsExtensionPresent("GL_NVX_gpu_memory_info"))
            {
                return DebugTelemetry.UnknownValue;
            }

            int dedicatedVidMemKb = gl.SilkGL.GetInteger((Silk.NET.OpenGL.GLEnum)0x9047);
            if (dedicatedVidMemKb > 0)
            {
                return FormatMemoryKilobytes(dedicatedVidMemKb);
            }

            int totalAvailableKb = gl.SilkGL.GetInteger((Silk.NET.OpenGL.GLEnum)0x9048);
            if (totalAvailableKb > 0)
            {
                return FormatMemoryKilobytes(totalAvailableKb);
            }
        }
        catch
        {
        }

        return DebugTelemetry.UnknownValue;
    }

    private static string FormatMemoryKilobytes(long kilobytes)
    {
        if (kilobytes <= 0)
        {
            return DebugTelemetry.UnknownValue;
        }

        double gib = kilobytes / 1024.0D / 1024.0D;
        if (gib >= 1.0D)
        {
            return $"{gib:0.##} GB";
        }

        double mib = kilobytes / 1024.0D;
        return $"{mib:0} MB";
    }
}
