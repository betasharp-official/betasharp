using BetaSharp.Client.Rendering.Core;
using BetaSharp.Entities;
using BetaSharp.Util.Maths;

namespace BetaSharp.Client.Rendering.Entities.Models;

public class ModelWolf : ModelBase
{

    public ModelPart wolfHeadMain;
    public ModelPart wolfBody;
    public ModelPart wolfLeg1;
    public ModelPart wolfLeg2;
    public ModelPart wolfLeg3;
    public ModelPart wolfLeg4;
    readonly ModelPart wolfRightEar;
    readonly ModelPart wolfLeftEar;
    readonly ModelPart wolfSnout;
    readonly ModelPart wolfTail;
    readonly ModelPart wolfMane;

    public ModelWolf()
    {
        float scale = 0.0F;
        float headHeight = 13.5F;
        wolfHeadMain = new ModelPart(0, 0);
        wolfHeadMain.addBox(-3.0F, -3.0F, -2.0F, 6, 6, 4, scale);
        wolfHeadMain.setRotationPoint(-1.0F, headHeight, -7.0F);
        wolfBody = new ModelPart(18, 14);
        wolfBody.addBox(-4.0F, -2.0F, -3.0F, 6, 9, 6, scale);
        wolfBody.setRotationPoint(0.0F, 14.0F, 2.0F);
        wolfMane = new ModelPart(21, 0);
        wolfMane.addBox(-4.0F, -3.0F, -3.0F, 8, 6, 7, scale);
        wolfMane.setRotationPoint(-1.0F, 14.0F, 2.0F);
        wolfLeg1 = new ModelPart(0, 18);
        wolfLeg1.addBox(-1.0F, 0.0F, -1.0F, 2, 8, 2, scale);
        wolfLeg1.setRotationPoint(-2.5F, 16.0F, 7.0F);
        wolfLeg2 = new ModelPart(0, 18);
        wolfLeg2.addBox(-1.0F, 0.0F, -1.0F, 2, 8, 2, scale);
        wolfLeg2.setRotationPoint(0.5F, 16.0F, 7.0F);
        wolfLeg3 = new ModelPart(0, 18);
        wolfLeg3.addBox(-1.0F, 0.0F, -1.0F, 2, 8, 2, scale);
        wolfLeg3.setRotationPoint(-2.5F, 16.0F, -4.0F);
        wolfLeg4 = new ModelPart(0, 18);
        wolfLeg4.addBox(-1.0F, 0.0F, -1.0F, 2, 8, 2, scale);
        wolfLeg4.setRotationPoint(0.5F, 16.0F, -4.0F);
        wolfTail = new ModelPart(9, 18);
        wolfTail.addBox(-1.0F, 0.0F, -1.0F, 2, 8, 2, scale);
        wolfTail.setRotationPoint(-1.0F, 12.0F, 8.0F);
        wolfRightEar = new ModelPart(16, 14);
        wolfRightEar.addBox(-3.0F, -5.0F, 0.0F, 2, 2, 1, scale);
        wolfRightEar.setRotationPoint(-1.0F, headHeight, -7.0F);
        wolfLeftEar = new ModelPart(16, 14);
        wolfLeftEar.addBox(1.0F, -5.0F, 0.0F, 2, 2, 1, scale);
        wolfLeftEar.setRotationPoint(-1.0F, headHeight, -7.0F);
        wolfSnout = new ModelPart(0, 10);
        wolfSnout.addBox(-2.0F, 0.0F, -5.0F, 3, 3, 4, scale);
        wolfSnout.setRotationPoint(-0.5F, headHeight, -7.0F);
    }

    public override void render(float limbSwing, float limbSwingAmount, float tailPitch, float netHeadYaw, float headPitch, float scale)
    {
        base.render(limbSwing, limbSwingAmount, tailPitch, netHeadYaw, headPitch, scale);
        setRotationAngles(limbSwing, limbSwingAmount, tailPitch, netHeadYaw, headPitch, scale);
        wolfHeadMain.renderWithRotation(scale);
        wolfBody.render(scale);
        wolfLeg1.render(scale);
        wolfLeg2.render(scale);
        wolfLeg3.render(scale);
        wolfLeg4.render(scale);
        wolfRightEar.renderWithRotation(scale);
        wolfLeftEar.renderWithRotation(scale);
        wolfSnout.renderWithRotation(scale);
        wolfTail.renderWithRotation(scale);
        wolfMane.render(scale);
    }

