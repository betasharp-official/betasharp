using BetaSharp.Blocks;
using BetaSharp.Client.Rendering.Core;
using BetaSharp.Items;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Client.Entities.FX;

public class EntitySlimeFX : EntityFX
{

    public EntitySlimeFX(IWorldContext world, double x, double y, double z, Item item) : base(world, x, y, z, 0.0D, 0.0D, 0.0D)
    {
        particleTextureIndex = item.getTextureId(0);
        particleRed = particleGreen = particleBlue = 1.0F;
        particleGravity = Block.SnowBlock.particleFallSpeedModifier;
        particleScale /= 2.0F;
    }

    public override int getFXLayer()
    {
        return 2;
    }

    public override void renderParticle(Tessellator t, float partialTick, float rotX, float rotY, float rotZ, float upX, float upZ)
    {
        float minU = ((float)(particleTextureIndex % 16) + particleTextureJitterX / 4.0F) / 16.0F;
        float maxU = minU + 0.999F / 64.0F;
        float minV = ((float)(particleTextureIndex / 16) + particleTextureJitterY / 4.0F) / 16.0F;
        float maxV = minV + 0.999F / 64.0F;
        float size = 0.1F * particleScale;
        float renderX = (float)(prevX + (x - prevX) * (double)partialTick - interpPosX);
        float renderY = (float)(prevY + (y - prevY) * (double)partialTick - interpPosY);
        float renderZ = (float)(prevZ + (z - prevZ) * (double)partialTick - interpPosZ);
        float brightness = getBrightnessAtEyes(partialTick);
        t.setColorOpaque_F(brightness * particleRed, brightness * particleGreen, brightness * particleBlue);
        t.addVertexWithUV((double)(renderX - rotX * size - upX * size), (double)(renderY - rotY * size), (double)(renderZ - rotZ * size - upZ * size), (double)minU, (double)maxV);
        t.addVertexWithUV((double)(renderX - rotX * size + upX * size), (double)(renderY + rotY * size), (double)(renderZ - rotZ * size + upZ * size), (double)minU, (double)minV);
        t.addVertexWithUV((double)(renderX + rotX * size + upX * size), (double)(renderY + rotY * size), (double)(renderZ + rotZ * size + upZ * size), (double)maxU, (double)minV);
        t.addVertexWithUV((double)(renderX + rotX * size - upX * size), (double)(renderY - rotY * size), (double)(renderZ + rotZ * size - upZ * size), (double)maxU, (double)maxV);
    }
}
