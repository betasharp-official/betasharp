namespace BetaSharp.Client.Rendering.Entities.Models;

public class ModelSkeleton : ModelZombie
{

    public ModelSkeleton()
    {
        float var1 = 0.0F;
        bipedRightArm = new ModelPart(40, 16);
        bipedRightArm.addBox(-1.0F, -2.0F, -1.0F, 2, 12, 2, var1);
        bipedRightArm.setRotationPoint(-5.0F, 2.0F, 0.0F);
        bipedLeftArm = new ModelPart(40, 16)
        {
            mirror = true
        };
        bipedLeftArm.addBox(-1.0F, -2.0F, -1.0F, 2, 12, 2, var1);
        bipedLeftArm.setRotationPoint(5.0F, 2.0F, 0.0F);
        bipedRightLeg = new ModelPart(0, 16);
        bipedRightLeg.addBox(-1.0F, 0.0F, -1.0F, 2, 12, 2, var1);
        bipedRightLeg.setRotationPoint(-2.0F, 12.0F, 0.0F);
        bipedLeftLeg = new ModelPart(0, 16)
        {
            mirror = true
        };
        bipedLeftLeg.addBox(-1.0F, 0.0F, -1.0F, 2, 12, 2, var1);
        bipedLeftLeg.setRotationPoint(2.0F, 12.0F, 0.0F);
    }
}
