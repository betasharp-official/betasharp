using BetaSharp.Client.Rendering.Legacy;

namespace BetaSharp.Client.Rendering.Entities.Models;

public class ModelSheep1 : ModelQuadruped
{
    public ModelSheep1() : base(12, 0.0f)
    {
        head = new ModelPart(0, 0);
        head.addBox(-3.0F, -4.0F, -4.0F, 6, 6, 6, 0.6F);
        head.setRotationPoint(0.0F, 6.0F, -8.0F);
        body = new ModelPart(28, 8);
        body.addBox(-4.0F, -10.0F, -7.0F, 8, 16, 6, 1.75F);
        body.setRotationPoint(0.0F, 5.0F, 2.0F);
        float var1 = 0.5F;
        leg1 = new ModelPart(0, 16);
        leg1.addBox(-2.0F, 0.0F, -2.0F, 4, 6, 4, var1);
        leg1.setRotationPoint(-3.0F, 12.0F, 7.0F);
        leg2 = new ModelPart(0, 16);
        leg2.addBox(-2.0F, 0.0F, -2.0F, 4, 6, 4, var1);
        leg2.setRotationPoint(3.0F, 12.0F, 7.0F);
        leg3 = new ModelPart(0, 16);
        leg3.addBox(-2.0F, 0.0F, -2.0F, 4, 6, 4, var1);
        leg3.setRotationPoint(-3.0F, 12.0F, -5.0F);
        leg4 = new ModelPart(0, 16);
        leg4.addBox(-2.0F, 0.0F, -2.0F, 4, 6, 4, var1);
        leg4.setRotationPoint(3.0F, 12.0F, -5.0F);
    }
}
