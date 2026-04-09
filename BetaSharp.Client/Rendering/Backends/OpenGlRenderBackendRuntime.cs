using BetaSharp.Client.Diagnostics.GuiBackends;
using BetaSharp.Client.Diagnostics;
using BetaSharp.Client.Options;
using BetaSharp.Client.Rendering.Core;
using BetaSharp.Client.Rendering.Core.OpenGL;
using BetaSharp.Client.Rendering.Presentation;
using Microsoft.Extensions.Logging;
using Silk.NET.OpenGL;
using LegacyGLEnum = BetaSharp.Client.Rendering.Core.OpenGL.GLEnum;

namespace BetaSharp.Client.Rendering.Backends;

internal sealed class OpenGlRenderBackendRuntime : IRenderBackendRuntime
{
    public RendererBackendKind Kind => RendererBackendKind.OpenGL;

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

    public void UpdateWindow(bool processMessages)
    {
        Display.update(processMessages);
    }

    public IRenderPresentation CreatePresentation(int width, int height, GameOptions options)
    {
        return RenderPresentationFactory.Create(Kind, width, height, options);
    }

    public IImGuiRendererBackend CreateImGuiRendererBackend()
    {
        return ImGuiRendererBackendFactory.Create(Kind);
    }
}
