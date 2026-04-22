using BetaSharp.Client.Options;
using BetaSharp.Client.Rendering.Core;
using BetaSharp.Client.Rendering.Core.OpenGL;
using BetaSharp.Entities;
using BetaSharp.Util.Maths;

namespace BetaSharp.Client.Rendering.Entities;

public class FishingBobberEntityRenderer : EntityRenderer
{

    public void render(EntityFish var1, double x, double y, double z, float yaw, float tickDelta)
    {
        GLManager.GL.PushMatrix();
        GLManager.GL.Translate((float)x, (float)y, (float)z);
        GLManager.GL.Enable(GLEnum.RescaleNormal);
        GLManager.GL.Scale(0.5F, 0.5F, 0.5F);
        byte var10 = 1;
        byte var11 = 2;
        loadTexture("/particles.png");
        Tessellator var12 = Tessellator.instance;
        float var13 = (var10 * 8 + 0) / 128.0F;
        float var14 = (var10 * 8 + 8) / 128.0F;
        float var15 = (var11 * 8 + 0) / 128.0F;
        float var16 = (var11 * 8 + 8) / 128.0F;
        float var17 = 1.0F;
        float var18 = 0.5F;
        float var19 = 0.5F;
        GLManager.GL.Rotate(180.0F - Dispatcher.PlayerViewY, 0.0F, 1.0F, 0.0F);
        GLManager.GL.Rotate(-Dispatcher.PlayerViewX, 1.0F, 0.0F, 0.0F);
        var12.startDrawingQuads();
        var12.setNormal(0.0F, 1.0F, 0.0F);
        var12.addVertexWithUV((double)(0.0F - var18), (double)(0.0F - var19), 0.0D, (double)var13, (double)var16);
        var12.addVertexWithUV((double)(var17 - var18), (double)(0.0F - var19), 0.0D, (double)var14, (double)var16);
        var12.addVertexWithUV((double)(var17 - var18), (double)(1.0F - var19), 0.0D, (double)var14, (double)var15);
        var12.addVertexWithUV((double)(0.0F - var18), (double)(1.0F - var19), 0.0D, (double)var13, (double)var15);
        var12.draw();
        GLManager.GL.Disable(GLEnum.RescaleNormal);
        GLManager.GL.PopMatrix();
        if (var1.angler != null)
        {
            float var20 = (var1.angler.PrevYaw + (var1.angler.Yaw - var1.angler.PrevYaw) * tickDelta) * (float)Math.PI / 180.0F;
            double var21 = (double)MathHelper.Sin(var20);
            double var23 = (double)MathHelper.Cos(var20);
            float var25 = var1.angler.getSwingProgress(tickDelta);
            float var26 = MathHelper.Sin(MathHelper.Sqrt(var25) * (float)Math.PI);
            Vec3D var27 = new(-0.5D, 0.03D, 0.8D);
            var27.rotateAroundX(-(var1.angler.PrevPitch + (var1.angler.Pitch - var1.angler.PrevPitch) * tickDelta) * (float)Math.PI / 180.0F);
            var27.rotateAroundY(-(var1.angler.PrevYaw + (var1.angler.Yaw - var1.angler.PrevYaw) * tickDelta) * (float)Math.PI / 180.0F);
            var27.rotateAroundY(var26 * 0.5F);
            var27.rotateAroundX(-var26 * 0.7F);
            double var28 = var1.angler.PrevX + (var1.angler.X - var1.angler.PrevX) * (double)tickDelta + var27.x;
            double var30 = var1.angler.PrevY + (var1.angler.Y - var1.angler.PrevY) * (double)tickDelta + var27.y;
            double var32 = var1.angler.PrevZ + (var1.angler.Z - var1.angler.PrevZ) * (double)tickDelta + var27.z;
            if (Dispatcher.Options.CameraMode != EnumCameraMode.FirstPerson)
            {
                var20 = (var1.angler.LastBodyYaw + (var1.angler.BodyYaw - var1.angler.LastBodyYaw) * tickDelta) * (float)Math.PI / 180.0F;
                var21 = (double)MathHelper.Sin(var20);
                var23 = (double)MathHelper.Cos(var20);
                var28 = var1.angler.PrevX + (var1.angler.X - var1.angler.PrevX) * (double)tickDelta - var23 * 0.35D - var21 * 0.85D;
                var30 = var1.angler.PrevY + (var1.angler.Y - var1.angler.PrevY) * (double)tickDelta - 0.45D;
                var32 = var1.angler.PrevZ + (var1.angler.Z - var1.angler.PrevZ) * (double)tickDelta - var21 * 0.35D + var23 * 0.85D;
            }

            double var34 = var1.PrevX + (var1.X - var1.PrevX) * (double)tickDelta;
            double var36 = var1.PrevY + (var1.Y - var1.PrevY) * (double)tickDelta + 0.25D;
            double var38 = var1.PrevZ + (var1.Z - var1.PrevZ) * (double)tickDelta;
            double var40 = (double)(float)(var28 - var34);
            double var42 = (double)(float)(var30 - var36);
            double var44 = (double)(float)(var32 - var38);
            GLManager.GL.Disable(GLEnum.Texture2D);
            GLManager.GL.Disable(GLEnum.Lighting);
            var12.startDrawing(3);
            var12.setColorOpaque_I(0x000000);
            byte var46 = 16;

            for (int var47 = 0; var47 <= var46; ++var47)
            {
                float var48 = var47 / (float)var46;
                var12.addVertex(x + var40 * (double)var48, y + var42 * (double)(var48 * var48 + var48) * 0.5D + 0.25D, z + var44 * (double)var48);
            }

            var12.draw();
            GLManager.GL.Enable(GLEnum.Lighting);
            GLManager.GL.Enable(GLEnum.Texture2D);
        }

    }

    public override void Render(Entity target, double x, double y, double z, float yaw, float tickDelta)
    {
        render((EntityFish)target, x, y, z, yaw, tickDelta);
    }
}
