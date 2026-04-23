namespace BetaSharp.Client.Rendering.Entities.Models;

public class ModelSquid : ModelBase
{
    private readonly ModelPart squidBody;
    private readonly ModelPart[]
        squidTentacles = new ModelPart[8];

    public ModelSquid()
    {
        int yOffset = -16;
        squidBody = new ModelPart(0, 0);
        squidBody.addBox(-6.0F, -8.0F, -6.0F, 12, 16, 12);
        squidBody.rotationPointY += 24 + yOffset;

        for (int tentacleIndex = 0; tentacleIndex < squidTentacles.Length; ++tentacleIndex)
        {
            squidTentacles[tentacleIndex] = new ModelPart(48, 0);
            double tentacleAngle = tentacleIndex * Math.PI * 2.0D / squidTentacles.Length;
            float tentacleX = (float)Math.Cos(tentacleAngle) * 5.0F;
            float tentacleZ = (float)Math.Sin(tentacleAngle) * 5.0F;
            squidTentacles[tentacleIndex].addBox(-1.0F, 0.0F, -1.0F, 2, 18, 2);
            squidTentacles[tentacleIndex].rotationPointX = tentacleX;
            squidTentacles[tentacleIndex].rotationPointZ = tentacleZ;
            squidTentacles[tentacleIndex].rotationPointY = 31 + yOffset;
            tentacleAngle = tentacleIndex * Math.PI * -2.0D / squidTentacles.Length + Math.PI * 0.5D;
            squidTentacles[tentacleIndex].rotateAngleY = (float)tentacleAngle;
        }

    }

    public override void setRotationAngles(float limbSwing, float limbSwingAmount, float tentaclePitch, float netHeadYaw, float headPitch, float scale)
    {
        for (int tentacleIndex = 0; tentacleIndex < squidTentacles.Length; ++tentacleIndex)
        {
            squidTentacles[tentacleIndex].rotateAngleX = tentaclePitch;
        }

    }

    public override void render(float limbSwing, float limbSwingAmount, float tentaclePitch, float netHeadYaw, float headPitch, float scale)
    {
        setRotationAngles(limbSwing, limbSwingAmount, tentaclePitch, netHeadYaw, headPitch, scale);
        squidBody.render(scale);

        for (int tentacleIndex = 0; tentacleIndex < squidTentacles.Length; ++tentacleIndex)
        {
            squidTentacles[tentacleIndex].render(scale);
        }

    }
}
