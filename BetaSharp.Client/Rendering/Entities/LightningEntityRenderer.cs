using BetaSharp.Client.Rendering.Core;
using BetaSharp.Client.Rendering.Core.OpenGL;
using BetaSharp.Entities;
using BetaSharp.Util.Maths;

namespace BetaSharp.Client.Rendering.Entities;

public class LightningEntityRenderer : EntityRenderer
{

    public void render(EntityLightningBolt var1, double x, double y, double z, float yaw, float tickDelta)
    {
        Tessellator var10 = Tessellator.instance;
        GLManager.GL.Disable(GLEnum.Texture2D);
        GLManager.GL.Disable(GLEnum.Lighting);
        GLManager.GL.Enable(GLEnum.Blend);
        GLManager.GL.BlendFunc(GLEnum.SrcAlpha, GLEnum.One);
        double[] var11 = new double[8];
        double[] var12 = new double[8];
        double var13 = 0.0D;
        double var15 = 0.0D;
        JavaRandom var17 = new(var1.renderSeed);

        for (int var18 = 7; var18 >= 0; --var18)
        {
            var11[var18] = var13;
            var12[var18] = var15;
            var13 += var17.NextInt(11) - 5;
            var15 += var17.NextInt(11) - 5;
        }

        for (int var45 = 0; var45 < 4; ++var45)
        {
            JavaRandom var46 = new(var1.renderSeed);

            for (int var19 = 0; var19 < 3; ++var19)
            {
                int var20 = 7;
                int var21 = 0;
                if (var19 > 0)
                {
                    var20 = 7 - var19;
                }

                if (var19 > 0)
                {
                    var21 = var20 - 2;
                }

                double var22 = var11[var20] - var13;
                double var24 = var12[var20] - var15;

                for (int var26 = var20; var26 >= var21; --var26)
                {
                    double var27 = var22;
                    double var29 = var24;
                    if (var19 == 0)
                    {
                        var22 += var46.NextInt(11) - 5;
                        var24 += var46.NextInt(11) - 5;
                    }
                    else
                    {
                        var22 += var46.NextInt(31) - 15;
                        var24 += var46.NextInt(31) - 15;
                    }

                    var10.startDrawing(5);
                    float var31 = 0.5F;
                    var10.setColorRGBA_F(0.9F * var31, 0.9F * var31, 1.0F * var31, 0.3F);
                    double var32 = 0.1D + var45 * 0.2D;
                    if (var19 == 0)
                    {
                        var32 *= var26 * 0.1D + 1.0D;
                    }

                    double var34 = 0.1D + var45 * 0.2D;
                    if (var19 == 0)
                    {
                        var34 *= (var26 - 1) * 0.1D + 1.0D;
                    }

                    for (int var36 = 0; var36 < 5; ++var36)
                    {
                        double var37 = x + 0.5D - var32;
                        double var39 = z + 0.5D - var32;
                        if (var36 == 1 || var36 == 2)
                        {
                            var37 += var32 * 2.0D;
                        }

                        if (var36 == 2 || var36 == 3)
                        {
                            var39 += var32 * 2.0D;
                        }

                        double var41 = x + 0.5D - var34;
                        double var43 = z + 0.5D - var34;
                        if (var36 == 1 || var36 == 2)
                        {
                            var41 += var34 * 2.0D;
                        }

                        if (var36 == 2 || var36 == 3)
                        {
                            var43 += var34 * 2.0D;
                        }

                        var10.addVertex(var41 + var22, y + var26 * 16, var43 + var24);
                        var10.addVertex(var37 + var27, y + (var26 + 1) * 16, var39 + var29);
                    }

                    var10.draw();
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