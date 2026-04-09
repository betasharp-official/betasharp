using BetaSharp.Client.Diagnostics.GuiBackends;
using BetaSharp.Client.Diagnostics;
using BetaSharp.Client.Options;
using BetaSharp.Client.Rendering.Core;
using BetaSharp.Client.Rendering.Core.OpenGL;
using BetaSharp.Client.Rendering.Presentation;
using Microsoft.Extensions.Logging;

namespace BetaSharp.Client.Rendering.Backends;

internal sealed class OpenGlRenderBackendRuntime : IRenderBackendRuntime
{
    public RendererBackendKind Kind => RendererBackendKind.OpenGL;

    public void InitializeGraphicsContext(DebugTelemetry telemetry)
    {
        GLManager.Init(Display.getGL()!);
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
            GLManager.GL.GetFloat(GLEnum.MaxTextureMaxAnisotropy, out float maxAnisotropy);
            GameOptions.MaxAnisotropy = maxAnisotropy;
            logger.LogInformation("Max Anisotropy: {MaxAnisotropy}", maxAnisotropy);
        }
        else
        {
            GameOptions.MaxAnisotropy = 1.0f;
        }

        GLManager.GL.Enable(GLEnum.Texture2D);
        GLManager.GL.ShadeModel(GLEnum.Smooth);
        GLManager.GL.ClearDepth(1.0D);
        GLManager.GL.Enable(GLEnum.DepthTest);
        GLManager.GL.DepthFunc(GLEnum.Lequal);
        GLManager.GL.Enable(GLEnum.AlphaTest);
        GLManager.GL.AlphaFunc(GLEnum.Greater, 0.1F);
        GLManager.GL.CullFace(GLEnum.Back);
        GLManager.GL.MatrixMode(GLEnum.Projection);
        GLManager.GL.LoadIdentity();
        GLManager.GL.MatrixMode(GLEnum.Modelview);
    }

    public void SetMainViewport(int width, int height)
    {
        GLManager.GL.Viewport(0, 0, (uint)width, (uint)height);
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
