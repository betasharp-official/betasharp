using BetaSharp.Client.Rendering.Core;
using BetaSharp.Client.Rendering.Core.OpenGL;
using BetaSharp.Entities;
using BetaSharp.Util.Maths;

namespace BetaSharp.Client.Rendering.Entities;

public class LightningEntityRenderer : EntityRenderer
{

    public void render(EntityLightningBolt entity, double x, double y, double z, float yaw, float tickDelta)
    {
        Tessellator tessellator = Tessellator.instance;
        GLManager.GL.Disable(GLEnum.Texture2D);
        GLManager.GL.Disable(GLEnum.Lighting);
        GLManager.GL.Enable(GLEnum.Blend);
        GLManager.GL.BlendFunc(GLEnum.SrcAlpha, GLEnum.One);
        double[] xOffsets = new double[8];
        double[] zOffsets = new double[8];
        double xOffset = 0.0D;
        double zOffset = 0.0D;
        JavaRandom random = new(entity.renderSeed);

        for (int seg = 7; seg >= 0; --seg)
        {
            xOffsets[seg] = xOffset;
            zOffsets[seg] = zOffset;
            xOffset += random.NextInt(11) - 5;
            zOffset += random.NextInt(11) - 5;
        }

        for (int pass = 0; pass < 4; ++pass)
        {
            JavaRandom passRandom = new(entity.renderSeed);

            for (int branch = 0; branch < 3; ++branch)
            {
                int startSeg = 7;
                int endSeg = 0;
                if (branch > 0)
                {
                    startSeg = 7 - branch;
                }

                if (branch > 0)
                {
                    endSeg = startSeg - 2;
                }

                double xDiff = xOffsets[startSeg] - xOffset;
                double zDiff = zOffsets[startSeg] - zOffset;

                for (int step = startSeg; step >= endSeg; --step)
                {
                    double prevXDiff = xDiff;
                    double prevZDiff = zDiff;
                    if (branch == 0)
                    {
                        xDiff += passRandom.NextInt(11) - 5;
                        zDiff += passRandom.NextInt(11) - 5;
                    }
                    else
                    {
                        xDiff += passRandom.NextInt(31) - 15;
                        zDiff += passRandom.NextInt(31) - 15;
                    }

                    tessellator.startDrawing(5);
                    float dimFactor = 0.5F;
                    tessellator.setColorRGBA_F(0.9F * dimFactor, 0.9F * dimFactor, 1.0F * dimFactor, 0.3F);
                    double widthCurrent = 0.1D + pass * 0.2D;
                    if (branch == 0)
                    {
                        widthCurrent *= step * 0.1D + 1.0D;
                    }

                    double widthPrev = 0.1D + pass * 0.2D;
                    if (branch == 0)
                    {
                        widthPrev *= (step - 1) * 0.1D + 1.0D;
                    }

                    for (int v = 0; v < 5; ++v)
                    {
                        double x1 = x + 0.5D - widthCurrent;
                        double z1 = z + 0.5D - widthCurrent;
                        if (v == 1 || v == 2)
                        {
                            x1 += widthCurrent * 2.0D;
                        }

                        if (v == 2 || v == 3)
                        {
                            z1 += widthCurrent * 2.0D;
                        }

                        double x2 = x + 0.5D - widthPrev;
                        double z2 = z + 0.5D - widthPrev;
                        if (v == 1 || v == 2)
                        {
                            x2 += widthPrev * 2.0D;
                        }

                        if (v == 2 || v == 3)
                        {
                            z2 += widthPrev * 2.0D;
                        }

                        tessellator.addVertex(x2 + xDiff, y + step * 16, z2 + zDiff);
                        tessellator.addVertex(x1 + prevXDiff, y + (step + 1) * 16, z1 + prevZDiff);
                    }

                    tessellator.draw();
                }
            }
        }

        GLManager.GL.Disable(GLEnum.Blend);
        GLManager.GL.Enable(GLEnum.Lighting);
        GLManager.GL.Enable(GLEnum.Texture2D);
    }

    public override void render(Entity target, double x, double y, double z, float yaw, float tickDelta)
    {
        render((EntityLightningBolt)target, x, y, z, yaw, tickDelta);
    }
}