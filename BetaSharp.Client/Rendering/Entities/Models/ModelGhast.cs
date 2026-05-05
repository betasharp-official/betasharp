using BetaSharp.Util.Maths;

namespace BetaSharp.Client.Rendering.Entities.Models;

public class ModelGhast : ModelBase
{
    private readonly ModelPart body;
    private readonly ModelPart[] tentacles = new ModelPart[9];

    public ModelGhast()
    {
        int yOffset = -16;
        body = new ModelPart(0, 0);
        body.addBox(-8.0F, -8.0F, -8.0F, 16, 16, 16);
        body.rotationPointY += 24 + yOffset;
        JavaRandom random = new(1660);

        for (int tentacleIndex = 0; tentacleIndex < tentacles.Length; ++tentacleIndex)
        {
            tentacles[tentacleIndex] = new ModelPart(0, 0);
            float tentacleX = ((tentacleIndex % 3 - tentacleIndex / 3 % 2 * 0.5F + 0.25F) / 2.0F * 2.0F - 1.0F) * 5.0F;
            float tentacleZ = (tentacleIndex / 3 / 2.0F * 2.0F - 1.0F) * 5.0F;
            int tentacleLength = random.NextInt(7) + 8;
            tentacles[tentacleIndex].addBox(-1.0F, 0.0F, -1.0F, 2, tentacleLength, 2);
            tentacles[tentacleIndex].rotationPointX = tentacleX;
            tentacles[tentacleIndex].rotationPointZ = tentacleZ;
            tentacles[tentacleIndex].rotationPointY = 31 + yOffset;
        }

    }

    public override void setRotationAngles(float limbSwing, float limbSwingAmount, float ageInTicks, float netHeadYaw, float headPitch, float scale)
    {
        for (int tentacleIndex = 0; tentacleIndex < tentacles.Length; ++tentacleIndex)
        {
            tentacles[tentacleIndex].rotateAngleX = 0.2F * MathHelper.Sin(ageInTicks * 0.3F + tentacleIndex) + 0.4F;
        }

    }

    public override void render(float limbSwing, float limbSwingAmount, float ageInTicks, float netHeadYaw, float headPitch, float scale)
    {
        setRotationAngles(limbSwing, limbSwingAmount, ageInTicks, netHeadYaw, headPitch, scale);
        body.render(scale);

        for (int tentacleIndex = 0; tentacleIndex < tentacles.Length; ++tentacleIndex)
        {
            tentacles[tentacleIndex].render(scale);
        }

    }
}
