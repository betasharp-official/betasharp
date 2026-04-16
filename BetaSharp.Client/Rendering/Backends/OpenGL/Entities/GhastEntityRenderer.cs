using BetaSharp.Client.Rendering.Core;
using BetaSharp.Client.Rendering.Entities.Models;
using BetaSharp.Entities;

namespace BetaSharp.Client.Rendering.Entities;

public class GhastEntityRenderer : LivingEntityRenderer
{
    public GhastEntityRenderer() : base(new ModelGhast(), 0.5F)
    {
    }

    protected void render(EntityGhast var1, float var2)
    {
        float var4 = (var1.prevAttackCounter + (var1.attackCounter - var1.prevAttackCounter) * var2) / 20.0F;
        if (var4 < 0.0F)
        {
            var4 = 0.0F;
        }

        var4 = 1.0F / (var4 * var4 * var4 * var4 * var4 * 2.0F + 1.0F);
        float var5 = (8.0F + var4) / 2.0F;
        float var6 = (8.0F + 1.0F / var4) / 2.0F;
        Scene.Scale(var6, var5, var6);
        Scene.SetColor(1.0F, 1.0F, 1.0F, 1.0F);
    }

    protected override void PreRenderCallback(EntityLiving var1, float var2)
    {
        render((EntityGhast)var1, var2);
    }
}
