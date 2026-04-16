using BetaSharp.Client.Rendering.Legacy;
using BetaSharp.Util.Maths;

namespace BetaSharp.Client.Rendering.Entities.Models;

public class ModelZombie : ModelBiped
{
    public override void setRotationAngles(float var1, float var2, float var3, float var4, float var5, float var6)
    {
        base.setRotationAngles(var1, var2, var3, var4, var5, var6);
        float var7 = MathHelper.Sin(onGround * (float)Math.PI);
        float var8 = MathHelper.Sin((1.0F - (1.0F - onGround) * (1.0F - onGround)) * (float)Math.PI);
        bipedRightArm.rotateAngleZ = 0.0F;
        bipedLeftArm.rotateAngleZ = 0.0F;
        bipedRightArm.rotateAngleY = -(0.1F - var7 * 0.6F);
        bipedLeftArm.rotateAngleY = 0.1F - var7 * 0.6F;
        bipedRightArm.rotateAngleX = (float)Math.PI * -0.5F;
        bipedLeftArm.rotateAngleX = (float)Math.PI * -0.5F;
        bipedRightArm.rotateAngleX -= var7 * 1.2F - var8 * 0.4F;
        bipedLeftArm.rotateAngleX -= var7 * 1.2F - var8 * 0.4F;
        bipedRightArm.rotateAngleZ += MathHelper.Cos(var3 * 0.09F) * 0.05F + 0.05F;
        bipedLeftArm.rotateAngleZ -= MathHelper.Cos(var3 * 0.09F) * 0.05F + 0.05F;
        bipedRightArm.rotateAngleX += MathHelper.Sin(var3 * 0.067F) * 0.05F;
        bipedLeftArm.rotateAngleX -= MathHelper.Sin(var3 * 0.067F) * 0.05F;
    }
}
