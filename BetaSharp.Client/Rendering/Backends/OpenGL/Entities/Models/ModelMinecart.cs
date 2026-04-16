using BetaSharp.Client.Rendering.Legacy;

namespace BetaSharp.Client.Rendering.Entities.Models;

public class ModelMinecart : ModelBase
{
    public ModelPart[] sideModels = new ModelPart[7];

    public ModelMinecart()
    {
        sideModels[0] = new ModelPart(0, 10);
        sideModels[1] = new ModelPart(0, 0);
        sideModels[2] = new ModelPart(0, 0);
        sideModels[3] = new ModelPart(0, 0);
        sideModels[4] = new ModelPart(0, 0);
        sideModels[5] = new ModelPart(44, 10);
        byte var1 = 20;
        byte var2 = 8;
        byte var3 = 16;
        byte var4 = 4;
        sideModels[0].addBox(-var1 / 2, -var3 / 2, -1.0F, var1, var3, 2, 0.0F);
        sideModels[0].setRotationPoint(0.0F, 0 + var4, 0.0F);
        sideModels[5].addBox(-var1 / 2 + 1, -var3 / 2 + 1, -1.0F, var1 - 2, var3 - 2, 1, 0.0F);
        sideModels[5].setRotationPoint(0.0F, 0 + var4, 0.0F);
        sideModels[1].addBox(-var1 / 2 + 2, -var2 - 1, -1.0F, var1 - 4, var2, 2, 0.0F);
        sideModels[1].setRotationPoint(-var1 / 2 + 1, 0 + var4, 0.0F);
        sideModels[2].addBox(-var1 / 2 + 2, -var2 - 1, -1.0F, var1 - 4, var2, 2, 0.0F);
        sideModels[2].setRotationPoint(var1 / 2 - 1, 0 + var4, 0.0F);
        sideModels[3].addBox(-var1 / 2 + 2, -var2 - 1, -1.0F, var1 - 4, var2, 2, 0.0F);
        sideModels[3].setRotationPoint(0.0F, 0 + var4, -var3 / 2 + 1);
        sideModels[4].addBox(-var1 / 2 + 2, -var2 - 1, -1.0F, var1 - 4, var2, 2, 0.0F);
        sideModels[4].setRotationPoint(0.0F, 0 + var4, var3 / 2 - 1);
        sideModels[0].rotateAngleX = (float)Math.PI * 0.5F;
        sideModels[1].rotateAngleY = (float)Math.PI * 3.0F / 2.0F;
        sideModels[2].rotateAngleY = (float)Math.PI * 0.5F;
        sideModels[3].rotateAngleY = (float)Math.PI;
        sideModels[5].rotateAngleX = (float)Math.PI * -0.5F;
    }

    public override void render(ILegacyFixedFunctionApi gl, float var1, float var2, float var3, float var4, float var5,
        float var6)
    {
        sideModels[5].rotationPointY = 4.0F - var3;

        for (int var7 = 0; var7 < 6; ++var7)
        {
            sideModels[var7].render(gl, var6);
        }
    }

    public override void setRotationAngles(float var1, float var2, float var3, float var4, float var5, float var6)
    {
    }
}
