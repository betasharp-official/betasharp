using BetaSharp.Util.Maths;

namespace BetaSharp.Client.Rendering.Entities.Models;

public class ModelQuadruped : ModelBase
{

    public ModelPart head = new(0, 0);
    public ModelPart body;
    public ModelPart leg1;
    public ModelPart leg2;
    public ModelPart leg3;
    public ModelPart leg4;

    public ModelQuadruped(int legHeight, float scale)
    {
        head.addBox(-4.0F, -4.0F, -8.0F, 8, 8, 8, scale);
        head.setRotationPoint(0.0F, 18 - legHeight, -6.0F);
        body = new ModelPart(28, 8);
        body.addBox(-5.0F, -10.0F, -7.0F, 10, 16, 8, scale);
        body.setRotationPoint(0.0F, 17 - legHeight, 2.0F);
        leg1 = new ModelPart(0, 16);
        leg1.addBox(-2.0F, 0.0F, -2.0F, 4, legHeight, 4, scale);
        leg1.setRotationPoint(-3.0F, 24 - legHeight, 7.0F);
        leg2 = new ModelPart(0, 16);
        leg2.addBox(-2.0F, 0.0F, -2.0F, 4, legHeight, 4, scale);
        leg2.setRotationPoint(3.0F, 24 - legHeight, 7.0F);
        leg3 = new ModelPart(0, 16);
        leg3.addBox(-2.0F, 0.0F, -2.0F, 4, legHeight, 4, scale);
        leg3.setRotationPoint(-3.0F, 24 - legHeight, -5.0F);
        leg4 = new ModelPart(0, 16);
        leg4.addBox(-2.0F, 0.0F, -2.0F, 4, legHeight, 4, scale);
        leg4.setRotationPoint(3.0F, 24 - legHeight, -5.0F);
    }

    public override void render(float limbSwing, float limbSwingAmount, float ageInTicks, float netHeadYaw, float headPitch, float scale)
    {
        setRotationAngles(limbSwing, limbSwingAmount, ageInTicks, netHeadYaw, headPitch, scale);
        head.render(scale);
        body.render(scale);
        leg1.render(scale);
        leg2.render(scale);
        leg3.render(scale);
        leg4.render(scale);
    }

    public override void setRotationAngles(float limbSwing, float limbSwingAmount, float ageInTicks, float netHeadYaw, float headPitch, float scale)
    {
        head.rotateAngleX = headPitch / (180.0F / (float)Math.PI);
        head.rotateAngleY = netHeadYaw / (180.0F / (float)Math.PI);
        body.rotateAngleX = (float)Math.PI * 0.5F;
        leg1.rotateAngleX = MathHelper.Cos(limbSwing * 0.6662F) * 1.4F * limbSwingAmount;
        leg2.rotateAngleX = MathHelper.Cos(limbSwing * 0.6662F + (float)Math.PI) * 1.4F * limbSwingAmount;
        leg3.rotateAngleX = MathHelper.Cos(limbSwing * 0.6662F + (float)Math.PI) * 1.4F * limbSwingAmount;
        leg4.rotateAngleX = MathHelper.Cos(limbSwing * 0.6662F) * 1.4F * limbSwingAmount;
    }
}
