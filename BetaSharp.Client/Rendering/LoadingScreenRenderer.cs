using BetaSharp.Client.Guis;
using BetaSharp.Client.Rendering.Core;
using Silk.NET.OpenGL.Legacy;

namespace BetaSharp.Client.Rendering;

public class LoadingScreenRenderer : LoadingDisplay
{

    private string field_1004_a = "";
    private readonly BetaSharp _game;
    private string field_1007_c = "";
    private long field_1006_d = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
;
    private bool field_1005_e;

    public LoadingScreenRenderer(BetaSharp var1)
    {
        _game = var1;
    }

    public void printText(string var1)
    {
        field_1005_e = false;
        func_597_c(var1);
    }

    public void progressStartNoAbort(string var1)
    {
        field_1005_e = true;
        func_597_c(field_1007_c);
    }

    public void func_597_c(string var1)
    {
        if (!_game.running)
        {
            if (!field_1005_e)
            {
                throw new BetaSharpShutdownException();
            }
        }
        else
        {
            field_1007_c = var1;
            ScaledResolution var2 = new(_game.options, _game.displayWidth, _game.displayHeight);
            GLManager.GL.Clear(ClearBufferMask.DepthBufferBit);
            GLManager.GL.MatrixMode(GLEnum.Projection);
            GLManager.GL.LoadIdentity();
            GLManager.GL.Ortho(0.0D, var2.ScaledWidthDouble, var2.ScaledHeightDouble, 0.0D, 100.0D, 300.0D);
            GLManager.GL.MatrixMode(GLEnum.Modelview);
            GLManager.GL.LoadIdentity();
            GLManager.GL.Translate(0.0F, 0.0F, -200.0F);
        }
    }

    public void progressStage(string var1)
    {
        if (!_game.running)
        {
            if (!field_1005_e)
            {
                throw new BetaSharpShutdownException();
            }
        }
        else
        {
            field_1006_d = 0L;
            field_1004_a = var1;
            setLoadingProgress(-1);
            field_1006_d = 0L;
        }
    }

    public void setLoadingProgress(int var1)
    {
        if (!_game.running)
        {
            if (!field_1005_e)
            {
                throw new BetaSharpShutdownException();
            }
        }
        else
        {
            long var2 = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
;
            if (var2 - field_1006_d >= 20L)
            {
                field_1006_d = var2;
                ScaledResolution var4 = new(_game.options, _game.displayWidth, _game.displayHeight);
                int var5 = var4.ScaledWidth;
                int var6 = var4.ScaledHeight;
                GLManager.GL.Clear(ClearBufferMask.DepthBufferBit);
                GLManager.GL.MatrixMode(GLEnum.Projection);
                GLManager.GL.LoadIdentity();
                GLManager.GL.Ortho(0.0D, var4.ScaledWidthDouble, var4.ScaledHeightDouble, 0.0D, 100.0D, 300.0D);
                GLManager.GL.MatrixMode(GLEnum.Modelview);
                GLManager.GL.LoadIdentity();
                GLManager.GL.Translate(0.0F, 0.0F, -200.0F);
                GLManager.GL.Clear(ClearBufferMask.DepthBufferBit | ClearBufferMask.ColorBufferBit);
                Tessellator var7 = Tessellator.instance;
                _game.textureManager.BindTexture(_game.textureManager.GetTextureId("/gui/background.png"));
                float var9 = 32.0F;
                var7.startDrawingQuads();
                var7.setColorOpaque_I(0x404040);
                var7.addVertexWithUV(0.0D, (double)var6, 0.0D, 0.0D, (double)((float)var6 / var9));
                var7.addVertexWithUV((double)var5, (double)var6, 0.0D, (double)((float)var5 / var9), (double)((float)var6 / var9));
                var7.addVertexWithUV((double)var5, 0.0D, 0.0D, (double)((float)var5 / var9), 0.0D);
                var7.addVertexWithUV(0.0D, 0.0D, 0.0D, 0.0D, 0.0D);
                var7.draw();
                if (var1 >= 0)
                {
                    byte var10 = 100;
                    byte var11 = 2;
                    int var12 = var5 / 2 - var10 / 2;
                    int var13 = var6 / 2 + 16;
                    GLManager.GL.Disable(GLEnum.Texture2D);
                    var7.startDrawingQuads();
                    var7.setColorOpaque_I(0x808080);
                    var7.addVertex((double)var12, (double)var13, 0.0D);
                    var7.addVertex((double)var12, (double)(var13 + var11), 0.0D);
                    var7.addVertex((double)(var12 + var10), (double)(var13 + var11), 0.0D);
                    var7.addVertex((double)(var12 + var10), (double)var13, 0.0D);
                    var7.setColorOpaque_I(0x80FF80);
                    var7.addVertex((double)var12, (double)var13, 0.0D);
                    var7.addVertex((double)var12, (double)(var13 + var11), 0.0D);
                    var7.addVertex((double)(var12 + var1), (double)(var13 + var11), 0.0D);
                    var7.addVertex((double)(var12 + var1), (double)var13, 0.0D);
                    var7.draw();
                    GLManager.GL.Enable(GLEnum.Texture2D);
                }

                _game.fontRenderer.DrawStringWithShadow(field_1007_c, (var5 - _game.fontRenderer.GetStringWidth(field_1007_c)) / 2, var6 / 2 - 4 - 16, Color.White);
                _game.fontRenderer.DrawStringWithShadow(field_1004_a, (var5 - _game.fontRenderer.GetStringWidth(field_1004_a)) / 2, var6 / 2 - 4 + 8, Color.White);
                Display.update();

                try
                {
                    java.lang.Thread.yield();
                }
                catch (Exception) { }

            }
        }
    }
}
