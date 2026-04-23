using BetaSharp.Util.Maths;

namespace BetaSharp.Client.Rendering.Entities.Models;

public class ModelSpider : ModelBase
{

    public ModelPart spiderHead;
    public ModelPart spiderNeck;
    public ModelPart spiderBody;
    public ModelPart spiderLeg1;
    public ModelPart spiderLeg2;
    public ModelPart spiderLeg3;
    public ModelPart spiderLeg4;
    public ModelPart spiderLeg5;
    public ModelPart spiderLeg6;
    public ModelPart spiderLeg7;
    public ModelPart spiderLeg8;

    public ModelSpider()
    {
        float scale = 0.0F;
        byte yOffset = 15;
        spiderHead = new ModelPart(32, 4);
        spiderHead.addBox(-4.0F, -4.0F, -8.0F, 8, 8, 8, scale);
        spiderHead.setRotationPoint(0.0F, 0 + yOffset, -3.0F);
        spiderNeck = new ModelPart(0, 0);
        spiderNeck.addBox(-3.0F, -3.0F, -3.0F, 6, 6, 6, scale);
        spiderNeck.setRotationPoint(0.0F, yOffset, 0.0F);
        spiderBody = new ModelPart(0, 12);
        spiderBody.addBox(-5.0F, -4.0F, -6.0F, 10, 8, 12, scale);
        spiderBody.setRotationPoint(0.0F, 0 + yOffset, 9.0F);
        spiderLeg1 = new ModelPart(18, 0);
        spiderLeg1.addBox(-15.0F, -1.0F, -1.0F, 16, 2, 2, scale);
        spiderLeg1.setRotationPoint(-4.0F, 0 + yOffset, 2.0F);
        spiderLeg2 = new ModelPart(18, 0);
        spiderLeg2.addBox(-1.0F, -1.0F, -1.0F, 16, 2, 2, scale);
        spiderLeg2.setRotationPoint(4.0F, 0 + yOffset, 2.0F);
        spiderLeg3 = new ModelPart(18, 0);
        spiderLeg3.addBox(-15.0F, -1.0F, -1.0F, 16, 2, 2, scale);
        spiderLeg3.setRotationPoint(-4.0F, 0 + yOffset, 1.0F);
        spiderLeg4 = new ModelPart(18, 0);
        spiderLeg4.addBox(-1.0F, -1.0F, -1.0F, 16, 2, 2, scale);
        spiderLeg4.setRotationPoint(4.0F, 0 + yOffset, 1.0F);
        spiderLeg5 = new ModelPart(18, 0);
        spiderLeg5.addBox(-15.0F, -1.0F, -1.0F, 16, 2, 2, scale);
        spiderLeg5.setRotationPoint(-4.0F, 0 + yOffset, 0.0F);
        spiderLeg6 = new ModelPart(18, 0);
        spiderLeg6.addBox(-1.0F, -1.0F, -1.0F, 16, 2, 2, scale);
        spiderLeg6.setRotationPoint(4.0F, 0 + yOffset, 0.0F);
        spiderLeg7 = new ModelPart(18, 0);
        spiderLeg7.addBox(-15.0F, -1.0F, -1.0F, 16, 2, 2, scale);
        spiderLeg7.setRotationPoint(-4.0F, 0 + yOffset, -1.0F);
        spiderLeg8 = new ModelPart(18, 0);
        spiderLeg8.addBox(-1.0F, -1.0F, -1.0F, 16, 2, 2, scale);
        spiderLeg8.setRotationPoint(4.0F, 0 + yOffset, -1.0F);
    }

    public override void render(float limbSwing, float limbSwingAmount, float ageInTicks, float netHeadYaw, float headPitch, float scale)
    {
        setRotationAngles(limbSwing, limbSwingAmount, ageInTicks, netHeadYaw, headPitch, scale);
        spiderHead.render(scale);
        spiderNeck.render(scale);
        spiderBody.render(scale);
        spiderLeg1.render(scale);
        spiderLeg2.render(scale);
        spiderLeg3.render(scale);
        spiderLeg4.render(scale);
        spiderLeg5.render(scale);
        spiderLeg6.render(scale);
        spiderLeg7.render(scale);
        spiderLeg8.render(scale);
    }

