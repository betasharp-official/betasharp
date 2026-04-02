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

    public ModelCreeper(float var1)
    {
        byte var2 = 4;
        head = new ModelPart(0, 0);
        head.addBox(-4.0F, -8.0F, -4.0F, 8, 8, 8, var1);
        head.setRotationPoint(0.0F, var2, 0.0F);
        field_1270_b = new ModelPart(32, 0);
        field_1270_b.addBox(-4.0F, -8.0F, -4.0F, 8, 8, 8, var1 + 0.5F);
        field_1270_b.setRotationPoint(0.0F, var2, 0.0F);
        body = new ModelPart(16, 16);
        body.addBox(-4.0F, 0.0F, -2.0F, 8, 12, 4, var1);
        body.setRotationPoint(0.0F, var2, 0.0F);
        leg1 = new ModelPart(0, 16);
        leg1.addBox(-2.0F, 0.0F, -2.0F, 4, 6, 4, var1);
        leg1.setRotationPoint(-2.0F, 12 + var2, 4.0F);
        leg2 = new ModelPart(0, 16);
        leg2.addBox(-2.0F, 0.0F, -2.0F, 4, 6, 4, var1);
        leg2.setRotationPoint(2.0F, 12 + var2, 4.0F);
        leg3 = new ModelPart(0, 16);
        leg3.addBox(-2.0F, 0.0F, -2.0F, 4, 6, 4, var1);
        leg3.setRotationPoint(-2.0F, 12 + var2, -4.0F);
        leg4 = new ModelPart(0, 16);
        leg4.addBox(-2.0F, 0.0F, -2.0F, 4, 6, 4, var1);
        leg4.setRotationPoint(2.0F, 12 + var2, -4.0F);
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
        head.rotateAngleY = var4 / (180.0F / (float)Math.PI);
        head.rotateAngleX = var5 / (180.0F / (float)Math.PI);
        leg1.rotateAngleX = MathHelper.Cos(var1 * 0.6662F) * 1.4F * var2;
        leg2.rotateAngleX = MathHelper.Cos(var1 * 0.6662F + (float)Math.PI) * 1.4F * var2;
        leg3.rotateAngleX = MathHelper.Cos(var1 * 0.6662F + (float)Math.PI) * 1.4F * var2;
        leg4.rotateAngleX = MathHelper.Cos(var1 * 0.6662F) * 1.4F * var2;
    }
}
