namespace BetaSharp.Client.Rendering.Entities.Models;

public class ModelSlime : ModelBase
{
    private readonly ModelPart slimeBodies;
    private readonly ModelPart slimeRightEye;
    private readonly ModelPart slimeLeftEye;
    private readonly ModelPart slimeMouth;


    public ModelSlime(int var1)
    {
        slimeBodies = new ModelPart(0, var1);
        slimeBodies.addBox(-4.0F, 16.0F, -4.0F, 8, 8, 8);
        if (var1 > 0)
        {
            slimeBodies = new ModelPart(0, var1);
            slimeBodies.addBox(-3.0F, 17.0F, -3.0F, 6, 6, 6);
            slimeRightEye = new ModelPart(32, 0);
            slimeRightEye.addBox(-3.25F, 18.0F, -3.5F, 2, 2, 2);
            slimeLeftEye = new ModelPart(32, 4);
            slimeLeftEye.addBox(1.25F, 18.0F, -3.5F, 2, 2, 2);
            slimeMouth = new ModelPart(32, 8);
            slimeMouth.addBox(0.0F, 21.0F, -3.5F, 1, 1, 1);
        }

    }

    public override void setRotationAngles(float var1, float var2, float var3, float var4, float var5, float var6)
    {
    }

    public override void render(float var1, float var2, float var3, float var4, float var5, float var6)
    {
        setRotationAngles(var1, var2, var3, var4, var5, var6);
        slimeBodies.render(var6);
        if (slimeRightEye != null)
        {
            slimeRightEye.render(var6);
            slimeLeftEye.render(var6);
            slimeMouth.render(var6);
        }

    }
}
