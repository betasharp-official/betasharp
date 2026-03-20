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
        byte boatWidth = 24;
        byte sideHeight = 6;
        byte boatLength = 20;
        byte yOffset = 4;
        boatSides[0].addBox(-boatWidth / 2, -boatLength / 2 + 2, -3.0F, boatWidth, boatLength - 4, 4, 0.0F);
        boatSides[0].setRotationPoint(0.0F, 0 + yOffset, 0.0F);
        boatSides[1].addBox(-boatWidth / 2 + 2, -sideHeight - 1, -1.0F, boatWidth - 4, sideHeight, 2, 0.0F);
        boatSides[1].setRotationPoint(-boatWidth / 2 + 1, 0 + yOffset, 0.0F);
        boatSides[2].addBox(-boatWidth / 2 + 2, -sideHeight - 1, -1.0F, boatWidth - 4, sideHeight, 2, 0.0F);
        boatSides[2].setRotationPoint(boatWidth / 2 - 1, 0 + yOffset, 0.0F);
        boatSides[3].addBox(-boatWidth / 2 + 2, -sideHeight - 1, -1.0F, boatWidth - 4, sideHeight, 2, 0.0F);
        boatSides[3].setRotationPoint(0.0F, 0 + yOffset, -boatLength / 2 + 1);
        boatSides[4].addBox(-boatWidth / 2 + 2, -sideHeight - 1, -1.0F, boatWidth - 4, sideHeight, 2, 0.0F);
        boatSides[4].setRotationPoint(0.0F, 0 + yOffset, boatLength / 2 - 1);
        boatSides[0].rotateAngleX = (float)Math.PI * 0.5F;
        boatSides[1].rotateAngleY = (float)Math.PI * 3.0F / 2.0F;
        boatSides[2].rotateAngleY = (float)Math.PI * 0.5F;
        boatSides[3].rotateAngleY = (float)Math.PI;
    }

    public override void render(float limbSwing, float limbSwingAmount, float ageInTicks, float headYaw, float headPitch, float scale)
    {
        for (int i = 0; i < 5; ++i)
        {
            boatSides[i].render(scale);
        }

    }

    public override void setRotationAngles(float limbSwing, float limbSwingAmount, float ageInTicks, float headYaw, float headPitch, float scale)
    {
    }
}
