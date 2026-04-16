using BetaSharp.Client.Rendering.Legacy;
using BetaSharp.Util.Maths;

namespace BetaSharp.Client.Rendering.Entities.Models;

public class ModelBiped : ModelBase
{
    public ModelPart bipedHead;
    public ModelPart bipedHeadwear;
    public ModelPart bipedBody;
    public ModelPart bipedRightArm;
    public ModelPart bipedLeftArm;
    public ModelPart bipedRightLeg;
    public ModelPart bipedLeftLeg;
    public ModelPart bipedEars;
    public ModelPart bipedCloak;
    public bool field_1279_h;
    public bool field_1278_i;
    public bool isSneak;

    public ModelBiped() : this(0.0f)
    {
    }

    public ModelBiped(float var1) : this(var1, 0.0f)
    {
    }

    public ModelBiped(float var1, float var2)
    {
        field_1279_h = false;
        field_1278_i = false;
        isSneak = false;
        bipedCloak = new ModelPart(0, 0);
        bipedCloak.addBox(-5.0F, 0.0F, -1.0F, 10, 16, 1, var1);
        bipedEars = new ModelPart(24, 0);
        bipedEars.addBox(-3.0F, -6.0F, -1.0F, 6, 6, 1, var1);
        bipedHead = new ModelPart(0, 0);
        bipedHead.addBox(-4.0F, -8.0F, -4.0F, 8, 8, 8, var1);
        bipedHead.setRotationPoint(0.0F, 0.0F + var2, 0.0F);
        bipedHeadwear = new ModelPart(32, 0);
        bipedHeadwear.addBox(-4.0F, -8.0F, -4.0F, 8, 8, 8, var1 + 0.5F);
        bipedHeadwear.setRotationPoint(0.0F, 0.0F + var2, 0.0F);
        bipedBody = new ModelPart(16, 16);
        bipedBody.addBox(-4.0F, 0.0F, -2.0F, 8, 12, 4, var1);
        bipedBody.setRotationPoint(0.0F, 0.0F + var2, 0.0F);
        bipedRightArm = new ModelPart(40, 16);
        bipedRightArm.addBox(-3.0F, -2.0F, -2.0F, 4, 12, 4, var1);
        bipedRightArm.setRotationPoint(-5.0F, 2.0F + var2, 0.0F);
        bipedLeftArm = new ModelPart(40, 16)
        {
            mirror = true
        };
        bipedLeftArm.addBox(-1.0F, -2.0F, -2.0F, 4, 12, 4, var1);
        bipedLeftArm.setRotationPoint(5.0F, 2.0F + var2, 0.0F);
        bipedRightLeg = new ModelPart(0, 16);
        bipedRightLeg.addBox(-2.0F, 0.0F, -2.0F, 4, 12, 4, var1);
        bipedRightLeg.setRotationPoint(-2.0F, 12.0F + var2, 0.0F);
        bipedLeftLeg = new ModelPart(0, 16)
        {
            mirror = true
        };
        bipedLeftLeg.addBox(-2.0F, 0.0F, -2.0F, 4, 12, 4, var1);
        bipedLeftLeg.setRotationPoint(2.0F, 12.0F + var2, 0.0F);
    }

    public override void render(ILegacyFixedFunctionApi gl, float var1, float var2, float var3, float var4, float var5,
        float var6)
    {
        setRotationAngles(var1, var2, var3, var4, var5, var6);
        bipedHead.render(gl, var6);
        bipedBody.render(gl, var6);
        bipedRightArm.render(gl, var6);
        bipedLeftArm.render(gl, var6);
        bipedRightLeg.render(gl, var6);
        bipedLeftLeg.render(gl, var6);
        bipedHeadwear.render(gl, var6);
    }

