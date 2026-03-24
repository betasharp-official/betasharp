using BetaSharp.Blocks;
using BetaSharp.Client.Rendering.Core;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Client.Entities.FX;

public class EntityDiggingFX : EntityFX
{

    private readonly Block targetedBlock;
    private readonly int hitFace;
    private readonly int blockMeta;

    public EntityDiggingFX(IWorldContext world, double x, double y, double z, double velocityX, double velocityY, double velocityZ, Block targetedBlock, int hitFace, int meta) : base(world, x, y, z, velocityX, velocityY, velocityZ)
    {
        this.targetedBlock = targetedBlock;
        particleTextureIndex = targetedBlock.getTexture(hitFace, meta);
        particleGravity = targetedBlock.particleFallSpeedModifier;
        particleRed = particleGreen = particleBlue = 0.6F;
        particleScale /= 2.0F;
        blockMeta = meta;
    }

    public EntityDiggingFX GetColorMultiplier(int x, int y, int z)
    {
        // Grass top face is tinted green
        if (targetedBlock == Block.GrassBlock && particleTextureIndex != 0)
        {
            return this;
        }

        int color = targetedBlock.getColorMultiplier(world.Reader, x, y, z, blockMeta);
        
        particleRed *= (color >> 16 & 255) / 255.0F;
        particleGreen *= (color >> 8 & 255) / 255.0F;
        particleBlue *= (color & 255) / 255.0F;
        
        return this;
    }

    public override int getFXLayer()
    {
        return 1;
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
