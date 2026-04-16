using BetaSharp.Client.Guis;
using BetaSharp.Client.Rendering.Core;
using BetaSharp.Client.Rendering.Entities.Models;
using BetaSharp.Client.Rendering.Legacy;
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

    public void setRenderPassModel(ModelBase renderPassModel)
    {
        this.renderPassModel = renderPassModel;
    }

    public virtual void DoRenderLiving(EntityLiving entity, double x, double y, double z, float yaw, float tickDelta)
    {
        Scene.PushMatrix();
        Scene.Disable(SceneRenderCapability.CullFace);
        mainModel.onGround = func_167_c(entity, tickDelta);
        if (renderPassModel != null)
        {
            renderPassModel.onGround = mainModel.onGround;
        }

        mainModel.isRiding = entity.hasVehicle();
        if (renderPassModel != null)
        {
            renderPassModel.isRiding = mainModel.isRiding;
        }

        try
        {
            float bodyYaw = entity.lastBodyYaw + (entity.bodyYaw - entity.lastBodyYaw) * tickDelta;
            float headYaw = entity.prevYaw + (entity.yaw - entity.prevYaw) * tickDelta;
            float pitch = entity.prevPitch + (entity.pitch - entity.prevPitch) * tickDelta;
            Func_22012_b(entity, x, y, z);
            float animationProgress = getAnimationProgress(entity, tickDelta);
            RotateCorpse(entity, animationProgress, bodyYaw, tickDelta);
            float modelScale = 1.0F / 16.0F;
            Scene.Enable(SceneRenderCapability.RescaleNormal);
            Scene.Scale(-1.0F, -1.0F, 1.0F);
            PreRenderCallback(entity, tickDelta);
            Scene.Translate(0.0F, -24.0F * modelScale - (1 / 128f), 0.0F);
            float walkAnimationSpeed = entity.lastWalkAnimationSpeed +
                                       (entity.walkAnimationSpeed - entity.lastWalkAnimationSpeed) * tickDelta;
            float walkAnimationPhase = entity.animationPhase - entity.walkAnimationSpeed * (1.0F - tickDelta);
            if (walkAnimationSpeed > 1.0F)
            {
                walkAnimationSpeed = 1.0F;
            }

            LoadDownloadableImageTexture((entity as EntityPlayer)?.name, entity.getTexture());
            Scene.Enable(SceneRenderCapability.AlphaTest);
            mainModel.setLivingAnimations(entity, walkAnimationPhase, walkAnimationSpeed, tickDelta, Scene);
            mainModel.render(Scene, walkAnimationPhase, walkAnimationSpeed, animationProgress, headYaw - bodyYaw, pitch,
                modelScale);

            for (int renderPass = 0; renderPass < 4; ++renderPass)
            {
                if (ShouldRenderPass(entity, renderPass, tickDelta))
                {
                    renderPassModel.render(Scene, walkAnimationPhase, walkAnimationSpeed, animationProgress,
                        headYaw - bodyYaw, pitch, modelScale);
                    Scene.Disable(SceneRenderCapability.Blend);
                    Scene.Enable(SceneRenderCapability.AlphaTest);
                }
            }

            RenderMore(entity, tickDelta);
            float brightness = entity.getBrightnessAtEyes(tickDelta);
            int colorMultiplier = getColorMultiplier(entity, brightness, tickDelta);
            if ((colorMultiplier >> 24 & 255) > 0 || entity.hurtTime > 0 || entity.deathTime > 0)
            {
                Scene.Disable(SceneRenderCapability.Texture2D);
                Scene.Disable(SceneRenderCapability.AlphaTest);
                Scene.Enable(SceneRenderCapability.Blend);
                Scene.SetBlendFunction(SceneBlendFactor.SrcAlpha, SceneBlendFactor.OneMinusSrcAlpha);
                Scene.SetDepthFunction(SceneDepthFunction.Equal);
                if (entity.hurtTime > 0 || entity.deathTime > 0)
                {
                    Scene.SetColor(brightness, 0.0F, 0.0F, 0.4F);
                    mainModel.render(Scene, walkAnimationPhase, walkAnimationSpeed, animationProgress,
                        headYaw - bodyYaw, pitch, modelScale);

                    for (int renderPass = 0; renderPass < 4; ++renderPass)
                    {
                        if (func_27005_b(entity, renderPass, tickDelta))
                        {
                            Scene.SetColor(brightness, 0.0F, 0.0F, 0.4F);
                            renderPassModel.render(Scene, walkAnimationPhase, walkAnimationSpeed, animationProgress,
                                headYaw - bodyYaw, pitch, modelScale);
                        }
                    }
                }

                if ((colorMultiplier >> 24 & 255) > 0)
                {
                    float colorRed = (colorMultiplier >> 16 & 255) / 255.0F;
                    float colorGreen = (colorMultiplier >> 8 & 255) / 255.0F;
                    float colorBlue = (colorMultiplier & 255) / 255.0F;
                    float colorAlpha = (colorMultiplier >> 24 & 255) / 255.0F;
                    Scene.SetColor(colorRed, colorGreen, colorBlue, colorAlpha);
                    mainModel.render(Scene, walkAnimationPhase, walkAnimationSpeed, animationProgress,
                        headYaw - bodyYaw, pitch, modelScale);

                    for (int renderPass = 0; renderPass < 4; ++renderPass)
                    {
                        if (func_27005_b(entity, renderPass, tickDelta))
                        {
                            Scene.SetColor(colorRed, colorGreen, colorBlue, colorAlpha);
                            renderPassModel.render(Scene, walkAnimationPhase, walkAnimationSpeed, animationProgress,
                                headYaw - bodyYaw, pitch, modelScale);
                        }
                    }
                }

                Scene.SetDepthFunction(SceneDepthFunction.Lequal);
                Scene.Disable(SceneRenderCapability.Blend);
                Scene.Enable(SceneRenderCapability.AlphaTest);
                Scene.Enable(SceneRenderCapability.Texture2D);
            }

            Scene.Disable(SceneRenderCapability.RescaleNormal);
        }
        catch (Exception e)
        {
            _logger.LogError(e, e.Message);
        }

        Scene.PopMatrix();
        PassSpecialRender(entity, x, y, z);
    }

    protected virtual void Func_22012_b(EntityLiving entity, double x, double y, double z)
    {
        Scene.Translate((float)x, (float)y, (float)z);
    }

    protected virtual void RotateCorpse(EntityLiving entity, float animationProgress, float bodyYaw, float tickDelta)
    {
        Scene.Rotate(180.0F - bodyYaw, 0.0F, 1.0F, 0.0F);
        if (entity.deathTime > 0)
        {
            float deathAnimationProgress = (entity.deathTime + tickDelta - 1.0F) / 20.0F * 1.6F;
            deathAnimationProgress = MathHelper.Sqrt(deathAnimationProgress);
            if (deathAnimationProgress > 1.0F)
            {
                deathAnimationProgress = 1.0F;
            }

            Scene.Rotate(deathAnimationProgress * getDeathMaxRotation(entity), 0.0F, 0.0F, 1.0F);
        }
    }

    protected float func_167_c(EntityLiving entity, float tickDelta)
    {
        return entity.getSwingProgress(tickDelta);
    }

    protected virtual float getAnimationProgress(EntityLiving entity, float tickDelta)
    {
        return entity.age + tickDelta;
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
            renderLivingLabel(entity, entity.id.ToString(), x, y, z, 64);
        }
    }

    protected void renderLivingLabel(EntityLiving entity, string labelText, double x, double y, double z,
        int maxDistance)
    {
        float distanceToCamera = entity.getDistance(Dispatcher.CameraEntity);
        if (distanceToCamera <= maxDistance)
        {
            ITextRenderer textRenderer = TextRenderer;
            float labelBaseScale = 1.6F;
            float labelScale = (float)(1.0D / 60.0D) * labelBaseScale;
            Scene.PushMatrix();
            Scene.Translate((float)x + 0.0F, (float)y + 2.3F, (float)z);
            Scene.SetNormal(0.0F, 1.0F, 0.0F);
            Scene.Rotate(-Dispatcher.PlayerViewY, 0.0F, 1.0F, 0.0F);
            Scene.Rotate(Dispatcher.PlayerViewX, 1.0F, 0.0F, 0.0F);
            Scene.Scale(-labelScale, -labelScale, labelScale);
            Scene.Disable(SceneRenderCapability.Lighting);
            Scene.SetDepthMask(false);
            Scene.Disable(SceneRenderCapability.DepthTest);
            Scene.Enable(SceneRenderCapability.Blend);
            Scene.SetBlendFunction(SceneBlendFactor.SrcAlpha, SceneBlendFactor.OneMinusSrcAlpha);
            Tessellator tessellator = Tessellator.instance;
            int verticalOffset = 0;
            if (labelText.Equals("deadmau5"))
            {
                verticalOffset = -10;
            }

            Scene.Disable(SceneRenderCapability.Texture2D);
            tessellator.startDrawingQuads();
            int halfLabelWidth = textRenderer.GetStringWidth(labelText) / 2;
            tessellator.setColorRGBA_F(0.0F, 0.0F, 0.0F, 0.25F);
            tessellator.addVertex(-halfLabelWidth - 1, -1 + verticalOffset, 0.0D);
            tessellator.addVertex(-halfLabelWidth - 1, 8 + verticalOffset, 0.0D);
            tessellator.addVertex(halfLabelWidth + 1, 8 + verticalOffset, 0.0D);
            tessellator.addVertex(halfLabelWidth + 1, -1 + verticalOffset, 0.0D);
            tessellator.draw();
            Scene.Enable(SceneRenderCapability.Texture2D);
            textRenderer.DrawString(labelText, -textRenderer.GetStringWidth(labelText) / 2, verticalOffset,
                Color.WhiteAlpha20);
            Scene.Enable(SceneRenderCapability.DepthTest);
            Scene.SetDepthMask(true);
            textRenderer.DrawString(labelText, -textRenderer.GetStringWidth(labelText) / 2, verticalOffset,
                Color.WhiteAlpha20);
            Scene.Enable(SceneRenderCapability.Lighting);
            Scene.Disable(SceneRenderCapability.Blend);
            Scene.SetColor(1.0F, 1.0F, 1.0F, 1.0F);
            Scene.PopMatrix();
        }
    }

    public override void Render(Entity target, double x, double y, double z, float yaw, float tickDelta)
    {
        DoRenderLiving((EntityLiving)target, x, y, z, yaw, tickDelta);
    }
}
