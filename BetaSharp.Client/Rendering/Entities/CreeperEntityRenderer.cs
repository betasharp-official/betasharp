using BetaSharp.Client.Rendering.Core;
using BetaSharp.Client.Rendering.Core.OpenGL;
using BetaSharp.Client.Rendering.Entities.Models;
using BetaSharp.Entities;
using BetaSharp.Util.Maths;

namespace BetaSharp.Client.Rendering.Entities;

public class CreeperEntityRenderer : LivingEntityRenderer
{

    private readonly ModelBase model = new ModelCreeper(2.0F);

    public CreeperEntityRenderer() : base(new ModelCreeper(), 0.5F)
    {
    }

    protected void UpdateCreeperScale(EntityCreeper creeperEntity, float partialTick)
    {
        float progress = creeperEntity.GetCreeperFlashTime(partialTick);
        float pulse = 1.0F + MathHelper.Sin(progress * 100.0F) * progress * 0.01F;

        if (progress < 0.0F)
        {
            progress = 0.0F;
        }

        if (progress > 1.0F)
        {
            progress = 1.0F;
        }

        progress *= progress;
        progress *= progress;
        float scaleX = (1.0F + progress * 0.4F) * pulse;
        float scaleY = (1.0F + progress * 0.1F) / pulse;
        GLManager.GL.Scale(scaleX, scaleY, scaleX);
    }

    protected int UpdateCreeperColorMultiplier(EntityCreeper creeperEntity, float brightness, float tickDelta)
    {
        float progress = creeperEntity.GetCreeperFlashTime(tickDelta);
        if ((int)(progress * 10.0F) % 2 == 0)
        {
            return 0;
        }
        else
        {
            int a = (int)(progress * 0.2F * 255.0F);
            if (a < 0)
            {
                a = 0;
            }

            if (a > 255)
            {
                a = 255;
            }

            int r = 255;
            int g = 255;
            int b = 255;
            return a << 24 | r << 16 | g << 8 | b;
        }
    }

    protected bool func_27006_a(EntityCreeper creeperEntity, int renderPass, float tickDelta)
    {
        if (creeperEntity.Powered.Value)
        {
            if (renderPass == 1)
            {
                float animationTime = creeperEntity.Age + tickDelta;
                loadTexture("/armor/power.png");
                GLManager.GL.MatrixMode(GLEnum.Texture2D); //wtf?
                GLManager.GL.LoadIdentity();
                float textureOffsetX = animationTime * 0.01F;
                float textureOffsetY = animationTime * 0.01F;
                GLManager.GL.Translate(textureOffsetX, textureOffsetY, 0.0F);
                setRenderPassModel(model);
                GLManager.GL.MatrixMode(GLEnum.Modelview);
                GLManager.GL.Enable(GLEnum.Blend);
                float overlayBrightness = 0.5F;
                GLManager.GL.Color4(overlayBrightness, overlayBrightness, overlayBrightness, 1.0F);
                GLManager.GL.Disable(GLEnum.Lighting);
                GLManager.GL.BlendFunc(GLEnum.One, GLEnum.One);
                return true;
            }

            if (renderPass == 2)
            {
                GLManager.GL.MatrixMode(GLEnum.Texture);
                GLManager.GL.LoadIdentity();
                GLManager.GL.MatrixMode(GLEnum.Modelview);
                GLManager.GL.Enable(GLEnum.Lighting);
                GLManager.GL.Disable(GLEnum.Blend);
            }
        }

        return false;
    }

    protected bool func_27007_b(EntityCreeper creeperEntity, int renderPass, float tickDelta)
    {
        return false;
    }

    protected override void PreRenderCallback(EntityLiving entity, float partialTick)
    {
        UpdateCreeperScale((EntityCreeper)entity, partialTick);
    }

    protected override int getColorMultiplier(EntityLiving entity, float brightness, float tickDelta)
    {
        return UpdateCreeperColorMultiplier((EntityCreeper)entity, brightness, tickDelta);
    }

    protected override bool ShouldRenderPass(EntityLiving entity, int renderPass, float tickDelta)
    {
        return func_27006_a((EntityCreeper)entity, renderPass, tickDelta);
    }

    protected override bool func_27005_b(EntityLiving entity, int renderPass, float tickDelta)
    {
        return func_27007_b((EntityCreeper)entity, renderPass, tickDelta);
    }
}
