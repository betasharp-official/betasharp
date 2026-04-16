using BetaSharp.Client.Rendering.Legacy;
using BetaSharp.Util.Maths;

namespace BetaSharp.Client.Rendering.Entities.Models;

public class ModelChicken : ModelBase
{
    public ModelPart head;
    public ModelPart body;
    public ModelPart rightLeg;
    public ModelPart leftLeg;
    public ModelPart rightWing;
    public ModelPart leftWing;
    public ModelPart bill;
    public ModelPart chin;

    public ModelChicken()
    {
        byte var1 = 16;
        head = new ModelPart(0, 0);
        head.addBox(-2.0F, -6.0F, -2.0F, 4, 6, 3, 0.0F);
        head.setRotationPoint(0.0F, -1 + var1, -4.0F);
        bill = new ModelPart(14, 0);
        bill.addBox(-2.0F, -4.0F, -4.0F, 4, 2, 2, 0.0F);
        bill.setRotationPoint(0.0F, -1 + var1, -4.0F);
        chin = new ModelPart(14, 4);
        chin.addBox(-1.0F, -2.0F, -3.0F, 2, 2, 2, 0.0F);
        chin.setRotationPoint(0.0F, -1 + var1, -4.0F);
        body = new ModelPart(0, 9);
        body.addBox(-3.0F, -4.0F, -3.0F, 6, 8, 6, 0.0F);
        body.setRotationPoint(0.0F, 0 + var1, 0.0F);
        rightLeg = new ModelPart(26, 0);
        rightLeg.addBox(-1.0F, 0.0F, -3.0F, 3, 5, 3);
        rightLeg.setRotationPoint(-2.0F, 3 + var1, 1.0F);
        leftLeg = new ModelPart(26, 0);
        leftLeg.addBox(-1.0F, 0.0F, -3.0F, 3, 5, 3);
        leftLeg.setRotationPoint(1.0F, 3 + var1, 1.0F);
        rightWing = new ModelPart(24, 13);
        rightWing.addBox(0.0F, 0.0F, -3.0F, 1, 4, 6);
        rightWing.setRotationPoint(-4.0F, -3 + var1, 0.0F);
        leftWing = new ModelPart(24, 13);
        leftWing.addBox(-1.0F, 0.0F, -3.0F, 1, 4, 6);
        leftWing.setRotationPoint(4.0F, -3 + var1, 0.0F);
    }

    public override void render(ILegacyFixedFunctionApi gl, float var1, float var2, float var3, float var4, float var5,
        float var6)
    {
        setRotationAngles(var1, var2, var3, var4, var5, var6);
        head.render(gl, var6);
        bill.render(gl, var6);
        chin.render(gl, var6);
        body.render(gl, var6);
        rightLeg.render(gl, var6);
        leftLeg.render(gl, var6);
        rightWing.render(gl, var6);
        leftWing.render(gl, var6);
    }

    public override void setRotationAngles(float var1, float var2, float var3, float var4, float var5, float var6)
    {
        head.rotateAngleX = -(var5 / (180.0F / (float)Math.PI));
        head.rotateAngleY = var4 / (180.0F / (float)Math.PI);
        bill.rotateAngleX = head.rotateAngleX;
        bill.rotateAngleY = head.rotateAngleY;
        chin.rotateAngleX = head.rotateAngleX;
        chin.rotateAngleY = head.rotateAngleY;
        body.rotateAngleX = (float)Math.PI * 0.5F;
        rightLeg.rotateAngleX = MathHelper.Cos(var1 * 0.6662F) * 1.4F * var2;
        leftLeg.rotateAngleX = MathHelper.Cos(var1 * 0.6662F + (float)Math.PI) * 1.4F * var2;
        rightWing.rotateAngleZ = var3;
        leftWing.rotateAngleZ = -var3;
    }
}
