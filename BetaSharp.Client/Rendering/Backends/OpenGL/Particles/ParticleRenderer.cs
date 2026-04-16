using BetaSharp.Client.Rendering.Core;
using BetaSharp.Client.Rendering.Core.Textures;
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
        ParticleBuffer[] layers,
        float yaw, float pitch,
        double x, double y, double z,
        double lastTickX, double lastTickY, double lastTickZ,
        float partialTick,
        ITextureManager textureManager,
        IWorldContext world)
    {
        float radYaw = yaw * MathF.PI / 180.0f;
        float radPitch = pitch * MathF.PI / 180.0f;

        float cosYaw = MathHelper.Cos(radYaw);
        float sinYaw = MathHelper.Sin(radYaw);
        float cosPitch = MathHelper.Cos(radPitch);
        float sinPitch = MathHelper.Sin(radPitch);

        // Billboarding vecs (vertical orientation of the particle quad)
        float upX = -sinYaw * sinPitch;
        float upZ = cosYaw * sinPitch;

        double interpX = lastTickX + (x - lastTickX) * partialTick;
        double interpY = lastTickY + (y - lastTickY) * partialTick;
        double interpZ = lastTickZ + (z - lastTickZ) * partialTick;

        Tessellator t = Tessellator.instance;

        for (int layer = 0; layer < 3; layer++)
        {
            ParticleBuffer buf = layers[layer];
            if (buf.Count == 0)
            {
                continue;
            }


            textureManager.BindTexture(textureManager.GetTextureId(s_layerTextures[layer]));
            t.startDrawingQuads();

            for (int i = 0; i < buf.Count; i++)
            {
                ref readonly ParticleTypeConfig config = ref ParticleTypeConfig.Configs[(int)buf.Type[i]];

                // Calculate relative pos to camera
                float rx = (float)(buf.PrevX[i] + (buf.X[i] - buf.PrevX[i]) * partialTick - interpX);
                float ry = (float)(buf.PrevY[i] + (buf.Y[i] - buf.PrevY[i]) * partialTick - interpY);
                float rz = (float)(buf.PrevZ[i] + (buf.Z[i] - buf.PrevZ[i]) * partialTick - interpZ);

                float scale = ComputeScale(config.Scale, buf, i, partialTick);
                float size = 0.1f * scale;

                float brightness = ComputeBrightness(config.Brightness, buf, i, partialTick, world);

                ComputeUVs(config.UV, buf.TextureIndex[i], buf.TexJitterX[i], buf.TexJitterY[i],
                    out float minU, out float maxU, out float minV, out float maxV);

                t.setColorOpaque_F(buf.Red[i] * brightness, buf.Green[i] * brightness, buf.Blue[i] * brightness);
                t.addVertexWithUV(rx - cosYaw * size - upX * size, ry - cosPitch * size,
                    rz - sinYaw * size - upZ * size, maxU, maxV);
                t.addVertexWithUV(rx - cosYaw * size + upX * size, ry + cosPitch * size,
                    rz - sinYaw * size + upZ * size, maxU, minV);
                t.addVertexWithUV(rx + cosYaw * size + upX * size, ry + cosPitch * size,
                    rz + sinYaw * size + upZ * size, minU, minV);
                t.addVertexWithUV(rx + cosYaw * size - upX * size, ry - cosPitch * size,
                    rz + sinYaw * size - upZ * size, minU, maxV);
            }

            t.draw();
        }
    }

    public static void RenderSpecial(List<ISpecialParticle> specialParticles,
        double x, double y, double z,
        double lastTickX, double lastTickY, double lastTickZ,
        float partialTick)
    {
        if (specialParticles.Count == 0)
        {
            return;
        }

        double interpX = lastTickX + (x - lastTickX) * partialTick;
        double interpY = lastTickY + (y - lastTickY) * partialTick;
        double interpZ = lastTickZ + (z - lastTickZ) * partialTick;

        Tessellator t = Tessellator.instance;
        for (int i = 0; i < specialParticles.Count; i++)
        {
            specialParticles[i].Render(t, partialTick, interpX, interpY, interpZ);
        }
    }

    private static float ComputeScale(ScaleModel model, ParticleBuffer buf, int i, float partialTick)
    {
        float progress = ((float)buf.Age[i] + partialTick) / buf.MaxAge[i];

        return model switch
        {
            ScaleModel.Constant => buf.BaseScale[i],
            ScaleModel.GrowToFull => buf.BaseScale[i] * Math.Clamp(progress * 32.0f, 0.0f, 1.0f),
            ScaleModel.ShrinkHalf => buf.BaseScale[i] * (1.0f - progress * progress * 0.5f),
            ScaleModel.ShrinkSquared => buf.BaseScale[i] * (1.0f - progress * progress),
            ScaleModel.PortalEase => buf.BaseScale[i] * (1.0f - (1.0f - progress) * (1.0f - progress)),
            _ => buf.BaseScale[i],
        };
    }

    private static float ComputeBrightness(BrightnessModel model, ParticleBuffer buf, int i,
        float partialTick, IWorldContext world)
    {
        switch (model)
        {
            case BrightnessModel.AlwaysFull: return 1.0f;
            case BrightnessModel.FadeFromFull:
                {
                    float p = Math.Clamp(((float)buf.Age[i] + partialTick) / buf.MaxAge[i], 0.0f, 1.0f);
                    float worldBright = GetWorldBrightness(buf, i, world);
                    return worldBright * p + (1.0f - p);
                }
            case BrightnessModel.EaseToFull:
                {
                    float p = Math.Clamp(((float)buf.Age[i] + partialTick) / buf.MaxAge[i], 0.0f, 1.0f);
                    float worldBright = GetWorldBrightness(buf, i, world);
                    float ease = p * p * p * p; // Quartic ease-in
                    return worldBright * (1.0f - ease) + ease;
                }
            default: return GetWorldBrightness(buf, i, world); // WorldBased
        }
    }

    private static float GetWorldBrightness(ParticleBuffer buf, int i, IWorldContext world)
    {
        // World sampling uses the Floor of coordinates to find the specific block voxel
        int bx = MathHelper.Floor(buf.X[i]);
        int by = MathHelper.Floor(buf.Y[i]);
        int bz = MathHelper.Floor(buf.Z[i]);
        return world.Lighting.GetLuminance(bx, by, bz);
    }

    private static void ComputeUVs(UVModel model, int textureIndex, float jitterX, float jitterY,
        out float minU, out float maxU, out float minV, out float maxV)
    {
        // 0.999f is used to clamp UVs slightly inside the tile boundary to prevent texture bleeding at quad edges
        switch (model)
        {
            case UVModel.Jittered4x4:
                {
                    minU = ((textureIndex % 16) + jitterX / 4.0f) / 16.0f;
                    maxU = minU + 0.999f / 64.0f;
                    minV = ((textureIndex / 16) + jitterY / 4.0f) / 16.0f;
                    maxV = minV + 0.999f / 64.0f;
                    break;
                }
            case UVModel.Standard16x16:
            default:
                {
                    minU = (textureIndex % 16) / 16.0f;
                    maxU = minU + 0.999f / 16.0f;
                    minV = (textureIndex / 16) / 16.0f;
                    maxV = minV + 0.999f / 16.0f;
                    break;
                }
        }
    }
}