    public override void setRotationAngles(float limbSwing, float limbSwingAmount, float ageInTicks, float netHeadYaw, float headPitch, float scale)
    {
        spiderHead.rotateAngleY = netHeadYaw / (180.0F / (float)Math.PI);
        spiderHead.rotateAngleX = headPitch / (180.0F / (float)Math.PI);
        float baseLegAngle = (float)Math.PI * 0.25F;
        spiderLeg1.rotateAngleZ = -baseLegAngle;
        spiderLeg2.rotateAngleZ = baseLegAngle;
        spiderLeg3.rotateAngleZ = -baseLegAngle * 0.74F;
        spiderLeg4.rotateAngleZ = baseLegAngle * 0.74F;
        spiderLeg5.rotateAngleZ = -baseLegAngle * 0.74F;
        spiderLeg6.rotateAngleZ = baseLegAngle * 0.74F;
        spiderLeg7.rotateAngleZ = -baseLegAngle;
        spiderLeg8.rotateAngleZ = baseLegAngle;
        float yawOffset = -0.0F;
        float yawSpread = (float)Math.PI * 0.125F;
        spiderLeg1.rotateAngleY = yawSpread * 2.0F + yawOffset;
        spiderLeg2.rotateAngleY = -yawSpread * 2.0F - yawOffset;
        spiderLeg3.rotateAngleY = yawSpread * 1.0F + yawOffset;
        spiderLeg4.rotateAngleY = -yawSpread * 1.0F - yawOffset;
        spiderLeg5.rotateAngleY = -yawSpread * 1.0F + yawOffset;
        spiderLeg6.rotateAngleY = yawSpread * 1.0F - yawOffset;
        spiderLeg7.rotateAngleY = -yawSpread * 2.0F + yawOffset;
        spiderLeg8.rotateAngleY = yawSpread * 2.0F - yawOffset;
        float frontLegSwing = -(MathHelper.Cos(limbSwing * 0.6662F * 2.0F + 0.0F) * 0.4F) * limbSwingAmount;
        float midFrontLegSwing = -(MathHelper.Cos(limbSwing * 0.6662F * 2.0F + (float)Math.PI) * 0.4F) * limbSwingAmount;
        float midBackLegSwing = -(MathHelper.Cos(limbSwing * 0.6662F * 2.0F + (float)Math.PI * 0.5F) * 0.4F) * limbSwingAmount;
        float backLegSwing = -(MathHelper.Cos(limbSwing * 0.6662F * 2.0F + (float)Math.PI * 3.0F / 2.0F) * 0.4F) * limbSwingAmount;
        float frontLegLift = Math.Abs(MathHelper.Sin(limbSwing * 0.6662F + 0.0F) * 0.4F) * limbSwingAmount;
        float midFrontLegLift = Math.Abs(MathHelper.Sin(limbSwing * 0.6662F + (float)Math.PI) * 0.4F) * limbSwingAmount;
        float midBackLegLift = Math.Abs(MathHelper.Sin(limbSwing * 0.6662F + (float)Math.PI * 0.5F) * 0.4F) * limbSwingAmount;
        float backLegLift = Math.Abs(MathHelper.Sin(limbSwing * 0.6662F + (float)Math.PI * 3.0F / 2.0F) * 0.4F) * limbSwingAmount;
        spiderLeg1.rotateAngleY += frontLegSwing;
        spiderLeg2.rotateAngleY += -frontLegSwing;
        spiderLeg3.rotateAngleY += midFrontLegSwing;
        spiderLeg4.rotateAngleY += -midFrontLegSwing;
        spiderLeg5.rotateAngleY += midBackLegSwing;
        spiderLeg6.rotateAngleY += -midBackLegSwing;
        spiderLeg7.rotateAngleY += backLegSwing;
        spiderLeg8.rotateAngleY += -backLegSwing;
        spiderLeg1.rotateAngleZ += frontLegLift;
        spiderLeg2.rotateAngleZ += -frontLegLift;
        spiderLeg3.rotateAngleZ += midFrontLegLift;
        spiderLeg4.rotateAngleZ += -midFrontLegLift;
        spiderLeg5.rotateAngleZ += midBackLegLift;
        spiderLeg6.rotateAngleZ += -midBackLegLift;
        spiderLeg7.rotateAngleZ += backLegLift;
        spiderLeg8.rotateAngleZ += -backLegLift;
    }
}
