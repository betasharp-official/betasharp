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
        this.ShadowRadius = shadowRadius;
    }

    public void setRenderPassModel(ModelBase model)
    {
        renderPassModel = model;
    }

    public virtual void doRenderLiving(EntityLiving entity, double x, double y, double z, float yaw, float tickDelta)
    {
        GLManager.GL.PushMatrix();
        GLManager.GL.Disable(GLEnum.CullFace);
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
            float entityYaw = entity.prevYaw + (entity.yaw - entity.prevYaw) * tickDelta;
            float entityPitch = entity.prevPitch + (entity.pitch - entity.prevPitch) * tickDelta;
            func_22012_b(entity, x, y, z);
            float animationTimer = func_170_d(entity, tickDelta);
            rotateCorpse(entity, animationTimer, bodyYaw, tickDelta);
            float modelScale = 1.0F / 16.0F;
            GLManager.GL.Enable(GLEnum.RescaleNormal);
            GLManager.GL.Scale(-1.0F, -1.0F, 1.0F);
            preRenderCallback(entity, tickDelta);
            GLManager.GL.Translate(0.0F, -24.0F * modelScale - (1 / 128f), 0.0F);
            float walkSpeed = entity.lastWalkAnimationSpeed + (entity.walkAnimationSpeed - entity.lastWalkAnimationSpeed) * tickDelta;
            float animPhase = entity.animationPhase - entity.walkAnimationSpeed * (1.0F - tickDelta);
            if (walkSpeed > 1.0F)
            {
                walkSpeed = 1.0F;
            }

            LoadDownloadableImageTexture((entity as EntityPlayer)?.name, entity.getTexture());
            GLManager.GL.Enable(GLEnum.AlphaTest);
            mainModel.setLivingAnimations(entity, animPhase, walkSpeed, tickDelta);
            mainModel.render(animPhase, walkSpeed, animationTimer, entityYaw - bodyYaw, entityPitch, modelScale);

            for (int renderPass = 0; renderPass < 4; ++renderPass)
            {
                if (shouldRenderPass(entity, renderPass, tickDelta))
                {
                    renderPassModel.render(animPhase, walkSpeed, animationTimer, entityYaw - bodyYaw, entityPitch, modelScale);
                    GLManager.GL.Disable(GLEnum.Blend);
                    GLManager.GL.Enable(GLEnum.AlphaTest);
                }
            }

            renderMore(entity, tickDelta);
            float brightness = entity.getBrightnessAtEyes(tickDelta);
            int colorMultiplier = getColorMultiplier(entity, brightness, tickDelta);
            if ((colorMultiplier >> 24 & 255) > 0 || entity.hurtTime > 0 || entity.deathTime > 0)
            {
                GLManager.GL.Disable(GLEnum.Texture2D);
                GLManager.GL.Disable(GLEnum.AlphaTest);
                GLManager.GL.Enable(GLEnum.Blend);
                GLManager.GL.BlendFunc(GLEnum.SrcAlpha, GLEnum.OneMinusSrcAlpha);
                GLManager.GL.DepthFunc(GLEnum.Equal);
                if (entity.hurtTime > 0 || entity.deathTime > 0)
                {
                    GLManager.GL.Color4(brightness, 0.0F, 0.0F, 0.4F);
                    mainModel.render(animPhase, walkSpeed, animationTimer, entityYaw - bodyYaw, entityPitch, modelScale);

                    for (int renderPass = 0; renderPass < 4; ++renderPass)
                    {
                        if (func_27005_b(entity, renderPass, tickDelta))
                        {
                            GLManager.GL.Color4(brightness, 0.0F, 0.0F, 0.4F);
                            renderPassModel.render(animPhase, walkSpeed, animationTimer, entityYaw - bodyYaw, entityPitch, modelScale);
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
                    mainModel.render(animPhase, walkSpeed, animationTimer, entityYaw - bodyYaw, entityPitch, modelScale);

                    for (int renderPass = 0; renderPass < 4; ++renderPass)
                    {
                        if (func_27005_b(entity, renderPass, tickDelta))
                        {
                            GLManager.GL.Color4(red, green, blue, alpha);
                            renderPassModel.render(animPhase, walkSpeed, animationTimer, entityYaw - bodyYaw, entityPitch, modelScale);
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

        GLManager.GL.Enable(GLEnum.CullFace);
        GLManager.GL.PopMatrix();
        passSpecialRender(entity, x, y, z);
    }

    protected virtual void func_22012_b(EntityLiving entity, double x, double y, double z)
    {
        GLManager.GL.Translate((float)x, (float)y, (float)z);
    }

    protected virtual void rotateCorpse(EntityLiving entity, float animTimer, float bodyYaw, float tickDelta)
    {
        GLManager.GL.Rotate(180.0F - bodyYaw, 0.0F, 1.0F, 0.0F);
        if (entity.deathTime > 0)
        {
            float deathProgress = (entity.deathTime + tickDelta - 1.0F) / 20.0F * 1.6F;
            deathProgress = MathHelper.Sqrt(deathProgress);
            if (deathProgress > 1.0F)
            {
                deathProgress = 1.0F;
            }

            GLManager.GL.Rotate(deathProgress * getDeathMaxRotation(entity), 0.0F, 0.0F, 1.0F);
        }

    }

    protected float func_167_c(EntityLiving entity, float partialTicks)
    {
        return entity.getSwingProgress(partialTicks);
    }

    protected virtual float func_170_d(EntityLiving entity, float partialTicks)
    {
        return entity.age + partialTicks;
    }

    protected virtual void renderMore(EntityLiving entity, float partialTicks)
    {
    }

    protected virtual bool func_27005_b(EntityLiving entity, int renderPass, float partialTicks)
    {
        return shouldRenderPass(entity, renderPass, partialTicks);
    }

    protected virtual bool shouldRenderPass(EntityLiving entity, int renderPass, float partialTicks)
    {
        return false;
    }

    protected virtual float getDeathMaxRotation(EntityLiving entity)
    {
        return 90.0F;
    }

    protected virtual int getColorMultiplier(EntityLiving entity, float lightBrightness, float partialTicks)
    {
        return 0;
    }

    protected virtual void preRenderCallback(EntityLiving entity, float partialTicks)
    {
    }

    protected virtual void passSpecialRender(EntityLiving entity, double x, double y, double z)
    {
        if (BetaSharp.isDebugInfoEnabled())
        {
            renderLivingLabel(entity, entity.id.ToString(), x, y, z, 64);
        }

    }

    protected void renderLivingLabel(EntityLiving entity, string label, double x, double y, double z, int maxDistance)
    {
        float distance = entity.getDistance(Dispatcher.cameraEntity);
        if (distance <= maxDistance)
        {
            TextRenderer textRenderer = TextRenderer;
            float labelScale = 1.6F;
            float textScale = (float)(1.0D / 60.0D) * labelScale;
            GLManager.GL.PushMatrix();
            GLManager.GL.Translate((float)x + 0.0F, (float)y + 2.3F, (float)z);
            GLManager.GL.Normal3(0.0F, 1.0F, 0.0F);
            GLManager.GL.Rotate(-Dispatcher.playerViewY, 0.0F, 1.0F, 0.0F);
            GLManager.GL.Rotate(Dispatcher.playerViewX, 1.0F, 0.0F, 0.0F);
            GLManager.GL.Scale(-textScale, -textScale, textScale);
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
            int halfWidth = textRenderer.GetStringWidth(label) / 2;
            tessellator.setColorRGBA_F(0.0F, 0.0F, 0.0F, 0.25F);
            tessellator.addVertex(-halfWidth - 1, -1 + yOffset, 0.0D);
            tessellator.addVertex(-halfWidth - 1, 8 + yOffset, 0.0D);
            tessellator.addVertex(halfWidth + 1, 8 + yOffset, 0.0D);
            tessellator.addVertex(halfWidth + 1, -1 + yOffset, 0.0D);
            tessellator.draw();
            GLManager.GL.Enable(GLEnum.Texture2D);
            textRenderer.DrawString(label, -textRenderer.GetStringWidth(label) / 2, yOffset, Color.WhiteAlpha20);
            GLManager.GL.Enable(GLEnum.DepthTest);
            GLManager.GL.DepthMask(true);
            textRenderer.DrawString(label, -textRenderer.GetStringWidth(label) / 2, yOffset, Color.WhiteAlpha20);
            GLManager.GL.Enable(GLEnum.Lighting);
            GLManager.GL.Disable(GLEnum.Blend);
            GLManager.GL.Color4(1.0F, 1.0F, 1.0F, 1.0F);
            GLManager.GL.PopMatrix();
        }
    }

    public override void render(Entity target, double x, double y, double z, float yaw, float tickDelta)
    {
        doRenderLiving((EntityLiving)target, x, y, z, yaw, tickDelta);
    }
}
