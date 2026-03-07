using BetaSharp.Client.Rendering.Entities.Models;
using BetaSharp.Entities;

namespace BetaSharp.Client.Rendering.Entities;

public class PigEntityRenderer : LivingEntityRenderer
{

    public PigEntityRenderer(ModelBase mainModel, ModelBase var2, float var3) : base(mainModel, var3)
    {
        setRenderPassModel(var2);
    }

    protected bool renderSaddledPig(EntityPig var1, int var2, float var3)
    {
        loadTexture("/mob/saddle.png");
        return var2 == 0 && var1.Saddled.Value;
    }

    protected override bool shouldRenderPass(EntityLiving var1, int var2, float var3)
    {
        return renderSaddledPig((EntityPig)var1, var2, var3);
    }
}
