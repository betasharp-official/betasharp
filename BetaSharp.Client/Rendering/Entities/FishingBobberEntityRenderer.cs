using BetaSharp.Client.Options;
using BetaSharp.Client.Rendering.Core;
using BetaSharp.Client.Rendering.Core.OpenGL;
using BetaSharp.Entities;
using BetaSharp.Util.Maths;

namespace BetaSharp.Client.Rendering.Entities;

public class FishingBobberEntityRenderer : EntityRenderer
{

    public void render(EntityFish entity, double x, double y, double z, float yaw, float tickDelta)
    {
        GLManager.GL.PushMatrix();
        GLManager.GL.Translate((float)x, (float)y, (float)z);
        GLManager.GL.Enable(GLEnum.RescaleNormal);
        GLManager.GL.Scale(0.5F, 0.5F, 0.5F);
        byte texColumnU = 1;
        byte texColumnV = 2;
        loadTexture("/particles.png");
        Tessellator tessellator = Tessellator.instance;
        float uMin = (texColumnU * 8 + 0) / 128.0F;
        float uMax = (texColumnU * 8 + 8) / 128.0F;
        float vMin = (texColumnV * 8 + 0) / 128.0F;
        float vMax = (texColumnV * 8 + 8) / 128.0F;
        float width = 1.0F;
        float halfX = 0.5F;
        float halfY = 0.5F;
        GLManager.GL.Rotate(180.0F - Dispatcher.playerViewY, 0.0F, 1.0F, 0.0F);
        GLManager.GL.Rotate(-Dispatcher.playerViewX, 1.0F, 0.0F, 0.0F);
        tessellator.startDrawingQuads();
        tessellator.setNormal(0.0F, 1.0F, 0.0F);
        tessellator.addVertexWithUV((double)(0.0F - halfX), (double)(0.0F - halfY), 0.0D, (double)uMin, (double)vMax);
        tessellator.addVertexWithUV((double)(width - halfX), (double)(0.0F - halfY), 0.0D, (double)uMax, (double)vMax);
        tessellator.addVertexWithUV((double)(width - halfX), (double)(1.0F - halfY), 0.0D, (double)uMax, (double)vMin);
        tessellator.addVertexWithUV((double)(0.0F - halfX), (double)(1.0F - halfY), 0.0D, (double)uMin, (double)vMin);
        tessellator.draw();
        GLManager.GL.Disable(GLEnum.RescaleNormal);
        GLManager.GL.PopMatrix();
        if (entity.angler != null)
        {
            float anglerYawRad = (entity.angler.prevYaw + (entity.angler.yaw - entity.angler.prevYaw) * tickDelta) * (float)Math.PI / 180.0F;
            double sinYaw = (double)MathHelper.Sin(anglerYawRad);
            double cosYaw = (double)MathHelper.Cos(anglerYawRad);
            float swingProgress = entity.angler.getSwingProgress(tickDelta);
            float swingFactor = MathHelper.Sin(MathHelper.Sqrt(swingProgress) * (float)Math.PI);
            Vec3D rodTipOffset = new(-0.5D, 0.03D, 0.8D);
            rodTipOffset.rotateAroundX(-(entity.angler.prevPitch + (entity.angler.pitch - entity.angler.prevPitch) * tickDelta) * (float)Math.PI / 180.0F);
            rodTipOffset.rotateAroundY(-(entity.angler.prevYaw + (entity.angler.yaw - entity.angler.prevYaw) * tickDelta) * (float)Math.PI / 180.0F);
            rodTipOffset.rotateAroundY(swingFactor * 0.5F);
            rodTipOffset.rotateAroundX(-swingFactor * 0.7F);
            double rodTipX = entity.angler.prevX + (entity.angler.x - entity.angler.prevX) * (double)tickDelta + rodTipOffset.x;
            double rodTipY = entity.angler.prevY + (entity.angler.y - entity.angler.prevY) * (double)tickDelta + rodTipOffset.y;
            double rodTipZ = entity.angler.prevZ + (entity.angler.z - entity.angler.prevZ) * (double)tickDelta + rodTipOffset.z;
            if (Dispatcher.options.CameraMode != EnumCameraMode.FirstPerson)
            {
                anglerYawRad = (entity.angler.lastBodyYaw + (entity.angler.bodyYaw - entity.angler.lastBodyYaw) * tickDelta) * (float)Math.PI / 180.0F;
                sinYaw = (double)MathHelper.Sin(anglerYawRad);
                cosYaw = (double)MathHelper.Cos(anglerYawRad);
                rodTipX = entity.angler.prevX + (entity.angler.x - entity.angler.prevX) * (double)tickDelta - cosYaw * 0.35D - sinYaw * 0.85D;
                rodTipY = entity.angler.prevY + (entity.angler.y - entity.angler.prevY) * (double)tickDelta - 0.45D;
                rodTipZ = entity.angler.prevZ + (entity.angler.z - entity.angler.prevZ) * (double)tickDelta - sinYaw * 0.35D + cosYaw * 0.85D;
            }

            double bobberX = entity.prevX + (entity.x - entity.prevX) * (double)tickDelta;
            double bobberY = entity.prevY + (entity.y - entity.prevY) * (double)tickDelta + 0.25D;
            double bobberZ = entity.prevZ + (entity.z - entity.prevZ) * (double)tickDelta;
            double dx = (double)(float)(rodTipX - bobberX);
            double dy = (double)(float)(rodTipY - bobberY);
            double dz = (double)(float)(rodTipZ - bobberZ);
            GLManager.GL.Disable(GLEnum.Texture2D);
            GLManager.GL.Disable(GLEnum.Lighting);
            tessellator.startDrawing(3);
            tessellator.setColorOpaque_I(0x000000);
            byte segmentCount = 16;

            for (int i = 0; i <= segmentCount; ++i)
            {
                float segmentProgress = i / (float)segmentCount;
                tessellator.addVertex(x + dx * (double)segmentProgress, y + dy * (double)(segmentProgress * segmentProgress + segmentProgress) * 0.5D + 0.25D, z + dz * (double)segmentProgress);
            }

            tessellator.draw();
            GLManager.GL.Enable(GLEnum.Lighting);
            GLManager.GL.Enable(GLEnum.Texture2D);
        }

    }

    public override void render(Entity target, double x, double y, double z, float yaw, float tickDelta)
    {
        render((EntityFish)target, x, y, z, yaw, tickDelta);
    }
}
