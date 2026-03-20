using BetaSharp.Client.Rendering.Core;
using BetaSharp.Client.Rendering.Entities.Models;
using BetaSharp.Entities;

namespace BetaSharp.Client.Rendering.Entities;

public class GiantEntityRenderer : LivingEntityRenderer
{

    private readonly float scale;

    public GiantEntityRenderer(ModelBase mainModel, float shadowRadius, float entityScale) : base(mainModel, shadowRadius * entityScale)
    {
        scale = entityScale;
    }

    protected void preRenderScale(EntityGiantZombie entity, float partialTicks)
    {
        GLManager.GL.Scale(scale, scale, scale);
    }

    protected override void preRenderCallback(EntityLiving entity, float partialTicks)
    {
        preRenderScale((EntityGiantZombie)entity, partialTicks);
    }
}