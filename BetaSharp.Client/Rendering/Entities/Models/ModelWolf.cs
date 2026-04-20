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
        float var1 = 0.0F;
        float var2 = 13.5F;
        wolfHeadMain = new ModelPart(0, 0);
        wolfHeadMain.addBox(-3.0F, -3.0F, -2.0F, 6, 6, 4, var1);
        wolfHeadMain.setRotationPoint(-1.0F, var2, -7.0F);
        wolfBody = new ModelPart(18, 14);
        wolfBody.addBox(-4.0F, -2.0F, -3.0F, 6, 9, 6, var1);
        wolfBody.setRotationPoint(0.0F, 14.0F, 2.0F);
        wolfMane = new ModelPart(21, 0);
        wolfMane.addBox(-4.0F, -3.0F, -3.0F, 8, 6, 7, var1);
        wolfMane.setRotationPoint(-1.0F, 14.0F, 2.0F);
        wolfLeg1 = new ModelPart(0, 18);
        wolfLeg1.addBox(-1.0F, 0.0F, -1.0F, 2, 8, 2, var1);
        wolfLeg1.setRotationPoint(-2.5F, 16.0F, 7.0F);
        wolfLeg2 = new ModelPart(0, 18);
        wolfLeg2.addBox(-1.0F, 0.0F, -1.0F, 2, 8, 2, var1);
        wolfLeg2.setRotationPoint(0.5F, 16.0F, 7.0F);
        wolfLeg3 = new ModelPart(0, 18);
        wolfLeg3.addBox(-1.0F, 0.0F, -1.0F, 2, 8, 2, var1);
        wolfLeg3.setRotationPoint(-2.5F, 16.0F, -4.0F);
        wolfLeg4 = new ModelPart(0, 18);
        wolfLeg4.addBox(-1.0F, 0.0F, -1.0F, 2, 8, 2, var1);
        wolfLeg4.setRotationPoint(0.5F, 16.0F, -4.0F);
        wolfTail = new ModelPart(9, 18);
        wolfTail.addBox(-1.0F, 0.0F, -1.0F, 2, 8, 2, var1);
        wolfTail.setRotationPoint(-1.0F, 12.0F, 8.0F);
        wolfRightEar = new ModelPart(16, 14);
        wolfRightEar.addBox(-3.0F, -5.0F, 0.0F, 2, 2, 1, var1);
        wolfRightEar.setRotationPoint(-1.0F, var2, -7.0F);
        wolfLeftEar = new ModelPart(16, 14);
        wolfLeftEar.addBox(1.0F, -5.0F, 0.0F, 2, 2, 1, var1);
        wolfLeftEar.setRotationPoint(-1.0F, var2, -7.0F);
        wolfSnout = new ModelPart(0, 10);
        wolfSnout.addBox(-2.0F, 0.0F, -5.0F, 3, 3, 4, var1);
        wolfSnout.setRotationPoint(-0.5F, var2, -7.0F);
    }

    public override void render(float var1, float var2, float var3, float var4, float var5, float var6)
    {
        base.render(var1, var2, var3, var4, var5, var6);
        setRotationAngles(var1, var2, var3, var4, var5, var6);
        wolfHeadMain.renderWithRotation(var6);
        wolfBody.render(var6);
        wolfLeg1.render(var6);
        wolfLeg2.render(var6);
        wolfLeg3.render(var6);
        wolfLeg4.render(var6);
        wolfRightEar.renderWithRotation(var6);
        wolfLeftEar.renderWithRotation(var6);
        wolfSnout.renderWithRotation(var6);
        wolfTail.renderWithRotation(var6);
        wolfMane.render(var6);
    }

    public override void setLivingAnimations(EntityLiving var1, float var2, float var3, float var4)
    {
        EntityWolf var5 = (EntityWolf)var1;
        if (var5.isWolfAngry())
        {
            wolfTail.rotateAngleY = 0.0F;
        }
        else
        {
            wolfTail.rotateAngleY = MathHelper.Cos(var2 * 0.6662F) * 1.4F * var3;
        }

        if (var5.isWolfSitting())
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
            wolfLeg1.rotateAngleX = MathHelper.Cos(var2 * 0.6662F) * 1.4F * var3;
            wolfLeg2.rotateAngleX = MathHelper.Cos(var2 * 0.6662F + (float)Math.PI) * 1.4F * var3;
            wolfLeg3.rotateAngleX = MathHelper.Cos(var2 * 0.6662F + (float)Math.PI) * 1.4F * var3;
            wolfLeg4.rotateAngleX = MathHelper.Cos(var2 * 0.6662F) * 1.4F * var3;
        }

        float var6 = var5.getInterestedAngle(var4) + var5.getShakeAngle(var4, 0.0F);
        wolfHeadMain.rotateAngleZ = var6;
        wolfRightEar.rotateAngleZ = var6;
        wolfLeftEar.rotateAngleZ = var6;
        wolfSnout.rotateAngleZ = var6;
        wolfMane.rotateAngleZ = var5.getShakeAngle(var4, -0.08F);
        wolfBody.rotateAngleZ = var5.getShakeAngle(var4, -0.16F);
        wolfTail.rotateAngleZ = var5.getShakeAngle(var4, -0.2F);
        if (var5.getWolfShaking())
        {
            float var7 = var5.GetBrightnessAtEyes(var4) * var5.getShadingWhileShaking(var4);
            GLManager.GL.Color3(var7, var7, var7);
        }

    }

    public override void setRotationAngles(float var1, float var2, float var3, float var4, float var5, float var6)
    {
        base.setRotationAngles(var1, var2, var3, var4, var5, var6);
        wolfHeadMain.rotateAngleX = var5 / (180.0F / (float)Math.PI);
        wolfHeadMain.rotateAngleY = var4 / (180.0F / (float)Math.PI);
        wolfRightEar.rotateAngleY = wolfHeadMain.rotateAngleY;
        wolfRightEar.rotateAngleX = wolfHeadMain.rotateAngleX;
        wolfLeftEar.rotateAngleY = wolfHeadMain.rotateAngleY;
        wolfLeftEar.rotateAngleX = wolfHeadMain.rotateAngleX;
        wolfSnout.rotateAngleY = wolfHeadMain.rotateAngleY;
        wolfSnout.rotateAngleX = wolfHeadMain.rotateAngleX;
        wolfTail.rotateAngleX = var3;
    }
}
