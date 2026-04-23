using BetaSharp.Util.Maths;

namespace BetaSharp.Client.Rendering.Entities.Models;

public class ModelZombie : ModelBiped
{

    public override void setRotationAngles(float limbSwing, float limbSwingAmount, float ageInTicks, float netHeadYaw, float headPitch, float scale)
    {
        base.setRotationAngles(limbSwing, limbSwingAmount, ageInTicks, netHeadYaw, headPitch, scale);
        float swingProgress = MathHelper.Sin(onGround * (float)Math.PI);
        float attackSwing = MathHelper.Sin((1.0F - (1.0F - onGround) * (1.0F - onGround)) * (float)Math.PI);
        bipedRightArm.rotateAngleZ = 0.0F;
        bipedLeftArm.rotateAngleZ = 0.0F;
        bipedRightArm.rotateAngleY = -(0.1F - swingProgress * 0.6F);
        bipedLeftArm.rotateAngleY = 0.1F - swingProgress * 0.6F;
        bipedRightArm.rotateAngleX = (float)Math.PI * -0.5F;
        bipedLeftArm.rotateAngleX = (float)Math.PI * -0.5F;
        bipedRightArm.rotateAngleX -= swingProgress * 1.2F - attackSwing * 0.4F;
        bipedLeftArm.rotateAngleX -= swingProgress * 1.2F - attackSwing * 0.4F;
        bipedRightArm.rotateAngleZ += MathHelper.Cos(ageInTicks * 0.09F) * 0.05F + 0.05F;
        bipedLeftArm.rotateAngleZ -= MathHelper.Cos(ageInTicks * 0.09F) * 0.05F + 0.05F;
        bipedRightArm.rotateAngleX += MathHelper.Sin(ageInTicks * 0.067F) * 0.05F;
        bipedLeftArm.rotateAngleX -= MathHelper.Sin(ageInTicks * 0.067F) * 0.05F;
    }
}
