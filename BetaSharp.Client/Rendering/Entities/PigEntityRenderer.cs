using BetaSharp.Client.Rendering.Entities.Models;
using BetaSharp.Entities;

namespace BetaSharp.Client.Rendering.Entities;

public class PigEntityRenderer : LivingEntityRenderer
{

    public PigEntityRenderer(ModelBase mainModel, ModelBase saddleModel, float shadowRadius) : base(mainModel, shadowRadius)
    {
        setRenderPassModel(saddleModel);
    }

    protected bool renderSaddledPig(EntityPig pigEntity, int renderPass, float tickDelta)
    {
        loadTexture("/mob/saddle.png");
        return renderPass == 0 && pigEntity.Saddled.Value;
    }

    protected override bool ShouldRenderPass(EntityLiving entity, int renderPass, float tickDelta)
    {
        return renderSaddledPig((EntityPig)entity, renderPass, tickDelta);
    }
}
