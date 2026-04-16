using BetaSharp.Client.Rendering.Legacy;
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
        float var1 = 0.0F;
        byte var2 = 15;
        spiderHead = new ModelPart(32, 4);
        spiderHead.addBox(-4.0F, -4.0F, -8.0F, 8, 8, 8, var1);
        spiderHead.setRotationPoint(0.0F, 0 + var2, -3.0F);
        spiderNeck = new ModelPart(0, 0);
        spiderNeck.addBox(-3.0F, -3.0F, -3.0F, 6, 6, 6, var1);
        spiderNeck.setRotationPoint(0.0F, var2, 0.0F);
        spiderBody = new ModelPart(0, 12);
        spiderBody.addBox(-5.0F, -4.0F, -6.0F, 10, 8, 12, var1);
        spiderBody.setRotationPoint(0.0F, 0 + var2, 9.0F);
        spiderLeg1 = new ModelPart(18, 0);
        spiderLeg1.addBox(-15.0F, -1.0F, -1.0F, 16, 2, 2, var1);
        spiderLeg1.setRotationPoint(-4.0F, 0 + var2, 2.0F);
        spiderLeg2 = new ModelPart(18, 0);
        spiderLeg2.addBox(-1.0F, -1.0F, -1.0F, 16, 2, 2, var1);
        spiderLeg2.setRotationPoint(4.0F, 0 + var2, 2.0F);
        spiderLeg3 = new ModelPart(18, 0);
        spiderLeg3.addBox(-15.0F, -1.0F, -1.0F, 16, 2, 2, var1);
        spiderLeg3.setRotationPoint(-4.0F, 0 + var2, 1.0F);
        spiderLeg4 = new ModelPart(18, 0);
        spiderLeg4.addBox(-1.0F, -1.0F, -1.0F, 16, 2, 2, var1);
        spiderLeg4.setRotationPoint(4.0F, 0 + var2, 1.0F);
        spiderLeg5 = new ModelPart(18, 0);
        spiderLeg5.addBox(-15.0F, -1.0F, -1.0F, 16, 2, 2, var1);
        spiderLeg5.setRotationPoint(-4.0F, 0 + var2, 0.0F);
        spiderLeg6 = new ModelPart(18, 0);
        spiderLeg6.addBox(-1.0F, -1.0F, -1.0F, 16, 2, 2, var1);
        spiderLeg6.setRotationPoint(4.0F, 0 + var2, 0.0F);
        spiderLeg7 = new ModelPart(18, 0);
        spiderLeg7.addBox(-15.0F, -1.0F, -1.0F, 16, 2, 2, var1);
        spiderLeg7.setRotationPoint(-4.0F, 0 + var2, -1.0F);
        spiderLeg8 = new ModelPart(18, 0);
        spiderLeg8.addBox(-1.0F, -1.0F, -1.0F, 16, 2, 2, var1);
        spiderLeg8.setRotationPoint(4.0F, 0 + var2, -1.0F);
    }

    public override void render(ILegacyFixedFunctionApi gl, float var1, float var2, float var3, float var4, float var5,
        float var6)
    {
        setRotationAngles(var1, var2, var3, var4, var5, var6);
        spiderHead.render(gl, var6);
        spiderNeck.render(gl, var6);
        spiderBody.render(gl, var6);
        spiderLeg1.render(gl, var6);
        spiderLeg2.render(gl, var6);
        spiderLeg3.render(gl, var6);
        spiderLeg4.render(gl, var6);
        spiderLeg5.render(gl, var6);
        spiderLeg6.render(gl, var6);
        spiderLeg7.render(gl, var6);
        spiderLeg8.render(gl, var6);
    }

    public override void setRotationAngles(float var1, float var2, float var3, float var4, float var5, float var6)
    {
        spiderHead.rotateAngleY = var4 / (180.0F / (float)Math.PI);
        spiderHead.rotateAngleX = var5 / (180.0F / (float)Math.PI);
        float var7 = (float)Math.PI * 0.25F;
        spiderLeg1.rotateAngleZ = -var7;
        spiderLeg2.rotateAngleZ = var7;
        spiderLeg3.rotateAngleZ = -var7 * 0.74F;
        spiderLeg4.rotateAngleZ = var7 * 0.74F;
        spiderLeg5.rotateAngleZ = -var7 * 0.74F;
        spiderLeg6.rotateAngleZ = var7 * 0.74F;
        spiderLeg7.rotateAngleZ = -var7;
        spiderLeg8.rotateAngleZ = var7;
        float var8 = -0.0F;
        float var9 = (float)Math.PI * 0.125F;
        spiderLeg1.rotateAngleY = var9 * 2.0F + var8;
        spiderLeg2.rotateAngleY = -var9 * 2.0F - var8;
        spiderLeg3.rotateAngleY = var9 * 1.0F + var8;
        spiderLeg4.rotateAngleY = -var9 * 1.0F - var8;
        spiderLeg5.rotateAngleY = -var9 * 1.0F + var8;
        spiderLeg6.rotateAngleY = var9 * 1.0F - var8;
        spiderLeg7.rotateAngleY = -var9 * 2.0F + var8;
        spiderLeg8.rotateAngleY = var9 * 2.0F - var8;
        float var10 = -(MathHelper.Cos(var1 * 0.6662F * 2.0F + 0.0F) * 0.4F) * var2;
        float var11 = -(MathHelper.Cos(var1 * 0.6662F * 2.0F + (float)Math.PI) * 0.4F) * var2;
        float var12 = -(MathHelper.Cos(var1 * 0.6662F * 2.0F + (float)Math.PI * 0.5F) * 0.4F) * var2;
        float var13 = -(MathHelper.Cos(var1 * 0.6662F * 2.0F + (float)Math.PI * 3.0F / 2.0F) * 0.4F) * var2;
        float var14 = Math.Abs(MathHelper.Sin(var1 * 0.6662F + 0.0F) * 0.4F) * var2;
        float var15 = Math.Abs(MathHelper.Sin(var1 * 0.6662F + (float)Math.PI) * 0.4F) * var2;
        float var16 = Math.Abs(MathHelper.Sin(var1 * 0.6662F + (float)Math.PI * 0.5F) * 0.4F) * var2;
        float var17 = Math.Abs(MathHelper.Sin(var1 * 0.6662F + (float)Math.PI * 3.0F / 2.0F) * 0.4F) * var2;
        spiderLeg1.rotateAngleY += var10;
        spiderLeg2.rotateAngleY += -var10;
        spiderLeg3.rotateAngleY += var11;
        spiderLeg4.rotateAngleY += -var11;
        spiderLeg5.rotateAngleY += var12;
        spiderLeg6.rotateAngleY += -var12;
        spiderLeg7.rotateAngleY += var13;
        spiderLeg8.rotateAngleY += -var13;
        spiderLeg1.rotateAngleZ += var14;
        spiderLeg2.rotateAngleZ += -var14;
        spiderLeg3.rotateAngleZ += var15;
        spiderLeg4.rotateAngleZ += -var15;
        spiderLeg5.rotateAngleZ += var16;
        spiderLeg6.rotateAngleZ += -var16;
        spiderLeg7.rotateAngleZ += var17;
        spiderLeg8.rotateAngleZ += -var17;
    }
}
