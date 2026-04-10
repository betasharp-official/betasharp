using BetaSharp.Client.Options;
using BetaSharp.Client.Rendering.Core;
using BetaSharp.Client.Rendering.Core.OpenGL;
using BetaSharp.Client.Rendering.Core.Textures;
using BetaSharp.Entities;
using BetaSharp.Worlds.Maps;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Color = BetaSharp.Client.Guis.Color;

namespace BetaSharp.Client.Rendering;

public class MapItemRenderer
{
    private readonly int[] colors = new int[128 * 128];
    private readonly TextureHandle _textureId;
    private readonly GameOptions _options;
    private readonly ITextRenderer _textRenderer;

    public MapItemRenderer(ITextRenderer textRenderer, GameOptions options, ITextureManager textureManager)
    {
        _options = options;
        _textRenderer = textRenderer;
        _textureId = textureManager.Load(new Image<Rgba32>(128, 128));

        for (int i = 0; i < 128 * 128; ++i)
        {
            colors[i] = 0;
        }

    }

    public void render(EntityPlayer player, ITextureManager textureManager, MapState mapState)
    {
        for (int i = 0; i < 128 * 128; ++i)
        {
            byte color = mapState.Colors[i];
            if (color / 4 == 0)
            {
                // render translucent checkerboard pattern for transparent pixels
                colors[i] = (i + i / 128 & 1) * 8 + 16 << 24;
            }
            else
            {
                uint colorValue = MapColor.ById(color / 4).ColorValue;
                int lowest2Bits = color & 3;
                byte brightness = lowest2Bits switch
                {
                    0 => 180,
                    1 => 220,
                    _ => 255,
                };

                uint r = (colorValue >> 16 & 255) * brightness / 255;
                uint g = (colorValue >> 8 & 255) * brightness / 255;
                uint b = (colorValue & 255) * brightness / 255;

                colors[i] = unchecked((int)(0xFF000000u | r << 16 | g << 8 | b));
            }
        }

        if (_textureId.Texture != null) textureManager.Bind(colors, 128, 128, _textureId.Texture);
        Tessellator tess = Tessellator.instance;
        _textureId.Bind();
        GLManager.GL.Enable(GLEnum.Blend);
        GLManager.GL.Disable(GLEnum.AlphaTest);
        tess.startDrawingQuads();
        tess.addVertexWithUV(0, 128, -0.01F, 0.0D, 1.0D);
        tess.addVertexWithUV(128, 128, -0.01F, 1.0D, 1.0D);
        tess.addVertexWithUV(128, 0, -0.01F, 1.0D, 0.0D);
        tess.addVertexWithUV(0, 0, -0.01F, 0.0D, 0.0D);
        tess.draw();
        GLManager.GL.Enable(GLEnum.AlphaTest);
        GLManager.GL.Disable(GLEnum.Blend);
        textureManager.BindTexture(textureManager.GetTextureId("/misc/mapicons.png"));
        foreach (var icon in mapState.Icons)
        {
            GLManager.GL.PushMatrix();
            GLManager.GL.Translate((sbyte)icon.X / 2.0F + 64.0F, (sbyte)icon.Z / 2.0F + 64.0F, -0.02F);
            GLManager.GL.Rotate((sbyte)icon.Rotation * 360 / 16.0F, 0.0F, 0.0F, 1.0F);
            GLManager.GL.Scale(4.0F, 4.0F, 3.0F);
            GLManager.GL.Translate(-(2.0F / 16.0F), 2.0F / 16.0F, 0.0F);
            float uMin = (icon.Type % 4 + 0) / 4.0F;
            float vMin = (icon.Type / 4 + 0) / 4.0F;
            float uMax = (icon.Type % 4 + 1) / 4.0F;
            float vMax = (icon.Type / 4 + 1) / 4.0F;
            tess.startDrawingQuads();
            tess.addVertexWithUV(-1, 1, 0, uMin, vMin);
            tess.addVertexWithUV(1, 1, 0, uMax, vMin);
            tess.addVertexWithUV(1, -1, 0, uMax, vMax);
            tess.addVertexWithUV(-1, -1, 0, uMin, vMax);
            tess.draw();
            GLManager.GL.PopMatrix();
        }

        GLManager.GL.PushMatrix();
        GLManager.GL.Translate(0.0F, 0.0F, -0.04F);
        GLManager.GL.Scale(1.0F, 1.0F, 1.0F);
        _textRenderer.DrawString(mapState.Id, 0, 0, Color.White);
        GLManager.GL.PopMatrix();
    }
}
