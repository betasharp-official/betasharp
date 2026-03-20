using BetaSharp.Client.Rendering.Core;
using BetaSharp.Client.Rendering.Entities.Models;
using BetaSharp.Entities;

namespace BetaSharp.Client.Rendering.Entities;

public class GhastEntityRenderer : LivingEntityRenderer
{

    public GhastEntityRenderer() : base(new ModelGhast(), 0.5F)
    {
    }

    protected void render(EntityGhast entity, float partialTicks)
    {
        float attackFactor = (entity.prevAttackCounter + (entity.attackCounter - entity.prevAttackCounter) * partialTicks) / 20.0F;
        if (attackFactor < 0.0F)
        {
            attackFactor = 0.0F;
        }

        attackFactor = 1.0F / (attackFactor * attackFactor * attackFactor * attackFactor * attackFactor * 2.0F + 1.0F);
        float scaleXZ = (8.0F + attackFactor) / 2.0F;
        float scaleY = (8.0F + 1.0F / attackFactor) / 2.0F;
        GLManager.GL.Scale(scaleXZ, scaleY, scaleXZ);
        GLManager.GL.Color4(1.0F, 1.0F, 1.0F, 1.0F);
    }

    protected override void preRenderCallback(EntityLiving entity, float partialTicks)
    {
        render((EntityGhast)entity, partialTicks);
    }
}