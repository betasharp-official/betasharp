using BetaSharp.Client.Rendering.Core;
using BetaSharp.Client.Rendering.Core.OpenGL;
using BetaSharp.Entities;
using BetaSharp.Util.Maths;

namespace BetaSharp.Client.Rendering.Entities;

public class LightningEntityRenderer : EntityRenderer
{

    public void render(EntityLightningBolt lightningBolt, double x, double y, double z, float yaw, float tickDelta)
    {
        Tessellator tessellator = Tessellator.instance;
        GLManager.GL.Disable(GLEnum.Texture2D);
        GLManager.GL.Disable(GLEnum.Lighting);
        GLManager.GL.Enable(GLEnum.Blend);
        GLManager.GL.BlendFunc(GLEnum.SrcAlpha, GLEnum.One);
        double[] xOffsets = new double[8];
        double[] zOffsets = new double[8];
        double offsetX = 0.0D;
        double offsetZ = 0.0D;
        JavaRandom random = new(lightningBolt.RenderSeed);

        for (int segmentIndex = 7; segmentIndex >= 0; --segmentIndex)
        {
            xOffsets[segmentIndex] = offsetX;
            zOffsets[segmentIndex] = offsetZ;
            offsetX += random.NextInt(11) - 5;
            offsetZ += random.NextInt(11) - 5;
        }

        for (int layerIndex = 0; layerIndex < 4; ++layerIndex)
        {
            JavaRandom branchRandom = new(lightningBolt.RenderSeed);

            for (int branchDepth = 0; branchDepth < 3; ++branchDepth)
            {
                int startIndex = 7;
                int endIndex = 0;
                if (branchDepth > 0)
                {
                    startIndex = 7 - branchDepth;
                }

                if (branchDepth > 0)
                {
                    endIndex = startIndex - 2;
                }

                double branchX = xOffsets[startIndex] - offsetX;
                double branchZ = zOffsets[startIndex] - offsetZ;

                for (int yIndex = startIndex; yIndex >= endIndex; --yIndex)
                {
                    double prevBranchX = branchX;
                    double prevBranchZ = branchZ;
                    if (branchDepth == 0)
                    {
                        branchX += branchRandom.NextInt(11) - 5;
                        branchZ += branchRandom.NextInt(11) - 5;
                    }
                    else
                    {
                        branchX += branchRandom.NextInt(31) - 15;
                        branchZ += branchRandom.NextInt(31) - 15;
                    }

                    tessellator.startDrawing(5);
                    float alphaScale = 0.5F;
                    tessellator.setColorRGBA_F(0.9F * alphaScale, 0.9F * alphaScale, 1.0F * alphaScale, 0.3F);
                    double outerRadius = 0.1D + layerIndex * 0.2D;
                    if (branchDepth == 0)
                    {
                        outerRadius *= yIndex * 0.1D + 1.0D;
                    }

                    double innerRadius = 0.1D + layerIndex * 0.2D;
                    if (branchDepth == 0)
                    {
                        innerRadius *= (yIndex - 1) * 0.1D + 1.0D;
                    }

                    for (int cornerIndex = 0; cornerIndex < 5; ++cornerIndex)
                    {
                        double outerX = x + 0.5D - outerRadius;
                        double outerZ = z + 0.5D - outerRadius;
                        if (cornerIndex == 1 || cornerIndex == 2)
                        {
                            outerX += outerRadius * 2.0D;
                        }

                        if (cornerIndex == 2 || cornerIndex == 3)
                        {
                            outerZ += outerRadius * 2.0D;
                        }

                        double innerX = x + 0.5D - innerRadius;
                        double innerZ = z + 0.5D - innerRadius;
                        if (cornerIndex == 1 || cornerIndex == 2)
                        {
                            innerX += innerRadius * 2.0D;
                        }

                        if (cornerIndex == 2 || cornerIndex == 3)
                        {
                            innerZ += innerRadius * 2.0D;
                        }

                        tessellator.addVertex(innerX + branchX, y + yIndex * 16, innerZ + branchZ);
                        tessellator.addVertex(outerX + prevBranchX, y + (yIndex + 1) * 16, outerZ + prevBranchZ);
                    }

                    tessellator.draw();
                }
            }
        }

        GLManager.GL.Disable(GLEnum.Blend);
        GLManager.GL.Enable(GLEnum.Lighting);
        GLManager.GL.Enable(GLEnum.Texture2D);
    }

    public override void Render(Entity target, double x, double y, double z, float yaw, float tickDelta)
    {
        render((EntityLightningBolt)target, x, y, z, yaw, tickDelta);
    }
}
