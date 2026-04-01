using BetaSharp.Client.Guis;
using BetaSharp.Client.Rendering.Core;
using BetaSharp.Client.Rendering.Core.Textures;
using Silk.NET.OpenGL;
using GLEnum = BetaSharp.Client.Rendering.Core.OpenGL.GLEnum;

namespace BetaSharp.Client.Rendering;

public class LoadingScreenRenderer(BetaSharp game) : LoadingDisplay
{
    private string _currentStage = string.Empty;
    private string _titleText = string.Empty;
    private long _lastUpdateMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    private bool _ignoreShutdownCheck;

    public void BeginLoading(string message)
    {
        _ignoreShutdownCheck = false;
        UpdateLoadingTitle(message);
    }

    public void BeginLoadingPersistent(string message)
    {
        _ignoreShutdownCheck = true;
        UpdateLoadingTitle(_titleText);
    }

    public void UpdateLoadingTitle(string message)
    {
        if (!game.Running && !_ignoreShutdownCheck)
        {
            throw new BetaSharpShutdownException();
        }

        if (game.Running)
        {
            _titleText = message;
            ScaledResolution resolution = new(game.Options, game.DisplayWidth, game.DisplayHeight);

            GLManager.GL.Clear(ClearBufferMask.DepthBufferBit);
            GLManager.GL.MatrixMode(GLEnum.Projection);
            GLManager.GL.LoadIdentity();
            GLManager.GL.Ortho(0.0, resolution.ScaledWidth, resolution.ScaledHeight, 0.0, 100.0, 300.0);
            GLManager.GL.MatrixMode(GLEnum.Modelview);
            GLManager.GL.LoadIdentity();
            GLManager.GL.Translate(0.0f, 0.0f, -200.0f);
        }
    }

    public void SetStage(string message)
    {
        if (!game.Running && !_ignoreShutdownCheck)
        {
            throw new BetaSharpShutdownException();
        }

        if (game.Running)
        {
            _lastUpdateMs = 0L;
            _currentStage = message;
            SetProgress(-1);
            _lastUpdateMs = 0L;
        }
    }

    public void SetProgress(int progress)
    {
        if (!game.Running && !_ignoreShutdownCheck)
        {
            throw new BetaSharpShutdownException();
        }

        if (!game.Running) return;

        long currentTimeMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        if (currentTimeMs - _lastUpdateMs < 20L) return;

        _lastUpdateMs = currentTimeMs;
        ScaledResolution resolution = new(game.Options, game.DisplayWidth, game.DisplayHeight);
        int width = resolution.ScaledWidth;
        int height = resolution.ScaledHeight;

        GLManager.GL.Clear(ClearBufferMask.DepthBufferBit);
        GLManager.GL.MatrixMode(GLEnum.Projection);
        GLManager.GL.LoadIdentity();
        GLManager.GL.Ortho(0.0, width, height, 0.0, 100.0, 300.0);
        GLManager.GL.MatrixMode(GLEnum.Modelview);
        GLManager.GL.LoadIdentity();
        GLManager.GL.Translate(0.0f, 0.0f, -200.0f);
        GLManager.GL.Clear(ClearBufferMask.DepthBufferBit | ClearBufferMask.ColorBufferBit);

        Tessellator tessellator = Tessellator.instance;
        GLManager.GL.Disable(GLEnum.Texture2D);
        tessellator.startDrawingQuads();
        tessellator.setColorOpaque_I(0x0000AA);
        tessellator.addVertex(0.0, height, 0.0);
        tessellator.addVertex(width, height, 0.0);
        tessellator.addVertex(width, 0.0, 0.0);
        tessellator.addVertex(0.0, 0.0, 0.0);
        tessellator.draw();
        GLManager.GL.Enable(GLEnum.Texture2D);

        if (progress >= 0)
        {
            const int progressBarWidth = 100;
            const int progressBarHeight = 2;
            int x = width / 2 - progressBarWidth / 2;
            int y = height / 2 + 16;

            GLManager.GL.Disable(GLEnum.Texture2D);
            tessellator.startDrawingQuads();
            tessellator.setColorOpaque_I(0x808080);
            tessellator.addVertex(x, y, 0.0);
            tessellator.addVertex(x, y + progressBarHeight, 0.0);
            tessellator.addVertex(x + progressBarWidth, y + progressBarHeight, 0.0);
            tessellator.addVertex(x + progressBarWidth, y, 0.0);

            tessellator.setColorOpaque_I(0x80FF80);
            tessellator.addVertex(x, y, 0.0);
            tessellator.addVertex(x, y + progressBarHeight, 0.0);
            tessellator.addVertex(x + progress, y + progressBarHeight, 0.0);
            tessellator.addVertex(x + progress, y, 0.0);
            tessellator.draw();
            GLManager.GL.Enable(GLEnum.Texture2D);
        }

        int bsodX = 50;
        int bsodY = 50;
        game.TextRenderer.DrawStringWithShadow("A fatal exception 0E has occurred at 028:C0011E36 in VXD VMM(01) + 00010E36.", bsodX, bsodY, Color.White);
        game.TextRenderer.DrawStringWithShadow("The current application will be terminated.", bsodX, bsodY + 16, Color.White);
        game.TextRenderer.DrawStringWithShadow("* Press any key to terminate the current application.", bsodX, bsodY + 48, Color.White);
        game.TextRenderer.DrawStringWithShadow("* Press CTRL+ALT+DEL again to restart your computer.", bsodX, bsodY + 64, Color.White);
        game.TextRenderer.DrawStringWithShadow("Press any key to continue _", bsodX, bsodY + 96, Color.White);

        game.TextRenderer.DrawStringWithShadow("Background Task: " + _titleText, bsodX, height - 70, Color.White);
        game.TextRenderer.DrawStringWithShadow("Current Stage: " + _currentStage, bsodX, height - 50, Color.White);

        Display.update();
        Thread.Yield();
    }
}
