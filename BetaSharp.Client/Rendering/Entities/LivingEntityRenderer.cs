using BetaSharp.Client.Guis;
using BetaSharp.Client.Rendering.Core;
using BetaSharp.Client.Rendering.Entities.Models;
using BetaSharp.Entities;
using BetaSharp.Util.Maths;
using java.lang;
using Microsoft.Extensions.Logging;
using Silk.NET.OpenGL.Legacy;
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

    public void setRenderPassModel(ModelBase var1)
    {
        renderPassModel = var1;
    }

    public virtual void doRenderLiving(EntityLiving var1, double var2, double var4, double var6, float var8, float var9)
    {
        GLManager.GL.PushMatrix();
        GLManager.GL.Disable(GLEnum.CullFace);
        mainModel.onGround = func_167_c(var1, var9);
        if (renderPassModel != null)
        {
            renderPassModel.onGround = mainModel.onGround;
        }

        mainModel.isRiding = var1.hasVehicle();
        if (renderPassModel != null)
        {
            renderPassModel.isRiding = mainModel.isRiding;
        }

        try
        {
            float var10 = var1.lastBodyYaw + (var1.bodyYaw - var1.lastBodyYaw) * var9;
            float var11 = var1.prevYaw + (var1.yaw - var1.prevYaw) * var9;
            float var12 = var1.prevPitch + (var1.pitch - var1.prevPitch) * var9;
            func_22012_b(var1, var2, var4, var6);
            float var13 = func_170_d(var1, var9);
            rotateCorpse(var1, var13, var10, var9);
            float var14 = 1.0F / 16.0F;
            GLManager.GL.Enable(GLEnum.RescaleNormal);
            GLManager.GL.Scale(-1.0F, -1.0F, 1.0F);
            preRenderCallback(var1, var9);
            GLManager.GL.Translate(0.0F, -24.0F * var14 - (1 / 128f), 0.0F);
            float var15 = var1.lastWalkAnimationSpeed + (var1.walkAnimationSpeed - var1.lastWalkAnimationSpeed) * var9;
            float var16 = var1.animationPhase - var1.walkAnimationSpeed * (1.0F - var9);
            if (var15 > 1.0F)
            {
                var15 = 1.0F;
            }

            LoadDownloadableImageTexture(var1.skinUrl, var1.getTexture());
            GLManager.GL.Enable(GLEnum.AlphaTest);
            mainModel.setLivingAnimations(var1, var16, var15, var9);
            mainModel.render(var16, var15, var13, var11 - var10, var12, var14);

            for (int var17 = 0; var17 < 4; ++var17)
            {
                if (shouldRenderPass(var1, var17, var9))
                {
                    renderPassModel.render(var16, var15, var13, var11 - var10, var12, var14);
                    GLManager.GL.Disable(GLEnum.Blend);
                    GLManager.GL.Enable(GLEnum.AlphaTest);
                }
            }

            renderMore(var1, var9);
            float var25 = var1.getBrightnessAtEyes(var9);
            int var18 = getColorMultiplier(var1, var25, var9);
            if ((var18 >> 24 & 255) > 0 || var1.hurtTime > 0 || var1.deathTime > 0)
            {
                GLManager.GL.Disable(GLEnum.Texture2D);
                GLManager.GL.Disable(GLEnum.AlphaTest);
                GLManager.GL.Enable(GLEnum.Blend);
                GLManager.GL.BlendFunc(GLEnum.SrcAlpha, GLEnum.OneMinusSrcAlpha);
                GLManager.GL.DepthFunc(GLEnum.Equal);
                if (var1.hurtTime > 0 || var1.deathTime > 0)
                {
                    GLManager.GL.Color4(var25, 0.0F, 0.0F, 0.4F);
                    mainModel.render(var16, var15, var13, var11 - var10, var12, var14);

                    for (int var19 = 0; var19 < 4; ++var19)
                    {
                        if (func_27005_b(var1, var19, var9))
                        {
                            GLManager.GL.Color4(var25, 0.0F, 0.0F, 0.4F);
                            renderPassModel.render(var16, var15, var13, var11 - var10, var12, var14);
                        }
                    }
                }

                if ((var18 >> 24 & 255) > 0)
                {
                    float var26 = (var18 >> 16 & 255) / 255.0F;
                    float var20 = (var18 >> 8 & 255) / 255.0F;
                    float var21 = (var18 & 255) / 255.0F;
                    float var22 = (var18 >> 24 & 255) / 255.0F;
                    GLManager.GL.Color4(var26, var20, var21, var22);
                    mainModel.render(var16, var15, var13, var11 - var10, var12, var14);

                    for (int var23 = 0; var23 < 4; ++var23)
                    {
                        if (func_27005_b(var1, var23, var9))
                        {
                            GLManager.GL.Color4(var26, var20, var21, var22);
                            renderPassModel.render(var16, var15, var13, var11 - var10, var12, var14);
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
        passSpecialRender(var1, var2, var4, var6);
    }

    protected virtual void func_22012_b(EntityLiving var1, double var2, double var4, double var6)
    {
        GLManager.GL.Translate((float)var2, (float)var4, (float)var6);
    }

    protected virtual void rotateCorpse(EntityLiving var1, float var2, float var3, float var4)
    {
        GLManager.GL.Rotate(180.0F - var3, 0.0F, 1.0F, 0.0F);
        if (var1.deathTime > 0)
        {
            float var5 = (var1.deathTime + var4 - 1.0F) / 20.0F * 1.6F;
            var5 = MathHelper.Sqrt(var5);
            if (var5 > 1.0F)
            {
                var5 = 1.0F;
            }

            GLManager.GL.Rotate(var5 * getDeathMaxRotation(var1), 0.0F, 0.0F, 1.0F);
        }

    }

    protected float func_167_c(EntityLiving var1, float var2)
    {
        return var1.getSwingProgress(var2);
    }

    protected virtual float func_170_d(EntityLiving var1, float var2)
    {
        return var1.age + var2;
    }

    protected virtual void renderMore(EntityLiving var1, float var2)
    {
    }

    protected virtual bool func_27005_b(EntityLiving var1, int var2, float var3)
    {
        return shouldRenderPass(var1, var2, var3);
    }

    protected virtual bool shouldRenderPass(EntityLiving var1, int var2, float var3)
    {
        return false;
    }

    protected virtual float getDeathMaxRotation(EntityLiving var1)
    {
        return 90.0F;
    }

    protected virtual int getColorMultiplier(EntityLiving var1, float var2, float var3)
    {
        return 0;
    }

    protected virtual void preRenderCallback(EntityLiving var1, float var2)
    {
    }

    protected virtual void passSpecialRender(EntityLiving var1, double var2, double var4, double var6)
    {
        if (BetaSharp.isDebugInfoEnabled())
        {
            renderLivingLabel(var1, Integer.toString(var1.id), var2, var4, var6, 64);
        }

    }

    protected void renderLivingLabel(EntityLiving var1, string var2, double var3, double var5, double var7, int var9)
    {
        float var10 = var1.getDistance(Dispatcher.cameraEntity);
        if (var10 <= var9)
        {
            TextRenderer var11 = TextRenderer;
            float var12 = 1.6F;
            float var13 = (float)(1.0D / 60.0D) * var12;
            GLManager.GL.PushMatrix();
            GLManager.GL.Translate((float)var3 + 0.0F, (float)var5 + 2.3F, (float)var7);
            GLManager.GL.Normal3(0.0F, 1.0F, 0.0F);
            GLManager.GL.Rotate(-Dispatcher.playerViewY, 0.0F, 1.0F, 0.0F);
            GLManager.GL.Rotate(Dispatcher.playerViewX, 1.0F, 0.0F, 0.0F);
            GLManager.GL.Scale(-var13, -var13, var13);
            GLManager.GL.Disable(GLEnum.Lighting);
            GLManager.GL.DepthMask(false);
            GLManager.GL.Disable(GLEnum.DepthTest);
            GLManager.GL.Enable(GLEnum.Blend);
            GLManager.GL.BlendFunc(GLEnum.SrcAlpha, GLEnum.OneMinusSrcAlpha);
            Tessellator var14 = Tessellator.instance;
            int var15 = 0;
            if (var2.Equals("deadmau5"))
            {
                var15 = -10;
            }

            GLManager.GL.Disable(GLEnum.Texture2D);
            var14.startDrawingQuads();
            int var16 = var11.GetStringWidth(var2) / 2;
            var14.setColorRGBA_F(0.0F, 0.0F, 0.0F, 0.25F);
            var14.addVertex(-var16 - 1, -1 + var15, 0.0D);
            var14.addVertex(-var16 - 1, 8 + var15, 0.0D);
            var14.addVertex(var16 + 1, 8 + var15, 0.0D);
            var14.addVertex(var16 + 1, -1 + var15, 0.0D);
            var14.draw();
            GLManager.GL.Enable(GLEnum.Texture2D);
            var11.DrawString(var2, -var11.GetStringWidth(var2) / 2, var15, Color.WhiteAlpha20);
            GLManager.GL.Enable(GLEnum.DepthTest);
            GLManager.GL.DepthMask(true);
            var11.DrawString(var2, -var11.GetStringWidth(var2) / 2, var15, Color.WhiteAlpha20);
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
