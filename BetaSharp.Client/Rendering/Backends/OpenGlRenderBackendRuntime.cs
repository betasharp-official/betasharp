using BetaSharp.Client.Diagnostics.GuiBackends;
using BetaSharp.Client.Diagnostics;
using BetaSharp.Client.Options;
using BetaSharp.Client.Rendering.Core;
using BetaSharp.Client.Rendering.Core.OpenGL;
using BetaSharp.Client.Rendering.Core.Textures;
using BetaSharp.Client.Rendering.Presentation;
using Microsoft.Extensions.Logging;
using Silk.NET.OpenGL;
using LegacyGLEnum = BetaSharp.Client.Rendering.Core.OpenGL.GLEnum;

namespace BetaSharp.Client.Rendering.Backends;

internal sealed class OpenGlRenderBackendRuntime : IRenderBackendRuntime
{
    public RendererBackendKind Kind => RendererBackendKind.OpenGL;
    public RendererBackendCapabilities Capabilities => RendererBackendCapabilities.For(Kind);

    public void InitializeGraphicsContext(DebugTelemetry telemetry)
    {
        GL silkGl = Display.getGL()!;
        (float r, float g, float b) = Display.GetInitialBackgroundColor();
        silkGl.ClearColor(r, g, b, 1.0f);
        silkGl.Enable(EnableCap.Multisample);

        GLManager.Init(silkGl);
        if (GLManager.GL is LegacyGL legacyGl)
        {
            telemetry.CaptureSystemInfo(legacyGl);
        }
        else
        {
            telemetry.CaptureSystemInfo(null);
        }
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

    public void UpdateDynamicTextures(TextureManager textureManager, bool isGamePaused)
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

    public void RenderStartupScreen(
        GameOptions options,
        int displayWidth,
        int displayHeight,
        int framebufferWidth,
        int framebufferHeight,
        TextureHandle logoTexture)
    {
        ScaledResolution scaledResolution = new(options, displayWidth, displayHeight);
        GLManager.GL.Clear(ClearBufferMask.DepthBufferBit | ClearBufferMask.ColorBufferBit);
        GLManager.GL.MatrixMode(LegacyGLEnum.Projection);
        GLManager.GL.LoadIdentity();
        GLManager.GL.Ortho(0.0D, scaledResolution.ScaledWidth, scaledResolution.ScaledHeight, 0.0D, 1000.0D, 3000.0D);
        GLManager.GL.MatrixMode(LegacyGLEnum.Modelview);
        GLManager.GL.LoadIdentity();
        GLManager.GL.Translate(0.0F, 0.0F, -2000.0F);
        GLManager.GL.Viewport(0, 0, (uint)framebufferWidth, (uint)framebufferHeight);
        GLManager.GL.ClearColor(0.0F, 0.0F, 0.0F, 0.0F);
        Tessellator tessellator = Tessellator.instance;
        GLManager.GL.Disable(LegacyGLEnum.Lighting);
        GLManager.GL.Enable(LegacyGLEnum.Texture2D);
        GLManager.GL.Disable(LegacyGLEnum.Fog);
        GLManager.GL.Color4(1.0F, 1.0F, 1.0F, 1.0F);
        logoTexture.Bind();
        tessellator.startDrawingQuads();
        tessellator.setColorOpaque_I(0xFFFFFF);
        tessellator.addVertexWithUV(0.0D, displayHeight, 0.0D, 0.0D, 0.0D);
        tessellator.addVertexWithUV(displayWidth, displayHeight, 0.0D, 0.0D, 0.0D);
        tessellator.addVertexWithUV(displayWidth, 0.0D, 0.0D, 0.0D, 0.0D);
        tessellator.addVertexWithUV(0.0D, 0.0D, 0.0D, 0.0D, 0.0D);
        tessellator.draw();
        const int logoWidth = 256;
        const int logoHeight = 256;
        GLManager.GL.Color4(1.0F, 1.0F, 1.0F, 1.0F);
        tessellator.setColorOpaque_I(0xFFFFFF);
        DrawTextureRegion(
            (scaledResolution.ScaledWidth - logoWidth) / 2,
            (scaledResolution.ScaledHeight - logoHeight) / 2,
            0,
            0,
            logoWidth,
            logoHeight);
        GLManager.GL.Disable(LegacyGLEnum.Lighting);
        GLManager.GL.Disable(LegacyGLEnum.Fog);
        GLManager.GL.Enable(LegacyGLEnum.AlphaTest);
        GLManager.GL.AlphaFunc(LegacyGLEnum.Greater, 0.1F);
        UpdateWindow(true);
    }

    public void CleanupRenderResources()
    {
        GLAllocation.deleteTexturesAndDisplayLists();
    }

    public void LogRenderResourceReport()
    {
        GLTexture.LogLeakReport();
    }

    public ILoadingScreenRenderer CreateLoadingScreenRenderer(BetaSharp client)
    {
        return new LoadingScreenRenderer(client);
    }

    public ISceneRenderer CreateSceneRenderer(BetaSharp client)
    {
        return new GameRenderer(client);
    }

    public IWorldRenderer CreateWorldRenderer(BetaSharp client, TextureManager textureManager)
    {
        return new WorldRenderer(client, textureManager);
    }

    public IRenderPresentation CreatePresentation(int width, int height, GameOptions options)
    {
        return RenderPresentationFactory.Create(Kind, width, height, options);
    }

    public IImGuiRendererBackend CreateImGuiRendererBackend()
    {
        return ImGuiRendererBackendFactory.Create(Kind);
    }

    private static void DrawTextureRegion(int x, int y, int texX, int texY, int width, int height)
    {
        const float uScale = 1 / 256f;
        const float vScale = 1 / 256f;

        Tessellator tessellator = Tessellator.instance;
        tessellator.startDrawingQuads();
        tessellator.addVertexWithUV(x + 0, y + height, 0, (texX + 0) * uScale, (texY + height) * vScale);
        tessellator.addVertexWithUV(x + width, y + height, 0, (texX + width) * uScale, (texY + height) * vScale);
        tessellator.addVertexWithUV(x + width, y + 0, 0, (texX + width) * uScale, (texY + 0) * vScale);
        tessellator.addVertexWithUV(x + 0, y + 0, 0, (texX + 0) * uScale, (texY + 0) * vScale);
        tessellator.draw();
    }
}
