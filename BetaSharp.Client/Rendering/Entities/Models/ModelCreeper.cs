using BetaSharp.Util.Maths;

namespace BetaSharp.Client.Rendering.Entities.Models;

public class ModelCreeper : ModelBase
{

    public ModelPart head;
    public ModelPart field_1270_b;
    public ModelPart body;
    public ModelPart leg1;
    public ModelPart leg2;
    public ModelPart leg3;
    public ModelPart leg4;

    public ModelCreeper() : this(0.0f)
    {
    }

    public ModelCreeper(float scale)
    {
        byte yOffset = 4;
        head = new ModelPart(0, 0);
        head.addBox(-4.0F, -8.0F, -4.0F, 8, 8, 8, scale);
        head.setRotationPoint(0.0F, yOffset, 0.0F);
        field_1270_b = new ModelPart(32, 0);
        field_1270_b.addBox(-4.0F, -8.0F, -4.0F, 8, 8, 8, scale + 0.5F);
        field_1270_b.setRotationPoint(0.0F, yOffset, 0.0F);
        body = new ModelPart(16, 16);
        body.addBox(-4.0F, 0.0F, -2.0F, 8, 12, 4, scale);
        body.setRotationPoint(0.0F, yOffset, 0.0F);
        leg1 = new ModelPart(0, 16);
        leg1.addBox(-2.0F, 0.0F, -2.0F, 4, 6, 4, scale);
        leg1.setRotationPoint(-2.0F, 12 + yOffset, 4.0F);
        leg2 = new ModelPart(0, 16);
        leg2.addBox(-2.0F, 0.0F, -2.0F, 4, 6, 4, scale);
        leg2.setRotationPoint(2.0F, 12 + yOffset, 4.0F);
        leg3 = new ModelPart(0, 16);
        leg3.addBox(-2.0F, 0.0F, -2.0F, 4, 6, 4, scale);
        leg3.setRotationPoint(-2.0F, 12 + yOffset, -4.0F);
        leg4 = new ModelPart(0, 16);
        leg4.addBox(-2.0F, 0.0F, -2.0F, 4, 6, 4, scale);
        leg4.setRotationPoint(2.0F, 12 + yOffset, -4.0F);
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
        head.rotateAngleY = netHeadYaw / (180.0F / (float)Math.PI);
        head.rotateAngleX = headPitch / (180.0F / (float)Math.PI);
        leg1.rotateAngleX = MathHelper.Cos(limbSwing * 0.6662F) * 1.4F * limbSwingAmount;
        leg2.rotateAngleX = MathHelper.Cos(limbSwing * 0.6662F + (float)Math.PI) * 1.4F * limbSwingAmount;
        leg3.rotateAngleX = MathHelper.Cos(limbSwing * 0.6662F + (float)Math.PI) * 1.4F * limbSwingAmount;
        leg4.rotateAngleX = MathHelper.Cos(limbSwing * 0.6662F) * 1.4F * limbSwingAmount;
    }
}
