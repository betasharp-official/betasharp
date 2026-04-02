using BetaSharp.Util.Maths;

namespace BetaSharp.Client.Rendering.Entities.Models;

public class ModelGhast : ModelBase
{
    private readonly ModelPart body;
    private readonly ModelPart[] tentacles = new ModelPart[9];

    public ModelGhast()
    {
        int var1 = -16;
        body = new ModelPart(0, 0);
        body.addBox(-8.0F, -8.0F, -8.0F, 16, 16, 16);
        body.rotationPointY += 24 + var1;
        JavaRandom var2 = new(1660);

        for (int var3 = 0; var3 < tentacles.Length; ++var3)
        {
            tentacles[var3] = new ModelPart(0, 0);
            float var4 = ((var3 % 3 - var3 / 3 % 2 * 0.5F + 0.25F) / 2.0F * 2.0F - 1.0F) * 5.0F;
            float var5 = (var3 / 3 / 2.0F * 2.0F - 1.0F) * 5.0F;
            int var6 = var2.NextInt(7) + 8;
            tentacles[var3].addBox(-1.0F, 0.0F, -1.0F, 2, var6, 2);
            tentacles[var3].rotationPointX = var4;
            tentacles[var3].rotationPointZ = var5;
            tentacles[var3].rotationPointY = 31 + var1;
        }

    }

    public override void setRotationAngles(float var1, float var2, float var3, float var4, float var5, float var6)
    {
        for (int var7 = 0; var7 < tentacles.Length; ++var7)
        {
            tentacles[var7].rotateAngleX = 0.2F * MathHelper.Sin(var3 * 0.3F + var7) + 0.4F;
        }

    }

    public override void render(float var1, float var2, float var3, float var4, float var5, float var6)
    {
        setRotationAngles(var1, var2, var3, var4, var5, var6);
        body.render(var6);

        for (int var7 = 0; var7 < tentacles.Length; ++var7)
        {
            tentacles[var7].render(var6);
        }

    }
}
