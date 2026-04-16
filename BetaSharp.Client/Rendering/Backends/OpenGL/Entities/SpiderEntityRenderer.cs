using BetaSharp.Client.Rendering.Core;
using BetaSharp.Client.Rendering.Entities.Models;
using BetaSharp.Client.Rendering.Legacy;
using BetaSharp.Entities;

namespace BetaSharp.Client.Rendering.Entities;

public class SpiderEntityRenderer : LivingEntityRenderer
{
    public SpiderEntityRenderer() : base(new ModelSpider(), 1.0F)
    {
        setRenderPassModel(new ModelSpider());
    }

    protected float setSpiderDeathMaxRotation(EntitySpider var1)
    {
        return 180.0F;
    }

    protected bool setSpiderEyeBrightness(EntitySpider var1, int var2, float var3)
    {
        if (var2 != 0)
        {
            return false;
        }
        else if (var2 != 0)
        {
            return false;
        }
        else
        {
            loadTexture("/mob/spider_eyes.png");
            float var4 = (1.0F - var1.getBrightnessAtEyes(1.0F)) * 0.5F;
            Scene.Enable(SceneRenderCapability.Blend);
            Scene.Disable(SceneRenderCapability.AlphaTest);
            Scene.SetBlendFunction(SceneBlendFactor.SrcAlpha, SceneBlendFactor.OneMinusSrcAlpha);
            Scene.SetColor(1.0F, 1.0F, 1.0F, var4);
            return true;
        }
    }

    protected override float getDeathMaxRotation(EntityLiving var1)
    {
        return setSpiderDeathMaxRotation((EntitySpider)var1);
    }

    protected override bool ShouldRenderPass(EntityLiving var1, int var2, float var3)
    {
        return setSpiderEyeBrightness((EntitySpider)var1, var2, var3);
    }
}
