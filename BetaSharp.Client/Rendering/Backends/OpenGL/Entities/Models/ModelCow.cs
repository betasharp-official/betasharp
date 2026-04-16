using BetaSharp.Client.Rendering.Legacy;

namespace BetaSharp.Client.Rendering.Entities.Models;

public class ModelCow : ModelQuadruped
{
    private readonly ModelPart udders;
    private readonly ModelPart horn1;
    private readonly ModelPart horn2;


    public ModelCow() : base(12, 0.0f)
    {
        head = new ModelPart(0, 0);
        head.addBox(-4.0F, -4.0F, -6.0F, 8, 8, 6, 0.0F);
        head.setRotationPoint(0.0F, 4.0F, -8.0F);
        horn1 = new ModelPart(22, 0);
        horn1.addBox(-4.0F, -5.0F, -4.0F, 1, 3, 1, 0.0F);
        horn1.setRotationPoint(0.0F, 3.0F, -7.0F);
        horn2 = new ModelPart(22, 0);
        horn2.addBox(3.0F, -5.0F, -4.0F, 1, 3, 1, 0.0F);
        horn2.setRotationPoint(0.0F, 3.0F, -7.0F);
        udders = new ModelPart(52, 0);
        udders.addBox(-2.0F, -3.0F, 0.0F, 4, 6, 2, 0.0F);
        udders.setRotationPoint(0.0F, 14.0F, 6.0F);
        udders.rotateAngleX = (float)Math.PI * 0.5F;
        body = new ModelPart(18, 4);
        body.addBox(-6.0F, -10.0F, -7.0F, 12, 18, 10, 0.0F);
        body.setRotationPoint(0.0F, 5.0F, 2.0F);
        --leg1.rotationPointX;
        ++leg2.rotationPointX;
        leg1.rotationPointZ += 0.0F;
        leg2.rotationPointZ += 0.0F;
        --leg3.rotationPointX;
        ++leg4.rotationPointX;
        --leg3.rotationPointZ;
        --leg4.rotationPointZ;
    }

    public override void render(ILegacyFixedFunctionApi gl, float var1, float var2, float var3, float var4, float var5,
        float var6)
    {
        base.render(gl, var1, var2, var3, var4, var5, var6);
        horn1.render(gl, var6);
        horn2.render(gl, var6);
        udders.render(gl, var6);
    }

    public override void setRotationAngles(float var1, float var2, float var3, float var4, float var5, float var6)
    {
        base.setRotationAngles(var1, var2, var3, var4, var5, var6);
        horn1.rotateAngleY = head.rotateAngleY;
        horn1.rotateAngleX = head.rotateAngleX;
        horn2.rotateAngleY = head.rotateAngleY;
        horn2.rotateAngleX = head.rotateAngleX;
    }
}
