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

    protected void updateCreeperScale(EntityCreeper var1, float var2)
    {
        float var4 = var1.setCreeperFlashTime(var2);
        float var5 = 1.0F + MathHelper.Sin(var4 * 100.0F) * var4 * 0.01F;
        if (var4 < 0.0F)
        {
            var4 = 0.0F;
        }

        if (var4 > 1.0F)
        {
            var4 = 1.0F;
        }

        var4 *= var4;
        var4 *= var4;
        float var6 = (1.0F + var4 * 0.4F) * var5;
        float var7 = (1.0F + var4 * 0.1F) / var5;
        GLManager.GL.Scale(var6, var7, var6);
    }

    protected int updateCreeperColorMultiplier(EntityCreeper var1, float var2, float var3)
    {
        float var5 = var1.setCreeperFlashTime(var3);
        if ((int)(var5 * 10.0F) % 2 == 0)
        {
            return 0;
        }
        else
        {
            int var6 = (int)(var5 * 0.2F * 255.0F);
            if (var6 < 0)
            {
                var6 = 0;
            }

            if (var6 > 255)
            {
                var6 = 255;
            }

            int var7 = 255;
            int var8 = 255;
            int var9 = 255;
            return var6 << 24 | var7 << 16 | var8 << 8 | var9;
        }
    }

    protected bool func_27006_a(EntityCreeper var1, int var2, float var3)
    {
        if (var1.getPowered())
        {
            if (var2 == 1)
            {
                float var4 = var1.age + var3;
                loadTexture("/armor/power.png");
                GLManager.GL.MatrixMode(GLEnum.Texture2D); //wtf?
                GLManager.GL.LoadIdentity();
                float var5 = var4 * 0.01F;
                float var6 = var4 * 0.01F;
                GLManager.GL.Translate(var5, var6, 0.0F);
                setRenderPassModel(model);
                GLManager.GL.MatrixMode(GLEnum.Modelview);
                GLManager.GL.Enable(GLEnum.Blend);
                float var7 = 0.5F;
                GLManager.GL.Color4(var7, var7, var7, 1.0F);
                GLManager.GL.Disable(GLEnum.Lighting);
                GLManager.GL.BlendFunc(GLEnum.One, GLEnum.One);
                return true;
            }

            if (var2 == 2)
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

    protected bool func_27007_b(EntityCreeper var1, int var2, float var3)
    {
        return false;
    }

    protected override void preRenderCallback(EntityLiving var1, float var2)
    {
        updateCreeperScale((EntityCreeper)var1, var2);
    }

    protected override int getColorMultiplier(EntityLiving var1, float var2, float var3)
    {
        return updateCreeperColorMultiplier((EntityCreeper)var1, var2, var3);
    }

    protected override bool shouldRenderPass(EntityLiving var1, int var2, float var3)
    {
        return func_27006_a((EntityCreeper)var1, var2, var3);
    }

    protected override bool func_27005_b(EntityLiving var1, int var2, float var3)
    {
        return func_27007_b((EntityCreeper)var1, var2, var3);
    }
}