namespace BetaSharp.Client.Rendering.Entities.Models;

public class ModelMinecart : ModelBase
{

    public ModelPart[] sideModels = new ModelPart[7];

    public ModelMinecart()
    {
        sideModels[0] = new ModelPart(0, 10);
        sideModels[1] = new ModelPart(0, 0);
        sideModels[2] = new ModelPart(0, 0);
        sideModels[3] = new ModelPart(0, 0);
        sideModels[4] = new ModelPart(0, 0);
        sideModels[5] = new ModelPart(44, 10);
        byte width = 20;
        byte height = 8;
        byte length = 16;
        byte yOffset = 4;
        sideModels[0].addBox(-width / 2, -length / 2, -1.0F, width, length, 2, 0.0F);
        sideModels[0].setRotationPoint(0.0F, 0 + yOffset, 0.0F);
        sideModels[5].addBox(-width / 2 + 1, -length / 2 + 1, -1.0F, width - 2, length - 2, 1, 0.0F);
        sideModels[5].setRotationPoint(0.0F, 0 + yOffset, 0.0F);
        sideModels[1].addBox(-width / 2 + 2, -height - 1, -1.0F, width - 4, height, 2, 0.0F);
        sideModels[1].setRotationPoint(-width / 2 + 1, 0 + yOffset, 0.0F);
        sideModels[2].addBox(-width / 2 + 2, -height - 1, -1.0F, width - 4, height, 2, 0.0F);
        sideModels[2].setRotationPoint(width / 2 - 1, 0 + yOffset, 0.0F);
        sideModels[3].addBox(-width / 2 + 2, -height - 1, -1.0F, width - 4, height, 2, 0.0F);
        sideModels[3].setRotationPoint(0.0F, 0 + yOffset, -length / 2 + 1);
        sideModels[4].addBox(-width / 2 + 2, -height - 1, -1.0F, width - 4, height, 2, 0.0F);
        sideModels[4].setRotationPoint(0.0F, 0 + yOffset, length / 2 - 1);
        sideModels[0].rotateAngleX = (float)Math.PI * 0.5F;
        sideModels[1].rotateAngleY = (float)Math.PI * 3.0F / 2.0F;
        sideModels[2].rotateAngleY = (float)Math.PI * 0.5F;
        sideModels[3].rotateAngleY = (float)Math.PI;
        sideModels[5].rotateAngleX = (float)Math.PI * -0.5F;
    }

    public override void render(float limbSwing, float limbSwingAmount, float animationProgress, float netHeadYaw, float headPitch, float scale)
    {
        sideModels[5].rotationPointY = 4.0F - animationProgress;

        for (int sideIndex = 0; sideIndex < 6; ++sideIndex)
        {
            sideModels[sideIndex].render(scale);
        }

    }

    public override void setRotationAngles(float limbSwing, float limbSwingAmount, float animationProgress, float netHeadYaw, float headPitch, float scale)
    {
    }
}
