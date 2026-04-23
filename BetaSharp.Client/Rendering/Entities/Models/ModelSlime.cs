namespace BetaSharp.Client.Rendering.Entities.Models;

public class ModelSlime : ModelBase
{
    private readonly ModelPart slimeBodies;
    private readonly ModelPart slimeRightEye;
    private readonly ModelPart slimeLeftEye;
    private readonly ModelPart slimeMouth;


    public ModelSlime(int textureOffsetY)
    {
        slimeBodies = new ModelPart(0, textureOffsetY);
        slimeBodies.addBox(-4.0F, 16.0F, -4.0F, 8, 8, 8);
        if (textureOffsetY > 0)
        {
            slimeBodies = new ModelPart(0, textureOffsetY);
            slimeBodies.addBox(-3.0F, 17.0F, -3.0F, 6, 6, 6);
            slimeRightEye = new ModelPart(32, 0);
            slimeRightEye.addBox(-3.25F, 18.0F, -3.5F, 2, 2, 2);
            slimeLeftEye = new ModelPart(32, 4);
            slimeLeftEye.addBox(1.25F, 18.0F, -3.5F, 2, 2, 2);
            slimeMouth = new ModelPart(32, 8);
            slimeMouth.addBox(0.0F, 21.0F, -3.5F, 1, 1, 1);
        }

    }

    public override void setRotationAngles(float limbSwing, float limbSwingAmount, float ageInTicks, float netHeadYaw, float headPitch, float scale)
    {
    }

    public override void render(float limbSwing, float limbSwingAmount, float ageInTicks, float netHeadYaw, float headPitch, float scale)
    {
        setRotationAngles(limbSwing, limbSwingAmount, ageInTicks, netHeadYaw, headPitch, scale);
        slimeBodies.render(scale);
        if (slimeRightEye != null)
        {
            slimeRightEye.render(scale);
            slimeLeftEye.render(scale);
            slimeMouth.render(scale);
        }

    }
}
