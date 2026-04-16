using BetaSharp.Client.Rendering.Legacy;

namespace BetaSharp.Client.Rendering.Entities.Models;

public class SignModel
{
    public ModelPart signBoard = new(0, 0);
    public ModelPart signStick = new(0, 14);

    public SignModel()
    {
        signBoard.addBox(-12.0F, -14.0F, -1.0F, 24, 12, 2, 0.0F);
        signStick.addBox(-1.0F, -2.0F, -1.0F, 2, 14, 2, 0.0F);
    }

    public void Render(ILegacyFixedFunctionApi gl)
    {
        signBoard.render(gl, 1.0F / 16.0F);
        signStick.render(gl, 1.0F / 16.0F);
    }
}
