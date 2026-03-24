using BetaSharp.Client.Rendering.Core;
using BetaSharp.Client.Rendering.Core.Textures;
using BetaSharp.Entities;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Client.Rendering.Particles;

public static class ParticleRenderer
{
    private static readonly string[] s_layerTextures =
    [
        "/particles.png",
        "/terrain.png",
        "/gui/items.png"
    ];

    public static void Render(
        ParticleBuffer[] layers, List<ISpecialParticle> specialParticles,
        Entity camera, float partialTick,
        TextureManager texMgr, IWorldContext world)
    {
        float cosYaw = MathHelper.Cos(camera.yaw * (float)Math.PI / 180.0f);
        float sinYaw = MathHelper.Sin(camera.yaw * (float)Math.PI / 180.0f);
        float cosPitch = MathHelper.Cos(camera.pitch * (float)Math.PI / 180.0f);
        float upX = -sinYaw * MathHelper.Sin(camera.pitch * (float)Math.PI / 180.0f);
        float upZ = cosYaw * MathHelper.Sin(camera.pitch * (float)Math.PI / 180.0f);

        double interpX = camera.lastTickX + (camera.x - camera.lastTickX) * partialTick;
        double interpY = camera.lastTickY + (camera.y - camera.lastTickY) * partialTick;
        double interpZ = camera.lastTickZ + (camera.z - camera.lastTickZ) * partialTick;

        Tessellator t = Tessellator.instance;

        for (int layer = 0; layer < 3; layer++)
        {
            ParticleBuffer buf = layers[layer];
            if (buf.Count == 0)
            {
                continue;
            }

            texMgr.BindTexture(texMgr.GetTextureId(s_layerTextures[layer]));
            t.startDrawingQuads();

            for (int i = 0; i < buf.Count; i++)
            {
                ref readonly ParticleTypeConfig config = ref ParticleTypeConfig.Configs[(int)buf.Type[i]];

                float rx = (float)(buf.PrevX[i] + (buf.X[i] - buf.PrevX[i]) * partialTick - interpX);
                float ry = (float)(buf.PrevY[i] + (buf.Y[i] - buf.PrevY[i]) * partialTick - interpY);
                float rz = (float)(buf.PrevZ[i] + (buf.Z[i] - buf.PrevZ[i]) * partialTick - interpZ);

                float scale = ComputeScale(config.Scale, buf, i, partialTick);
                float size = 0.1f * scale;

                float brightness = ComputeBrightness(config.Brightness, buf, i, partialTick, world);

                ComputeUVs(config.UV, buf.TextureIndex[i], buf.TexJitterX[i], buf.TexJitterY[i],
                    out float minU, out float maxU, out float minV, out float maxV);

                t.setColorOpaque_F(buf.Red[i] * brightness, buf.Green[i] * brightness, buf.Blue[i] * brightness);
                t.addVertexWithUV(rx - cosYaw * size - upX * size, ry - cosPitch * size, rz - sinYaw * size - upZ * size, maxU, maxV);
                t.addVertexWithUV(rx - cosYaw * size + upX * size, ry + cosPitch * size, rz - sinYaw * size + upZ * size, maxU, minV);
                t.addVertexWithUV(rx + cosYaw * size + upX * size, ry + cosPitch * size, rz + sinYaw * size + upZ * size, minU, minV);
                t.addVertexWithUV(rx + cosYaw * size - upX * size, ry - cosPitch * size, rz + sinYaw * size - upZ * size, minU, maxV);
            }

            t.draw();
        }
    }

    public static void RenderSpecial(List<ISpecialParticle> specialParticles,
        Entity camera, float partialTick)
    {
        if (specialParticles.Count == 0)
        {
            return;
        }

        double interpX = camera.lastTickX + (camera.x - camera.lastTickX) * partialTick;
        double interpY = camera.lastTickY + (camera.y - camera.lastTickY) * partialTick;
        double interpZ = camera.lastTickZ + (camera.z - camera.lastTickZ) * partialTick;

        Tessellator t = Tessellator.instance;
        for (int i = 0; i < specialParticles.Count; i++)
        {
            specialParticles[i].Render(t, partialTick, interpX, interpY, interpZ);
        }
    }

    private static float ComputeScale(ScaleModel model, ParticleBuffer buf, int i, float partialTick)
    {
        float progress = ((float)buf.Age[i] + partialTick) / buf.MaxAge[i];

        switch (model)
        {
            case ScaleModel.Constant:
                return buf.BaseScale[i];

            case ScaleModel.GrowToFull:
                {
                    float lifeProgress = progress * 32.0f;
                    if (lifeProgress < 0f) lifeProgress = 0f;
                    if (lifeProgress > 1f) lifeProgress = 1f;
                    return buf.BaseScale[i] * lifeProgress;
                }

            case ScaleModel.ShrinkHalf:
                return buf.BaseScale[i] * (1.0f - progress * progress * 0.5f);

            case ScaleModel.ShrinkSquared:
                return buf.BaseScale[i] * (1.0f - progress * progress);

            case ScaleModel.PortalEase:
                {
                    float inv = 1.0f - progress;
                    inv *= inv;
                    return buf.BaseScale[i] * (1.0f - inv);
                }

            default:
                return buf.BaseScale[i];
        }
    }

    private static float ComputeBrightness(BrightnessModel model, ParticleBuffer buf, int i,
        float partialTick, IWorldContext world)
    {
        switch (model)
        {
            case BrightnessModel.AlwaysFull:
                return 1.0f;

            case BrightnessModel.FadeFromFull:
                {
                    float progress = ((float)buf.Age[i] + partialTick) / buf.MaxAge[i];
                    if (progress < 0f) progress = 0f;
                    if (progress > 1f) progress = 1f;
                    float worldBright = GetWorldBrightness(buf, i, world);
                    return worldBright * progress + (1.0f - progress);
                }

            case BrightnessModel.EaseToFull:
                {
                    float progress = ((float)buf.Age[i] + partialTick) / buf.MaxAge[i];
                    if (progress < 0f) progress = 0f;
                    if (progress > 1f) progress = 1f;
                    float worldBright = GetWorldBrightness(buf, i, world);
                    float p = progress * progress * progress * progress;
                    return worldBright * (1.0f - p) + p;
                }

            default: // WorldBased
                return GetWorldBrightness(buf, i, world);
        }
    }

    private static float GetWorldBrightness(ParticleBuffer buf, int i, IWorldContext world)
    {
        int bx = MathHelper.Floor(buf.X[i]);
        int by = MathHelper.Floor(buf.Y[i]);
        int bz = MathHelper.Floor(buf.Z[i]);
        return world.Lighting.GetLuminance(bx, by, bz);
    }

    private static void ComputeUVs(UVModel model, int textureIndex, float jitterX, float jitterY,
        out float minU, out float maxU, out float minV, out float maxV)
    {
        if (model == UVModel.Jittered4x4)
        {
            minU = ((textureIndex % 16) + jitterX / 4.0f) / 16.0f;
            maxU = minU + 0.999f / 64.0f;
            minV = ((textureIndex / 16) + jitterY / 4.0f) / 16.0f;
            maxV = minV + 0.999f / 64.0f;
        }
        else
        {
            minU = (textureIndex % 16) / 16.0f;
            maxU = minU + 0.999f / 16.0f;
            minV = (textureIndex / 16) / 16.0f;
            maxV = minV + 0.999f / 16.0f;
        }
    }
}
