using BetaSharp.Blocks;
using BetaSharp.Client.Rendering.Blocks;
using BetaSharp.Client.Rendering.Core;
using BetaSharp.Client.Rendering.Entities.Models;
using BetaSharp.Entities;
using BetaSharp.Util.Maths;

namespace BetaSharp.Client.Rendering.Entities;

public class MinecartEntityRenderer : EntityRenderer
{
    protected ModelBase modelMinecart;

    public MinecartEntityRenderer()
    {
        ShadowRadius = 0.5F;
        modelMinecart = new ModelMinecart();
    }

    public void render(EntityMinecart var1, double x, double y, double z, float yaw, float tickDelta)
    {
        GLManager.GL.PushMatrix();
        double var10 = var1.lastTickX + (var1.x - var1.lastTickX) * (double)tickDelta;
        double var12 = var1.lastTickY + (var1.y - var1.lastTickY) * (double)tickDelta;
        double var14 = var1.lastTickZ + (var1.z - var1.lastTickZ) * (double)tickDelta;
        double var16 = (double)0.3F;
        Vec3D? var18 = var1.func_514_g(var10, var12, var14);
        float var19 = var1.prevPitch + (var1.pitch - var1.prevPitch) * tickDelta;
        if (var18 != null)
        {
            Vec3D var20 = var1.func_515_a(var10, var12, var14, var16) ?? var18.Value;
            Vec3D var21 = var1.func_515_a(var10, var12, var14, -var16) ?? var18.Value;

            x += var18.Value.x - var10;
            y += (var20.y + var21.y) / 2.0D - var12;
            z += var18.Value.z - var14;
            Vec3D var22 = var21 - var20;
            if (var22.magnitude() != 0.0D)
            {
                var22 = var22.normalize();
                yaw = (float)(Math.Atan2(var22.z, var22.x) * 180.0D / Math.PI);
                var19 = (float)(Math.Atan(var22.y) * 73.0D);
            }
        }

        GLManager.GL.Translate((float)x, (float)y, (float)z);
        GLManager.GL.Rotate(180.0F - yaw, 0.0F, 1.0F, 0.0F);
        GLManager.GL.Rotate(-var19, 0.0F, 0.0F, 1.0F);
        float var23 = var1.minecartTimeSinceHit - tickDelta;
        float var24 = var1.minecartCurrentDamage - tickDelta;
        if (var24 < 0.0F)
        {
            var24 = 0.0F;
        }

        if (var23 > 0.0F)
        {
            GLManager.GL.Rotate(MathHelper.Sin(var23) * var23 * var24 / 10.0F * var1.minecartRockDirection, 1.0F, 0.0F, 0.0F);
        }

        if (var1.type != 0)
        {
            loadTexture("/terrain.png");
            float var25 = 12.0F / 16.0F;
            GLManager.GL.Scale(var25, var25, var25);
            GLManager.GL.Translate(0.0F, 5.0F / 16.0F, 0.0F);
            GLManager.GL.Rotate(90.0F, 0.0F, 1.0F, 0.0F);
            if (var1.type == 1)
            {
                BlockRenderer.RenderBlockOnInventory(Block.Chest, 0, var1.getBrightnessAtEyes(tickDelta), Tessellator.instance);
            }
            else if (var1.type == 2)
            {
                BlockRenderer.RenderBlockOnInventory(Block.Furnace, 0, var1.getBrightnessAtEyes(tickDelta), Tessellator.instance);
            }

            GLManager.GL.Rotate(-90.0F, 0.0F, 1.0F, 0.0F);
            GLManager.GL.Translate(0.0F, -(5.0F / 16.0F), 0.0F);
            GLManager.GL.Scale(1.0F / var25, 1.0F / var25, 1.0F / var25);
        }

        loadTexture("/item/cart.png");
        GLManager.GL.Scale(-1.0F, -1.0F, 1.0F);
        modelMinecart.render(0.0F, 0.0F, -0.1F, 0.0F, 0.0F, 1.0F / 16.0F);
        GLManager.GL.PopMatrix();
    }

    public override void render(Entity target, double x, double y, double z, float yaw, float tickDelta)
    {
        render((EntityMinecart)target, x, y, z, yaw, tickDelta);
    }
}
