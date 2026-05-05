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

    public ModelBiped(float scale) : this(scale, 0.0f)
    {
    }

    public ModelBiped(float scale, float yOffset)
    {
        field_1279_h = false;
        field_1278_i = false;
        isSneak = false;
        bipedCloak = new ModelPart(0, 0);
        bipedCloak.addBox(-5.0F, 0.0F, -1.0F, 10, 16, 1, scale);
        bipedEars = new ModelPart(24, 0);
        bipedEars.addBox(-3.0F, -6.0F, -1.0F, 6, 6, 1, scale);
        bipedHead = new ModelPart(0, 0);
        bipedHead.addBox(-4.0F, -8.0F, -4.0F, 8, 8, 8, scale);
        bipedHead.setRotationPoint(0.0F, 0.0F + yOffset, 0.0F);
        bipedHeadwear = new ModelPart(32, 0);
        bipedHeadwear.addBox(-4.0F, -8.0F, -4.0F, 8, 8, 8, scale + 0.5F);
        bipedHeadwear.setRotationPoint(0.0F, 0.0F + yOffset, 0.0F);
        bipedBody = new ModelPart(16, 16);
        bipedBody.addBox(-4.0F, 0.0F, -2.0F, 8, 12, 4, scale);
        bipedBody.setRotationPoint(0.0F, 0.0F + yOffset, 0.0F);
        bipedRightArm = new ModelPart(40, 16);
        bipedRightArm.addBox(-3.0F, -2.0F, -2.0F, 4, 12, 4, scale);
        bipedRightArm.setRotationPoint(-5.0F, 2.0F + yOffset, 0.0F);
        bipedLeftArm = new ModelPart(40, 16)
        {
            mirror = true
        };
        bipedLeftArm.addBox(-1.0F, -2.0F, -2.0F, 4, 12, 4, scale);
        bipedLeftArm.setRotationPoint(5.0F, 2.0F + yOffset, 0.0F);
        bipedRightLeg = new ModelPart(0, 16);
        bipedRightLeg.addBox(-2.0F, 0.0F, -2.0F, 4, 12, 4, scale);
        bipedRightLeg.setRotationPoint(-2.0F, 12.0F + yOffset, 0.0F);
        bipedLeftLeg = new ModelPart(0, 16)
        {
            mirror = true
        };
        bipedLeftLeg.addBox(-2.0F, 0.0F, -2.0F, 4, 12, 4, scale);
        bipedLeftLeg.setRotationPoint(2.0F, 12.0F + yOffset, 0.0F);
    }

    public override void render(float limbSwing, float limbSwingAmount, float ageInTicks, float netHeadYaw, float headPitch, float scale)
    {
        setRotationAngles(limbSwing, limbSwingAmount, ageInTicks, netHeadYaw, headPitch, scale);
        bipedHead.render(scale);
        bipedBody.render(scale);
        bipedRightArm.render(scale);
        bipedLeftArm.render(scale);
        bipedRightLeg.render(scale);
        bipedLeftLeg.render(scale);
        bipedHeadwear.render(scale);
    }

    public override void setRotationAngles(float limbSwing, float limbSwingAmount, float ageInTicks, float netHeadYaw, float headPitch, float scale)
    {
        bipedHead.rotateAngleY = netHeadYaw / (180.0F / (float)Math.PI);
        bipedHead.rotateAngleX = headPitch / (180.0F / (float)Math.PI);
        bipedHeadwear.rotateAngleY = bipedHead.rotateAngleY;
        bipedHeadwear.rotateAngleX = bipedHead.rotateAngleX;
        bipedRightArm.rotateAngleX = MathHelper.Cos(limbSwing * 0.6662F + (float)Math.PI) * 2.0F * limbSwingAmount * 0.5F;
        bipedLeftArm.rotateAngleX = MathHelper.Cos(limbSwing * 0.6662F) * 2.0F * limbSwingAmount * 0.5F;
        bipedRightArm.rotateAngleZ = 0.0F;
        bipedLeftArm.rotateAngleZ = 0.0F;
        bipedRightLeg.rotateAngleX = MathHelper.Cos(limbSwing * 0.6662F) * 1.4F * limbSwingAmount;
        bipedLeftLeg.rotateAngleX = MathHelper.Cos(limbSwing * 0.6662F + (float)Math.PI) * 1.4F * limbSwingAmount;
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
            float swingProgress = onGround;
            bipedBody.rotateAngleY = MathHelper.Sin(MathHelper.Sqrt(swingProgress) * (float)Math.PI * 2.0F) * 0.2F;
            bipedRightArm.rotationPointZ = MathHelper.Sin(bipedBody.rotateAngleY) * 5.0F;
            bipedRightArm.rotationPointX = -MathHelper.Cos(bipedBody.rotateAngleY) * 5.0F;
            bipedLeftArm.rotationPointZ = -MathHelper.Sin(bipedBody.rotateAngleY) * 5.0F;
            bipedLeftArm.rotationPointX = MathHelper.Cos(bipedBody.rotateAngleY) * 5.0F;
            bipedRightArm.rotateAngleY += bipedBody.rotateAngleY;
            bipedLeftArm.rotateAngleY += bipedBody.rotateAngleY;
            bipedLeftArm.rotateAngleX += bipedBody.rotateAngleY;
            swingProgress = 1.0F - onGround;
            swingProgress *= swingProgress;
            swingProgress *= swingProgress;
            swingProgress = 1.0F - swingProgress;
            float attackSwing = MathHelper.Sin(swingProgress * (float)Math.PI);
            float headOffset = MathHelper.Sin(onGround * (float)Math.PI) * -(bipedHead.rotateAngleX - 0.7F) * (12.0F / 16.0F);
            bipedRightArm.rotateAngleX = (float)(bipedRightArm.rotateAngleX - ((double)attackSwing * 1.2D + (double)headOffset));
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

        bipedRightArm.rotateAngleZ += MathHelper.Cos(ageInTicks * 0.09F) * 0.05F + 0.05F;
        bipedLeftArm.rotateAngleZ -= MathHelper.Cos(ageInTicks * 0.09F) * 0.05F + 0.05F;
        bipedRightArm.rotateAngleX += MathHelper.Sin(ageInTicks * 0.067F) * 0.05F;
        bipedLeftArm.rotateAngleX -= MathHelper.Sin(ageInTicks * 0.067F) * 0.05F;
    }

    public void renderEars(float scale)
    {
        bipedEars.rotateAngleY = bipedHead.rotateAngleY;
        bipedEars.rotateAngleX = bipedHead.rotateAngleX;
        bipedEars.rotationPointX = 0.0F;
        bipedEars.rotationPointY = 0.0F;
        bipedEars.render(scale);
    }

    public void renderCloak(float scale)
    {
        bipedCloak.render(scale);
    }
}
