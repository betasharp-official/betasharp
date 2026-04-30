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

    public void render(EntityMinecart minecart, double x, double y, double z, float yaw, float tickDelta)
    {
        GLManager.GL.PushMatrix();
        double interpX = minecart.LastTickX + (minecart.X - minecart.LastTickX) * (double)tickDelta;
        double interpY = minecart.LastTickY + (minecart.Y - minecart.LastTickY) * (double)tickDelta;
        double interpZ = minecart.LastTickZ + (minecart.Z - minecart.LastTickZ) * (double)tickDelta;
        double trackOffset = (double)0.3F;
        Vec3D? trackPos = minecart.GetTrackPosition(interpX, interpY, interpZ);
        float pitch = minecart.PrevPitch + (minecart.Pitch - minecart.PrevPitch) * tickDelta;
        if (trackPos != null)
        {
            Vec3D forwardTrackPos = minecart.GetTrackPositionOffset(interpX, interpY, interpZ, trackOffset) ?? trackPos.Value;
            Vec3D backTrackPos = minecart.GetTrackPositionOffset(interpX, interpY, interpZ, -trackOffset) ?? trackPos.Value;

            x += trackPos.Value.x - interpX;
            y += (forwardTrackPos.y + backTrackPos.y) / 2.0D - interpY;
            z += trackPos.Value.z - interpZ;
            Vec3D trackDirection = backTrackPos - forwardTrackPos;
            if (trackDirection.magnitude() != 0.0D)
            {
                trackDirection = trackDirection.normalize();
                yaw = (float)(Math.Atan2(trackDirection.z, trackDirection.x) * 180.0D / Math.PI);
                pitch = (float)(Math.Atan(trackDirection.y) * 73.0D);
            }
        }

        GLManager.GL.Translate((float)x, (float)y, (float)z);
        GLManager.GL.Rotate(180.0F - yaw, 0.0F, 1.0F, 0.0F);
        GLManager.GL.Rotate(-pitch, 0.0F, 0.0F, 1.0F);
        float timeSinceHit = minecart.MinecartTimeSinceHit - tickDelta;
        float damageTaken = minecart.MinecartCurrentDamage - tickDelta;
        if (damageTaken < 0.0F)
        {
            damageTaken = 0.0F;
        }

        if (timeSinceHit > 0.0F)
        {
            GLManager.GL.Rotate(MathHelper.Sin(timeSinceHit) * timeSinceHit * damageTaken / 10.0F * minecart.MinecartRockDirection, 1.0F, 0.0F, 0.0F);
        }

        if (minecart.type != 0)
        {
            loadTexture("/terrain.png");
            float blockScale = 12.0F / 16.0F;
            GLManager.GL.Scale(blockScale, blockScale, blockScale);
            GLManager.GL.Translate(0.0F, 5.0F / 16.0F, 0.0F);
            GLManager.GL.Rotate(90.0F, 0.0F, 1.0F, 0.0F);
            if (minecart.type == 1)
            {
                BlockRenderer.RenderBlockOnInventory(Block.Chest, 0, minecart.GetBrightnessAtEyes(tickDelta), Tessellator.instance);
            }
            else if (minecart.type == 2)
            {
                BlockRenderer.RenderBlockOnInventory(Block.Furnace, 0, minecart.GetBrightnessAtEyes(tickDelta), Tessellator.instance);
            }

            GLManager.GL.Rotate(-90.0F, 0.0F, 1.0F, 0.0F);
            GLManager.GL.Translate(0.0F, -(5.0F / 16.0F), 0.0F);
            GLManager.GL.Scale(1.0F / blockScale, 1.0F / blockScale, 1.0F / blockScale);
        }

        loadTexture("/item/cart.png");
        GLManager.GL.Scale(-1.0F, -1.0F, 1.0F);
        modelMinecart.render(0.0F, 0.0F, -0.1F, 0.0F, 0.0F, 1.0F / 16.0F);
        GLManager.GL.PopMatrix();
    }

    public override void Render(Entity target, double x, double y, double z, float yaw, float tickDelta)
    {
        render((EntityMinecart)target, x, y, z, yaw, tickDelta);
    }
}
