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

    public ModelQuadruped(int var1, float var2)
    {
        head.addBox(-4.0F, -4.0F, -8.0F, 8, 8, 8, var2);
        head.setRotationPoint(0.0F, 18 - var1, -6.0F);
        body = new ModelPart(28, 8);
        body.addBox(-5.0F, -10.0F, -7.0F, 10, 16, 8, var2);
        body.setRotationPoint(0.0F, 17 - var1, 2.0F);
        leg1 = new ModelPart(0, 16);
        leg1.addBox(-2.0F, 0.0F, -2.0F, 4, var1, 4, var2);
        leg1.setRotationPoint(-3.0F, 24 - var1, 7.0F);
        leg2 = new ModelPart(0, 16);
        leg2.addBox(-2.0F, 0.0F, -2.0F, 4, var1, 4, var2);
        leg2.setRotationPoint(3.0F, 24 - var1, 7.0F);
        leg3 = new ModelPart(0, 16);
        leg3.addBox(-2.0F, 0.0F, -2.0F, 4, var1, 4, var2);
        leg3.setRotationPoint(-3.0F, 24 - var1, -5.0F);
        leg4 = new ModelPart(0, 16);
        leg4.addBox(-2.0F, 0.0F, -2.0F, 4, var1, 4, var2);
        leg4.setRotationPoint(3.0F, 24 - var1, -5.0F);
    }

    public override void render(float var1, float var2, float var3, float var4, float var5, float var6)
    {
        setRotationAngles(var1, var2, var3, var4, var5, var6);
        head.render(var6);
        body.render(var6);
        leg1.render(var6);
        leg2.render(var6);
        leg3.render(var6);
        leg4.render(var6);
    }

    public override void setRotationAngles(float var1, float var2, float var3, float var4, float var5, float var6)
    {
        head.rotateAngleX = var5 / (180.0F / (float)Math.PI);
        head.rotateAngleY = var4 / (180.0F / (float)Math.PI);
        body.rotateAngleX = (float)Math.PI * 0.5F;
        leg1.rotateAngleX = MathHelper.Cos(var1 * 0.6662F) * 1.4F * var2;
        leg2.rotateAngleX = MathHelper.Cos(var1 * 0.6662F + (float)Math.PI) * 1.4F * var2;
        leg3.rotateAngleX = MathHelper.Cos(var1 * 0.6662F + (float)Math.PI) * 1.4F * var2;
        leg4.rotateAngleX = MathHelper.Cos(var1 * 0.6662F) * 1.4F * var2;
    }
}