    public override void setRotationAngles(float var1, float var2, float var3, float var4, float var5, float var6)
    {
        bipedHead.rotateAngleY = var4 / (180.0F / (float)Math.PI);
        bipedHead.rotateAngleX = var5 / (180.0F / (float)Math.PI);
        bipedHeadwear.rotateAngleY = bipedHead.rotateAngleY;
        bipedHeadwear.rotateAngleX = bipedHead.rotateAngleX;
        bipedRightArm.rotateAngleX = MathHelper.Cos(var1 * 0.6662F + (float)Math.PI) * 2.0F * var2 * 0.5F;
        bipedLeftArm.rotateAngleX = MathHelper.Cos(var1 * 0.6662F) * 2.0F * var2 * 0.5F;
        bipedRightArm.rotateAngleZ = 0.0F;
        bipedLeftArm.rotateAngleZ = 0.0F;
        bipedRightLeg.rotateAngleX = MathHelper.Cos(var1 * 0.6662F) * 1.4F * var2;
        bipedLeftLeg.rotateAngleX = MathHelper.Cos(var1 * 0.6662F + (float)Math.PI) * 1.4F * var2;
        bipedRightLeg.rotateAngleY = 0.0F;
        bipedLeftLeg.rotateAngleY = 0.0F;
        if (isRiding)
        {
            bipedRightArm.rotateAngleX += (float)Math.PI * -0.2F;
            bipedLeftArm.rotateAngleX += (float)Math.PI * -0.2F;
            bipedRightLeg.rotateAngleX = (float)Math.PI * -0.4F;
            bipedLeftLeg.rotateAngleX = (float)Math.PI * -0.4F;
            bipedRightLeg.rotateAngleY = (float)Math.PI * 0.1F;
            bipedLeftLeg.rotateAngleY = (float)Math.PI * -0.1F;
        }

        if (field_1279_h)
        {
            bipedLeftArm.rotateAngleX = bipedLeftArm.rotateAngleX * 0.5F - (float)Math.PI * 0.1F;
        }

        if (field_1278_i)
        {
            bipedRightArm.rotateAngleX = bipedRightArm.rotateAngleX * 0.5F - (float)Math.PI * 0.1F;
        }

        bipedRightArm.rotateAngleY = 0.0F;
        bipedLeftArm.rotateAngleY = 0.0F;
        if (onGround > -9990.0F)
        {
            float var7 = onGround;
            bipedBody.rotateAngleY = MathHelper.Sin(MathHelper.Sqrt(var7) * (float)Math.PI * 2.0F) * 0.2F;
            bipedRightArm.rotationPointZ = MathHelper.Sin(bipedBody.rotateAngleY) * 5.0F;
            bipedRightArm.rotationPointX = -MathHelper.Cos(bipedBody.rotateAngleY) * 5.0F;
            bipedLeftArm.rotationPointZ = -MathHelper.Sin(bipedBody.rotateAngleY) * 5.0F;
            bipedLeftArm.rotationPointX = MathHelper.Cos(bipedBody.rotateAngleY) * 5.0F;
            bipedRightArm.rotateAngleY += bipedBody.rotateAngleY;
            bipedLeftArm.rotateAngleY += bipedBody.rotateAngleY;
            bipedLeftArm.rotateAngleX += bipedBody.rotateAngleY;
            var7 = 1.0F - onGround;
            var7 *= var7;
            var7 *= var7;
            var7 = 1.0F - var7;
            float var8 = MathHelper.Sin(var7 * (float)Math.PI);
            float var9 = MathHelper.Sin(onGround * (float)Math.PI) * -(bipedHead.rotateAngleX - 0.7F) * (12.0F / 16.0F);
            bipedRightArm.rotateAngleX = (float)(bipedRightArm.rotateAngleX - ((double)var8 * 1.2D + (double)var9));
            bipedRightArm.rotateAngleY += bipedBody.rotateAngleY * 2.0F;
            bipedRightArm.rotateAngleZ = MathHelper.Sin(onGround * (float)Math.PI) * -0.4F;
        }

        if (isSneak)
        {
            bipedBody.rotateAngleX = 0.5F;
            bipedRightLeg.rotateAngleX -= 0.0F;
            bipedLeftLeg.rotateAngleX -= 0.0F;
            bipedRightArm.rotateAngleX += 0.4F;
            bipedLeftArm.rotateAngleX += 0.4F;
            bipedRightLeg.rotationPointZ = 4.0F;
            bipedLeftLeg.rotationPointZ = 4.0F;
            bipedRightLeg.rotationPointY = 9.0F;
            bipedLeftLeg.rotationPointY = 9.0F;
            bipedHead.rotationPointY = 1.0F;
        }
        else
        {
            bipedBody.rotateAngleX = 0.0F;
            bipedRightLeg.rotationPointZ = 0.0F;
            bipedLeftLeg.rotationPointZ = 0.0F;
            bipedRightLeg.rotationPointY = 12.0F;
            bipedLeftLeg.rotationPointY = 12.0F;
            bipedHead.rotationPointY = 0.0F;
        }

        bipedRightArm.rotateAngleZ += MathHelper.Cos(var3 * 0.09F) * 0.05F + 0.05F;
        bipedLeftArm.rotateAngleZ -= MathHelper.Cos(var3 * 0.09F) * 0.05F + 0.05F;
        bipedRightArm.rotateAngleX += MathHelper.Sin(var3 * 0.067F) * 0.05F;
        bipedLeftArm.rotateAngleX -= MathHelper.Sin(var3 * 0.067F) * 0.05F;
    }

    public void renderEars(ILegacyFixedFunctionApi gl, float var1)
    {
        bipedEars.rotateAngleY = bipedHead.rotateAngleY;
        bipedEars.rotateAngleX = bipedHead.rotateAngleX;
        bipedEars.rotationPointX = 0.0F;
        bipedEars.rotationPointY = 0.0F;
        bipedEars.render(gl, var1);
    }

    public void renderCloak(ILegacyFixedFunctionApi gl, float var1)
    {
        bipedCloak.render(gl, var1);
    }
}
