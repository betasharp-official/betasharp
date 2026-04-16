using BetaSharp.Client.Rendering.Core;
using BetaSharp.Client.Rendering.Entities.Models;
using BetaSharp.Client.Rendering.Legacy;
using BetaSharp.Entities;

namespace BetaSharp.Client.Rendering.Entities;

public class SlimeEntityRenderer : LivingEntityRenderer
{
    private readonly ModelBase scaleAmount;

    public SlimeEntityRenderer(ModelBase mainModel, ModelBase var2, float var3) : base(mainModel, var3)
    {
        scaleAmount = var2;
    }

    protected bool renderSlimePassModel(EntitySlime var1, int var2, float var3)
    {
        if (var2 == 0)
        {
            setRenderPassModel(scaleAmount);
            Scene.Enable(SceneRenderCapability.Blend);
            Scene.SetBlendFunction(SceneBlendFactor.SrcAlpha, SceneBlendFactor.OneMinusSrcAlpha);
            return true;
        }
        else
        {
            if (var2 == 1)
            {
                Scene.Disable(SceneRenderCapability.Blend);
                Scene.SetColor(1.0F, 1.0F, 1.0F, 1.0F);
            }

            return false;
        }
    }

    protected void scaleSlime(EntitySlime var1, float var2)
    {
        int var3 = var1.getSlimeSize();
        float var4 = (var1.prevSquishAmount + (var1.squishAmount - var1.prevSquishAmount) * var2) /
                     (var3 * 0.5F + 1.0F);
        float var5 = 1.0F / (var4 + 1.0F);
        float var6 = var3;
        Scene.Scale(var5 * var6, 1.0F / var5 * var6, var5 * var6);
    }

    protected override void PreRenderCallback(EntityLiving var1, float var2)
    {
        scaleSlime((EntitySlime)var1, var2);
    }

    protected override bool ShouldRenderPass(EntityLiving var1, int var2, float var3)
    {
        return renderSlimePassModel((EntitySlime)var1, var2, var3);
    }
}
