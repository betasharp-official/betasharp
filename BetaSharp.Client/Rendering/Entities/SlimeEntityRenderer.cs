using BetaSharp.Client.Rendering.Core;
using BetaSharp.Client.Rendering.Core.OpenGL;
using BetaSharp.Client.Rendering.Entities.Models;
using BetaSharp.Entities;

namespace BetaSharp.Client.Rendering.Entities;

public class SlimeEntityRenderer : LivingEntityRenderer
{

    private readonly ModelBase scaleAmount;

    public SlimeEntityRenderer(ModelBase mainModel, ModelBase slimeOverlayModel, float shadowRadius) : base(mainModel, shadowRadius)
    {
        scaleAmount = slimeOverlayModel;
    }

    protected bool renderSlimePassModel(EntitySlime slimeEntity, int renderPass, float tickDelta)
    {
        if (renderPass == 0)
        {
            setRenderPassModel(scaleAmount);
            //GLManager.GL.Enable(GLEnum.Normalize);
            GLManager.GL.Enable(GLEnum.Blend);
            GLManager.GL.BlendFunc(GLEnum.SrcAlpha, GLEnum.OneMinusSrcAlpha);
            return true;
        }
        else
        {
            if (renderPass == 1)
            {
                GLManager.GL.Disable(GLEnum.Blend);
                GLManager.GL.Color4(1.0F, 1.0F, 1.0F, 1.0F);
            }

            return false;
        }
    }

    protected void scaleSlime(EntitySlime slimeEntity, float tickDelta)
    {
        int slimeSize = slimeEntity.SlimeSize;
        float squish = (slimeEntity.PrevSquishAmount + (slimeEntity.SquishAmount - slimeEntity.PrevSquishAmount) * tickDelta) / (slimeSize * 0.5F + 1.0F);
        float inverseSquish = 1.0F / (squish + 1.0F);
        float baseScale = slimeSize;
        GLManager.GL.Scale(inverseSquish * baseScale, 1.0F / inverseSquish * baseScale, inverseSquish * baseScale);
    }

    protected override void PreRenderCallback(EntityLiving entity, float tickDelta)
    {
        scaleSlime((EntitySlime)entity, tickDelta);
    }

    protected override bool ShouldRenderPass(EntityLiving entity, int renderPass, float tickDelta)
    {
        return renderSlimePassModel((EntitySlime)entity, renderPass, tickDelta);
    }
}
