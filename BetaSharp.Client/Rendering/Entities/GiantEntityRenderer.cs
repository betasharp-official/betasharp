using BetaSharp.Client.Rendering.Core;
using BetaSharp.Client.Rendering.Entities.Models;
using BetaSharp.Entities;

namespace BetaSharp.Client.Rendering.Entities;

public class GiantEntityRenderer : LivingEntityRenderer
{

    private readonly float scale;

    public GiantEntityRenderer(ModelBase mainModel, float shadowRadius, float scale) : base(mainModel, shadowRadius * scale)
    {
        this.scale = scale;
    }

    protected void preRenderScale(EntityGiantZombie giantEntity, float tickDelta)
    {
        GLManager.GL.Scale(scale, scale, scale);
    }

    protected override void PreRenderCallback(EntityLiving entity, float tickDelta)
    {
        preRenderScale((EntityGiantZombie)entity, tickDelta);
    }
}
