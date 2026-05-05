namespace BetaSharp.Client.Rendering.Entities.Models;

public class ModelBoat : ModelBase
{

    public ModelPart[] boatSides = new ModelPart[5];

    public ModelBoat()
    {
        boatSides[0] = new ModelPart(0, 8);
        boatSides[1] = new ModelPart(0, 0);
        boatSides[2] = new ModelPart(0, 0);
        boatSides[3] = new ModelPart(0, 0);
        boatSides[4] = new ModelPart(0, 0);
        byte width = 24;
        byte height = 6;
        byte length = 20;
        byte yOffset = 4;
        boatSides[0].addBox(-width / 2, -length / 2 + 2, -3.0F, width, length - 4, 4, 0.0F);
        boatSides[0].setRotationPoint(0.0F, 0 + yOffset, 0.0F);
        boatSides[1].addBox(-width / 2 + 2, -height - 1, -1.0F, width - 4, height, 2, 0.0F);
        boatSides[1].setRotationPoint(-width / 2 + 1, 0 + yOffset, 0.0F);
        boatSides[2].addBox(-width / 2 + 2, -height - 1, -1.0F, width - 4, height, 2, 0.0F);
        boatSides[2].setRotationPoint(width / 2 - 1, 0 + yOffset, 0.0F);
        boatSides[3].addBox(-width / 2 + 2, -height - 1, -1.0F, width - 4, height, 2, 0.0F);
        boatSides[3].setRotationPoint(0.0F, 0 + yOffset, -length / 2 + 1);
        boatSides[4].addBox(-width / 2 + 2, -height - 1, -1.0F, width - 4, height, 2, 0.0F);
        boatSides[4].setRotationPoint(0.0F, 0 + yOffset, length / 2 - 1);
        boatSides[0].rotateAngleX = (float)Math.PI * 0.5F;
        boatSides[1].rotateAngleY = (float)Math.PI * 3.0F / 2.0F;
        boatSides[2].rotateAngleY = (float)Math.PI * 0.5F;
        boatSides[3].rotateAngleY = (float)Math.PI;
    }

    public override void render(float limbSwing, float limbSwingAmount, float animationProgress, float netHeadYaw, float headPitch, float scale)
    {
        for (int sideIndex = 0; sideIndex < 5; ++sideIndex)
        {
            boatSides[sideIndex].render(scale);
        }

    }

    public override void setRotationAngles(float limbSwing, float limbSwingAmount, float animationProgress, float netHeadYaw, float headPitch, float scale)
    {
    }
}
