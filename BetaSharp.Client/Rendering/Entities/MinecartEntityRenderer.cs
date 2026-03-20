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

    public void render(EntityMinecart entity, double x, double y, double z, float yaw, float tickDelta)
    {
        GLManager.GL.PushMatrix();
        double interpX = entity.lastTickX + (entity.x - entity.lastTickX) * (double)tickDelta;
        double interpY = entity.lastTickY + (entity.y - entity.lastTickY) * (double)tickDelta;
        double interpZ = entity.lastTickZ + (entity.z - entity.lastTickZ) * (double)tickDelta;
        double railOffset = (double)0.3F;
        Vec3D? railDir = entity.func_514_g(interpX, interpY, interpZ);
        float pitchAngle = entity.prevPitch + (entity.pitch - entity.prevPitch) * tickDelta;
        if (railDir != null)
        {
            Vec3D forwardPos = entity.func_515_a(interpX, interpY, interpZ, railOffset) ?? railDir.Value;
            Vec3D backwardPos = entity.func_515_a(interpX, interpY, interpZ, -railOffset) ?? railDir.Value;

            x += railDir.Value.x - interpX;
            y += (forwardPos.y + backwardPos.y) / 2.0D - interpY;
            z += railDir.Value.z - interpZ;
            Vec3D railDelta = backwardPos - forwardPos;
            if (railDelta.magnitude() != 0.0D)
            {
                railDelta = railDelta.normalize();
                yaw = (float)(Math.Atan2(railDelta.z, railDelta.x) * 180.0D / Math.PI);
                pitchAngle = (float)(Math.Atan(railDelta.y) * 73.0D);
            }
        }

        GLManager.GL.Translate((float)x, (float)y, (float)z);
        GLManager.GL.Rotate(180.0F - yaw, 0.0F, 1.0F, 0.0F);
        GLManager.GL.Rotate(-pitchAngle, 0.0F, 0.0F, 1.0F);
        float timeSinceHit = entity.minecartTimeSinceHit - tickDelta;
        float currentDamage = entity.minecartCurrentDamage - tickDelta;
        if (currentDamage < 0.0F)
        {
            currentDamage = 0.0F;
        }

        if (timeSinceHit > 0.0F)
        {
            GLManager.GL.Rotate(MathHelper.Sin(timeSinceHit) * timeSinceHit * currentDamage / 10.0F * entity.minecartRockDirection, 1.0F, 0.0F, 0.0F);
        }

        if (entity.type != 0)
        {
            loadTexture("/terrain.png");
            float cartScale = 12.0F / 16.0F;
            GLManager.GL.Scale(cartScale, cartScale, cartScale);
            GLManager.GL.Translate(0.0F, 5.0F / 16.0F, 0.0F);
            GLManager.GL.Rotate(90.0F, 0.0F, 1.0F, 0.0F);
            if (entity.type == 1)
            {
                BlockRenderer.RenderBlockOnInventory(Block.Chest, 0, entity.getBrightnessAtEyes(tickDelta), Tessellator.instance);
            }
            else if (entity.type == 2)
            {
                BlockRenderer.RenderBlockOnInventory(Block.Furnace, 0, entity.getBrightnessAtEyes(tickDelta), Tessellator.instance);
            }

            GLManager.GL.Rotate(-90.0F, 0.0F, 1.0F, 0.0F);
            GLManager.GL.Translate(0.0F, -(5.0F / 16.0F), 0.0F);
            GLManager.GL.Scale(1.0F / cartScale, 1.0F / cartScale, 1.0F / cartScale);
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