    public override void setLivingAnimations(EntityLiving entity, float limbSwing, float limbSwingAmount, float partialTick)
    {
        EntityWolf wolf = (EntityWolf)entity;
        if (wolf.IsWolfAngry)
        {
            wolfTail.rotateAngleY = 0.0F;
        }
        else
        {
            wolfTail.rotateAngleY = MathHelper.Cos(limbSwing * 0.6662F) * 1.4F * limbSwingAmount;
        }

        if (wolf.IsWolfSitting)
        {
            wolfMane.setRotationPoint(-1.0F, 16.0F, -3.0F);
            wolfMane.rotateAngleX = (float)Math.PI * 0.4F;
            wolfMane.rotateAngleY = 0.0F;
            wolfBody.setRotationPoint(0.0F, 18.0F, 0.0F);
            wolfBody.rotateAngleX = (float)Math.PI * 0.25F;
            wolfTail.setRotationPoint(-1.0F, 21.0F, 6.0F);
            wolfLeg1.setRotationPoint(-2.5F, 22.0F, 2.0F);
            wolfLeg1.rotateAngleX = (float)Math.PI * 3.0F / 2.0F;
            wolfLeg2.setRotationPoint(0.5F, 22.0F, 2.0F);
            wolfLeg2.rotateAngleX = (float)Math.PI * 3.0F / 2.0F;
            wolfLeg3.rotateAngleX = (float)Math.PI * 1.85F;
            wolfLeg3.setRotationPoint(-2.49F, 17.0F, -4.0F);
            wolfLeg4.rotateAngleX = (float)Math.PI * 1.85F;
            wolfLeg4.setRotationPoint(0.51F, 17.0F, -4.0F);
        }
        else
        {
            wolfBody.setRotationPoint(0.0F, 14.0F, 2.0F);
            wolfBody.rotateAngleX = (float)Math.PI * 0.5F;
            wolfMane.setRotationPoint(-1.0F, 14.0F, -3.0F);
            wolfMane.rotateAngleX = wolfBody.rotateAngleX;
            wolfTail.setRotationPoint(-1.0F, 12.0F, 8.0F);
            wolfLeg1.setRotationPoint(-2.5F, 16.0F, 7.0F);
            wolfLeg2.setRotationPoint(0.5F, 16.0F, 7.0F);
            wolfLeg3.setRotationPoint(-2.5F, 16.0F, -4.0F);
            wolfLeg4.setRotationPoint(0.5F, 16.0F, -4.0F);
            wolfLeg1.rotateAngleX = MathHelper.Cos(limbSwing * 0.6662F) * 1.4F * limbSwingAmount;
            wolfLeg2.rotateAngleX = MathHelper.Cos(limbSwing * 0.6662F + (float)Math.PI) * 1.4F * limbSwingAmount;
            wolfLeg3.rotateAngleX = MathHelper.Cos(limbSwing * 0.6662F + (float)Math.PI) * 1.4F * limbSwingAmount;
            wolfLeg4.rotateAngleX = MathHelper.Cos(limbSwing * 0.6662F) * 1.4F * limbSwingAmount;
        }

        float shakeAngle = wolf.getInterestedAngle(partialTick) + wolf.getShakeAngle(partialTick, 0.0F);
        wolfHeadMain.rotateAngleZ = shakeAngle;
        wolfRightEar.rotateAngleZ = shakeAngle;
        wolfLeftEar.rotateAngleZ = shakeAngle;
        wolfSnout.rotateAngleZ = shakeAngle;
        wolfMane.rotateAngleZ = wolf.getShakeAngle(partialTick, -0.08F);
        wolfBody.rotateAngleZ = wolf.getShakeAngle(partialTick, -0.16F);
        wolfTail.rotateAngleZ = wolf.getShakeAngle(partialTick, -0.2F);
        if (wolf.getWolfShaking())
        {
            float shakeBrightness = wolf.GetBrightnessAtEyes(partialTick) * wolf.getShadingWhileShaking(partialTick);
            GLManager.GL.Color3(shakeBrightness, shakeBrightness, shakeBrightness);
        }

    }

    public override void setRotationAngles(float limbSwing, float limbSwingAmount, float tailPitch, float netHeadYaw, float headPitch, float scale)
    {
        base.setRotationAngles(limbSwing, limbSwingAmount, tailPitch, netHeadYaw, headPitch, scale);
        wolfHeadMain.rotateAngleX = headPitch / (180.0F / (float)Math.PI);
        wolfHeadMain.rotateAngleY = netHeadYaw / (180.0F / (float)Math.PI);
        wolfRightEar.rotateAngleY = wolfHeadMain.rotateAngleY;
        wolfRightEar.rotateAngleX = wolfHeadMain.rotateAngleX;
        wolfLeftEar.rotateAngleY = wolfHeadMain.rotateAngleY;
        wolfLeftEar.rotateAngleX = wolfHeadMain.rotateAngleX;
        wolfSnout.rotateAngleY = wolfHeadMain.rotateAngleY;
        wolfSnout.rotateAngleX = wolfHeadMain.rotateAngleX;
        wolfTail.rotateAngleX = tailPitch;
    }
}
