using BetaSharp.Client.Guis;
using BetaSharp.Client.Rendering.Core;
using BetaSharp.Client.Rendering.Core.OpenGL;
using BetaSharp.Client.Rendering.Entities.Models;
using BetaSharp.Entities;
using BetaSharp.Util.Maths;
using Microsoft.Extensions.Logging;
using Exception = System.Exception;

namespace BetaSharp.Client.Rendering.Entities;

public class LivingEntityRenderer : EntityRenderer
{

    protected ModelBase mainModel;
    protected ModelBase renderPassModel;
    private readonly ILogger<LivingEntityRenderer> _logger = Log.Instance.For<LivingEntityRenderer>();

    public LivingEntityRenderer(ModelBase mainModel, float shadowRadius)
    {
        this.mainModel = mainModel;
        ShadowRadius = shadowRadius;
    }

    public void setRenderPassModel(ModelBase model)
    {
        renderPassModel = model;
    }

    public virtual void DoRenderLiving(EntityLiving entity, double x, double y, double z, float yaw, float tickDelta)
    {
        GLManager.GL.PushMatrix();
        GLManager.GL.Disable(GLEnum.CullFace);
        mainModel.onGround = func_167_c(entity, tickDelta);
        if (renderPassModel != null)
        {
            renderPassModel.onGround = mainModel.onGround;
        }

        mainModel.isRiding = entity.HasVehicle();
        if (renderPassModel != null)
        {
            renderPassModel.isRiding = mainModel.isRiding;
        }

        try
        {
            float bodyYaw = entity.LastBodyYaw + (entity.BodyYaw - entity.LastBodyYaw) * tickDelta;
            float headYaw = entity.PrevYaw + (entity.Yaw - entity.PrevYaw) * tickDelta;
            float pitch = entity.PrevPitch + (entity.Pitch - entity.PrevPitch) * tickDelta;
            Func_22012_b(entity, x, y, z);
            float animationProgress = getAnimationProgress(entity, tickDelta);
            RotateCorpse(entity, animationProgress, bodyYaw, tickDelta);
            float modelScale = 1.0F / 16.0F;
            GLManager.GL.Enable(GLEnum.RescaleNormal);
            GLManager.GL.Scale(-1.0F, -1.0F, 1.0F);
            PreRenderCallback(entity, tickDelta);
            GLManager.GL.Translate(0.0F, -24.0F * modelScale - (1 / 128f), 0.0F);
            float walkSpeed = entity.LastWalkAnimationSpeed + (entity.WalkAnimationSpeed - entity.LastWalkAnimationSpeed) * tickDelta;
            float walkPhase = entity.AnimationPhase - entity.WalkAnimationSpeed * (1.0F - tickDelta);
            if (walkSpeed > 1.0F)
            {
                walkSpeed = 1.0F;
            }

            LoadDownloadableImageTexture((entity as EntityPlayer)?.name, entity.GetTexture());
            GLManager.GL.Enable(GLEnum.AlphaTest);
            mainModel.setLivingAnimations(entity, walkPhase, walkSpeed, tickDelta);
            mainModel.render(walkPhase, walkSpeed, animationProgress, headYaw - bodyYaw, pitch, modelScale);

            for (int renderPass = 0; renderPass < 4; ++renderPass)
            {
                if (ShouldRenderPass(entity, renderPass, tickDelta))
                {
                    renderPassModel.render(walkPhase, walkSpeed, animationProgress, headYaw - bodyYaw, pitch, modelScale);
                    GLManager.GL.Disable(GLEnum.Blend);
                    GLManager.GL.Enable(GLEnum.AlphaTest);
                }
            }

            RenderMore(entity, tickDelta);
            float brightness = entity.GetBrightnessAtEyes(tickDelta);
            int colorMultiplier = getColorMultiplier(entity, brightness, tickDelta);
            if ((colorMultiplier >> 24 & 255) > 0 || entity.HurtTime > 0 || entity.DeathTime > 0)
            {
                GLManager.GL.Disable(GLEnum.Texture2D);
                GLManager.GL.Disable(GLEnum.AlphaTest);
                GLManager.GL.Enable(GLEnum.Blend);
                GLManager.GL.BlendFunc(GLEnum.SrcAlpha, GLEnum.OneMinusSrcAlpha);
                GLManager.GL.DepthFunc(GLEnum.Equal);
                if (entity.HurtTime > 0 || entity.DeathTime > 0)
                {
                    GLManager.GL.Color4(brightness, 0.0F, 0.0F, 0.4F);
                    mainModel.render(walkPhase, walkSpeed, animationProgress, headYaw - bodyYaw, pitch, modelScale);

                    for (int damagePass = 0; damagePass < 4; ++damagePass)
                    {
                        if (func_27005_b(entity, damagePass, tickDelta))
                        {
                            GLManager.GL.Color4(brightness, 0.0F, 0.0F, 0.4F);
                            renderPassModel.render(walkPhase, walkSpeed, animationProgress, headYaw - bodyYaw, pitch, modelScale);
                        }
                    }
                }

                if ((colorMultiplier >> 24 & 255) > 0)
                {
                    float red = (colorMultiplier >> 16 & 255) / 255.0F;
                    float green = (colorMultiplier >> 8 & 255) / 255.0F;
                    float blue = (colorMultiplier & 255) / 255.0F;
                    float alpha = (colorMultiplier >> 24 & 255) / 255.0F;
                    GLManager.GL.Color4(red, green, blue, alpha);
                    mainModel.render(walkPhase, walkSpeed, animationProgress, headYaw - bodyYaw, pitch, modelScale);

                    for (int overlayPass = 0; overlayPass < 4; ++overlayPass)
                    {
                        if (func_27005_b(entity, overlayPass, tickDelta))
                        {
                            GLManager.GL.Color4(red, green, blue, alpha);
                            renderPassModel.render(walkPhase, walkSpeed, animationProgress, headYaw - bodyYaw, pitch, modelScale);
                        }
                    }
                }

                GLManager.GL.DepthFunc(GLEnum.Lequal);
                GLManager.GL.Disable(GLEnum.Blend);
                GLManager.GL.Enable(GLEnum.AlphaTest);
                GLManager.GL.Enable(GLEnum.Texture2D);
            }

            GLManager.GL.Disable(GLEnum.RescaleNormal);
        }
        catch (Exception e)
        {
            _logger.LogError(e, e.Message);
        }

        GLManager.GL.PopMatrix();
        PassSpecialRender(entity, x, y, z);
    }

