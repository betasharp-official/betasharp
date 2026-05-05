namespace BetaSharp.Client.Rendering.Entities.Models;

public class SheepModel : ModelQuadruped
{

    public SheepModel() : base(12, 0.0f)
    {
        head = new ModelPart(0, 0);
        head.addBox(-3.0F, -4.0F, -6.0F, 6, 6, 8, 0.0F);
        head.setRotationPoint(0.0F, 6.0F, -8.0F);
        body = new ModelPart(28, 8);
        body.addBox(-4.0F, -10.0F, -7.0F, 8, 16, 6, 0.0F);
        body.setRotationPoint(0.0F, 5.0F, 2.0F);
    }
}
