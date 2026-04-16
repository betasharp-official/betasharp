using BetaSharp.Client.Rendering.Legacy;

namespace BetaSharp.Client.Rendering.Entities.Models;

public class ModelBoat : ModelBase
{
    public ModelPart[] boatSides = new ModelPart[5];

    public ModelBoat()
    {
        boatSides[0] = new ModelPart(0, 8);
        boatSides[1] = new ModelPart(0, 0);
        boatSides[2] = new ModelPart(0, 0);
        boatSides[3] = new ModelPart(0, 0);
        boatSides[4] = new ModelPart(0, 0);
        byte var1 = 24;
        byte var2 = 6;
        byte var3 = 20;
        byte var4 = 4;
        boatSides[0].addBox(-var1 / 2, -var3 / 2 + 2, -3.0F, var1, var3 - 4, 4, 0.0F);
        boatSides[0].setRotationPoint(0.0F, 0 + var4, 0.0F);
        boatSides[1].addBox(-var1 / 2 + 2, -var2 - 1, -1.0F, var1 - 4, var2, 2, 0.0F);
        boatSides[1].setRotationPoint(-var1 / 2 + 1, 0 + var4, 0.0F);
        boatSides[2].addBox(-var1 / 2 + 2, -var2 - 1, -1.0F, var1 - 4, var2, 2, 0.0F);
        boatSides[2].setRotationPoint(var1 / 2 - 1, 0 + var4, 0.0F);
        boatSides[3].addBox(-var1 / 2 + 2, -var2 - 1, -1.0F, var1 - 4, var2, 2, 0.0F);
        boatSides[3].setRotationPoint(0.0F, 0 + var4, -var3 / 2 + 1);
        boatSides[4].addBox(-var1 / 2 + 2, -var2 - 1, -1.0F, var1 - 4, var2, 2, 0.0F);
        boatSides[4].setRotationPoint(0.0F, 0 + var4, var3 / 2 - 1);
        boatSides[0].rotateAngleX = (float)Math.PI * 0.5F;
        boatSides[1].rotateAngleY = (float)Math.PI * 3.0F / 2.0F;
        boatSides[2].rotateAngleY = (float)Math.PI * 0.5F;
        boatSides[3].rotateAngleY = (float)Math.PI;
    }

    public override void render(ILegacyFixedFunctionApi gl, float var1, float var2, float var3, float var4, float var5,
        float var6)
    {
        for (int var7 = 0; var7 < 5; ++var7)
        {
            boatSides[var7].render(gl, var6);
        }
    }

    public override void setRotationAngles(float var1, float var2, float var3, float var4, float var5, float var6)
    {
    }
}