    protected virtual void Func_22012_b(EntityLiving entity, double x, double y, double z)
    {
        GLManager.GL.Translate((float)x, (float)y, (float)z);
    }

    protected virtual void RotateCorpse(EntityLiving entity, float animationProgress, float bodyYaw, float tickDelta)
    {
        GLManager.GL.Rotate(180.0F - bodyYaw, 0.0F, 1.0F, 0.0F);
        if (entity.DeathTime > 0)
        {
            float deathRotation = (entity.DeathTime + tickDelta - 1.0F) / 20.0F * 1.6F;
            deathRotation = MathHelper.Sqrt(deathRotation);
            if (deathRotation > 1.0F)
            {
                deathRotation = 1.0F;
            }

            GLManager.GL.Rotate(deathRotation * getDeathMaxRotation(entity), 0.0F, 0.0F, 1.0F);
        }

    }

    protected float func_167_c(EntityLiving entity, float tickDelta)
    {
        return entity.getSwingProgress(tickDelta);
    }

    protected virtual float getAnimationProgress(EntityLiving entity, float tickDelta)
    {
        return entity.Age + tickDelta;
    }

    protected virtual void RenderMore(EntityLiving entity, float tickDelta)
    {
    }

    protected virtual bool func_27005_b(EntityLiving entity, int renderPass, float tickDelta)
    {
        return ShouldRenderPass(entity, renderPass, tickDelta);
    }

