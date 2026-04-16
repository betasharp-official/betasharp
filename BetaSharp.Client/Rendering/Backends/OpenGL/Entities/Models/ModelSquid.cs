using BetaSharp.Client.Rendering.Legacy;

namespace BetaSharp.Client.Rendering.Entities.Models;

public class ModelSquid : ModelBase
{
    private readonly ModelPart squidBody;

    private readonly ModelPart[]
        squidTentacles = new ModelPart[8];

    public ModelSquid()
    {
        int var1 = -16;
        squidBody = new ModelPart(0, 0);
        squidBody.addBox(-6.0F, -8.0F, -6.0F, 12, 16, 12);
        squidBody.rotationPointY += 24 + var1;

        for (int var2 = 0; var2 < squidTentacles.Length; ++var2)
        {
            squidTentacles[var2] = new ModelPart(48, 0);
            double var3 = var2 * Math.PI * 2.0D / squidTentacles.Length;
            float var5 = (float)Math.Cos(var3) * 5.0F;
            float var6 = (float)Math.Sin(var3) * 5.0F;
            squidTentacles[var2].addBox(-1.0F, 0.0F, -1.0F, 2, 18, 2);
            squidTentacles[var2].rotationPointX = var5;
            squidTentacles[var2].rotationPointZ = var6;
            squidTentacles[var2].rotationPointY = 31 + var1;
            var3 = var2 * Math.PI * -2.0D / squidTentacles.Length + Math.PI * 0.5D;
            squidTentacles[var2].rotateAngleY = (float)var3;
        }
    }

    public override void setRotationAngles(float var1, float var2, float var3, float var4, float var5, float var6)
    {
        for (int var7 = 0; var7 < squidTentacles.Length; ++var7)
        {
            squidTentacles[var7].rotateAngleX = var3;
        }
    }

    public override void render(ILegacyFixedFunctionApi gl, float var1, float var2, float var3, float var4, float var5,
        float var6)
    {
        setRotationAngles(var1, var2, var3, var4, var5, var6);
        squidBody.render(gl, var6);

        for (int var7 = 0; var7 < squidTentacles.Length; ++var7)
        {
            squidTentacles[var7].render(gl, var6);
        }
    }
}
