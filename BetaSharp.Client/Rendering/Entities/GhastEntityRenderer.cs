using BetaSharp.Client.Rendering.Core;
using BetaSharp.Client.Rendering.Entities.Models;
using BetaSharp.Entities;

namespace BetaSharp.Client.Rendering.Entities;

public class GhastEntityRenderer : LivingEntityRenderer
{

    public GhastEntityRenderer() : base(new ModelGhast(), 0.5F)
    {
    }

    protected void render(EntityGhast ghastEntity, float tickDelta)
    {
        float attackProgress = (ghastEntity.prevAttackCounter + (ghastEntity.attackCounter - ghastEntity.prevAttackCounter) * tickDelta) / 20.0F;
        if (attackProgress < 0.0F)
        {
            attackProgress = 0.0F;
        }

        attackProgress = 1.0F / (attackProgress * attackProgress * attackProgress * attackProgress * attackProgress * 2.0F + 1.0F);
        float scaleY = (8.0F + attackProgress) / 2.0F;
        float scaleXZ = (8.0F + 1.0F / attackProgress) / 2.0F;
        GLManager.GL.Scale(scaleXZ, scaleY, scaleXZ);
        GLManager.GL.Color4(1.0F, 1.0F, 1.0F, 1.0F);
    }

    protected override void PreRenderCallback(EntityLiving entity, float tickDelta)
    {
        render((EntityGhast)entity, tickDelta);
    }
}
