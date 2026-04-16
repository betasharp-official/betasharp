using BetaSharp.Client.Guis;
using BetaSharp.Client.Rendering.Core;
using BetaSharp.Client.Rendering.Core.Textures;
using Silk.NET.OpenGL;
using GLEnum = BetaSharp.Client.Rendering.Core.OpenGL.GLEnum;

namespace BetaSharp.Client.Rendering;

public class LoadingScreenRenderer(BetaSharp game) : ILoadingScreenRenderer
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

            if (!game.SupportsLegacyOpenGlRenderPath)
            {
                return;
            }

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

        if (!game.SupportsLegacyOpenGlRenderPath)
        {
            game.UpdateWindow(true);
            Thread.Yield();
            return;
        }

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
        TextureHandle backgroundHandle = game.TextureManager.GetTextureId("/gui/background.png");
        game.TextureManager.BindTexture(backgroundHandle);

        float tileSize = 32.0f;
        DrawTexturedQuad tess = new(tessellator);
        tess.SetTint(new Color(64, 64, 64, 255));

        for (float py = 0; py < height; py += tileSize)
        {
            float h = MathF.Min(tileSize, height - py);

            for (float px = 0; px < width; px += tileSize)
            {
                float w = MathF.Min(tileSize, width - px);
                tess.Draw(px, py, px + w, py + h, 0.0f, 0.0, 0.0, w / tileSize, h / tileSize);
            }
        }

        tess.Draw();

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

        int titleX = (width - game.TextRenderer.GetStringWidth(_titleText)) / 2;
        int titleY = height / 2 - 4 - 16;
        game.TextRenderer.DrawStringWithShadow(_titleText, titleX, titleY, Color.White);

        int stageX = (width - game.TextRenderer.GetStringWidth(_currentStage)) / 2;
        int stageY = height / 2 - 4 + 8;
        game.TextRenderer.DrawStringWithShadow(_currentStage, stageX, stageY, Color.White);

        game.UpdateWindow(true);
        Thread.Yield();
    }

    private class DrawTexturedQuad(Tessellator tessellator)
    {
        private Color _tint = new(64, 64, 64, 255);

        public void SetTint(Color tint)
        {
            _tint = tint;
        }

        public void Draw(float left, float top, float right, float bottom, float z, double uLeft, double vTop,
            double uRight, double vBottom)
        {
            tessellator.startDrawingQuads();
            tessellator.setColorRGBA(_tint);
            tessellator.addVertexWithUV(left, bottom, z, uLeft, vBottom);
            tessellator.addVertexWithUV(right, bottom, z, uRight, vBottom);
            tessellator.addVertexWithUV(right, top, z, uRight, vTop);
            tessellator.addVertexWithUV(left, top, z, uLeft, vTop);
        }

        public void Draw() => tessellator.draw();
    }
}