    protected virtual bool ShouldRenderPass(EntityLiving entity, int renderPass, float tickDelta)
    {
        return false;
    }

    protected virtual float getDeathMaxRotation(EntityLiving entity)
    {
        return 90.0F;
    }

    protected virtual int getColorMultiplier(EntityLiving entity, float brightness, float tickDelta)
    {
        return 0;
    }

    protected virtual void PreRenderCallback(EntityLiving entity, float tickDelta)
    {
    }

    protected virtual void PassSpecialRender(EntityLiving entity, double x, double y, double z)
    {
        if (Dispatcher.Options.ShowDebugInfo)
        {
            renderLivingLabel(entity, entity.ID.ToString(), x, y, z, 64);
        }

    }

    protected void renderLivingLabel(EntityLiving entity, string label, double x, double y, double z, int maxDistance)
    {
        float distance = entity.GetDistance(Dispatcher.CameraEntity);
        if (distance <= maxDistance)
        {
            TextRenderer fontRenderer = TextRenderer;
            float labelScale = 1.6F;
            float renderScale = (float)(1.0D / 60.0D) * labelScale;
            GLManager.GL.PushMatrix();
            GLManager.GL.Translate((float)x + 0.0F, (float)y + 2.3F, (float)z);
            GLManager.GL.Normal3(0.0F, 1.0F, 0.0F);
            GLManager.GL.Rotate(-Dispatcher.PlayerViewY, 0.0F, 1.0F, 0.0F);
            GLManager.GL.Rotate(Dispatcher.PlayerViewX, 1.0F, 0.0F, 0.0F);
            GLManager.GL.Scale(-renderScale, -renderScale, renderScale);
            GLManager.GL.Disable(GLEnum.Lighting);
            GLManager.GL.DepthMask(false);
            GLManager.GL.Disable(GLEnum.DepthTest);
            GLManager.GL.Enable(GLEnum.Blend);
            GLManager.GL.BlendFunc(GLEnum.SrcAlpha, GLEnum.OneMinusSrcAlpha);
            Tessellator tessellator = Tessellator.instance;
            int yOffset = 0;
            if (label.Equals("deadmau5"))
            {
                yOffset = -10;
            }

            GLManager.GL.Disable(GLEnum.Texture2D);
            tessellator.startDrawingQuads();
            int labelHalfWidth = fontRenderer.GetStringWidth(label) / 2;
            tessellator.setColorRGBA_F(0.0F, 0.0F, 0.0F, 0.25F);
            tessellator.addVertex(-labelHalfWidth - 1, -1 + yOffset, 0.0D);
            tessellator.addVertex(-labelHalfWidth - 1, 8 + yOffset, 0.0D);
            tessellator.addVertex(labelHalfWidth + 1, 8 + yOffset, 0.0D);
            tessellator.addVertex(labelHalfWidth + 1, -1 + yOffset, 0.0D);
            tessellator.draw();
            GLManager.GL.Enable(GLEnum.Texture2D);
            fontRenderer.DrawString(label, -fontRenderer.GetStringWidth(label) / 2, yOffset, Color.WhiteAlpha20);
            GLManager.GL.Enable(GLEnum.DepthTest);
            GLManager.GL.DepthMask(true);
            fontRenderer.DrawString(label, -fontRenderer.GetStringWidth(label) / 2, yOffset, Color.WhiteAlpha20);
            GLManager.GL.Enable(GLEnum.Lighting);
            GLManager.GL.Disable(GLEnum.Blend);
            GLManager.GL.Color4(1.0F, 1.0F, 1.0F, 1.0F);
            GLManager.GL.PopMatrix();
        }
    }

    public override void Render(Entity target, double x, double y, double z, float yaw, float tickDelta)
    {
        DoRenderLiving((EntityLiving)target, x, y, z, yaw, tickDelta);
    }
}
