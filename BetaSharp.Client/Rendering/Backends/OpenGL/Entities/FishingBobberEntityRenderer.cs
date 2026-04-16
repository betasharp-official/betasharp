using BetaSharp.Client.Options;
using BetaSharp.Client.Rendering.Core;
using BetaSharp.Client.Rendering.Legacy;
using BetaSharp.Entities;
using BetaSharp.Util.Maths;

namespace BetaSharp.Client.Rendering.Entities;

public class FishingBobberEntityRenderer : EntityRenderer
{
    public void render(EntityFish var1, double x, double y, double z, float yaw, float tickDelta)
    {
        Scene.PushMatrix();
        Scene.Translate((float)x, (float)y, (float)z);
        Scene.Enable(SceneRenderCapability.RescaleNormal);
        Scene.Scale(0.5F, 0.5F, 0.5F);
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
        Scene.Rotate(180.0F - Dispatcher.PlayerViewY, 0.0F, 1.0F, 0.0F);
        Scene.Rotate(-Dispatcher.PlayerViewX, 1.0F, 0.0F, 0.0F);
        var12.startDrawingQuads();
        var12.setNormal(0.0F, 1.0F, 0.0F);
        var12.addVertexWithUV((double)(0.0F - var18), (double)(0.0F - var19), 0.0D, (double)var13, (double)var16);
        var12.addVertexWithUV((double)(var17 - var18), (double)(0.0F - var19), 0.0D, (double)var14, (double)var16);
        var12.addVertexWithUV((double)(var17 - var18), (double)(1.0F - var19), 0.0D, (double)var14, (double)var15);
        var12.addVertexWithUV((double)(0.0F - var18), (double)(1.0F - var19), 0.0D, (double)var13, (double)var15);
        var12.draw();
        Scene.Disable(SceneRenderCapability.RescaleNormal);
        Scene.PopMatrix();
        if (var1.angler != null)
        {
            float var20 = (var1.angler.prevYaw + (var1.angler.yaw - var1.angler.prevYaw) * tickDelta) * (float)Math.PI /
                          180.0F;
            double var21 = (double)MathHelper.Sin(var20);
            double var23 = (double)MathHelper.Cos(var20);
            float var25 = var1.angler.getSwingProgress(tickDelta);
            float var26 = MathHelper.Sin(MathHelper.Sqrt(var25) * (float)Math.PI);
            Vec3D var27 = new(-0.5D, 0.03D, 0.8D);
            var27.rotateAroundX(-(var1.angler.prevPitch + (var1.angler.pitch - var1.angler.prevPitch) * tickDelta) *
                (float)Math.PI / 180.0F);
            var27.rotateAroundY(-(var1.angler.prevYaw + (var1.angler.yaw - var1.angler.prevYaw) * tickDelta) *
                (float)Math.PI / 180.0F);
            var27.rotateAroundY(var26 * 0.5F);
            var27.rotateAroundX(-var26 * 0.7F);
            double var28 = var1.angler.prevX + (var1.angler.x - var1.angler.prevX) * (double)tickDelta + var27.x;
            double var30 = var1.angler.prevY + (var1.angler.y - var1.angler.prevY) * (double)tickDelta + var27.y;
            double var32 = var1.angler.prevZ + (var1.angler.z - var1.angler.prevZ) * (double)tickDelta + var27.z;
            if (Dispatcher.Options.CameraMode != EnumCameraMode.FirstPerson)
            {
                var20 = (var1.angler.lastBodyYaw + (var1.angler.bodyYaw - var1.angler.lastBodyYaw) * tickDelta) *
                    (float)Math.PI / 180.0F;
                var21 = (double)MathHelper.Sin(var20);
                var23 = (double)MathHelper.Cos(var20);
                var28 = var1.angler.prevX + (var1.angler.x - var1.angler.prevX) * (double)tickDelta - var23 * 0.35D -
                        var21 * 0.85D;
                var30 = var1.angler.prevY + (var1.angler.y - var1.angler.prevY) * (double)tickDelta - 0.45D;
                var32 = var1.angler.prevZ + (var1.angler.z - var1.angler.prevZ) * (double)tickDelta - var21 * 0.35D +
                        var23 * 0.85D;
            }

            double var34 = var1.prevX + (var1.x - var1.prevX) * (double)tickDelta;
            double var36 = var1.prevY + (var1.y - var1.prevY) * (double)tickDelta + 0.25D;
            double var38 = var1.prevZ + (var1.z - var1.prevZ) * (double)tickDelta;
            double var40 = (double)(float)(var28 - var34);
            double var42 = (double)(float)(var30 - var36);
            double var44 = (double)(float)(var32 - var38);
            Scene.Disable(SceneRenderCapability.Texture2D);
            Scene.Disable(SceneRenderCapability.Lighting);
            var12.startDrawing(3);
            var12.setColorOpaque_I(0x000000);
            byte var46 = 16;

            for (int var47 = 0; var47 <= var46; ++var47)
            {
                float var48 = var47 / (float)var46;
                var12.addVertex(x + var40 * (double)var48, y + var42 * (double)(var48 * var48 + var48) * 0.5D + 0.25D,
                    z + var44 * (double)var48);
            }

            var12.draw();
            Scene.Enable(SceneRenderCapability.Lighting);
            Scene.Enable(SceneRenderCapability.Texture2D);
        }
    }

    public override void Render(Entity target, double x, double y, double z, float yaw, float tickDelta)
    {
        render((EntityFish)target, x, y, z, yaw, tickDelta);
    }
}
