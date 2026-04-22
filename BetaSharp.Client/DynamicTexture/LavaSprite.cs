using BetaSharp.Blocks;
using BetaSharp.Util.Maths;

namespace BetaSharp.Client.DynamicTexture;

internal class LavaSprite() : Rendering.Core.Textures.DynamicTexture(Block.FlowingLava.TextureId)
{
    private readonly float[] _heat = new float[256];
    private readonly float[] _heatDelta = new float[256];
    private float[] _current = new float[256];
    private float[] _next = new float[256];

    public override void Setup(BetaSharp game) => TryLoadCustomTexture(game, "custom_lava_still.png");

    public override void tick()
    {
        if (CustomFrames != null)
        {
            Buffer.BlockCopy(CustomFrames[CustomFrameIndex], 0, Pixels, 0, Pixels.Length);
            CustomFrameIndex = (CustomFrameIndex + 1) % CustomFrameCount;
            return;
        }

        for (int x = 0; x < 16; ++x)
        {
            for (int y = 0; y < 16; ++y)
            {
                float accumulatedHeat = 0.0F;
                int distortX = (int)(MathHelper.Sin(y * (float)Math.PI * 2.0F / 16.0F) * 1.2F);
                int distortY = (int)(MathHelper.Sin(x * (float)Math.PI * 2.0F / 16.0F) * 1.2F);

                int nx;
                for (nx = x - 1; nx <= x + 1; ++nx)
                {
                    int ny;
                    for (ny = y - 1; ny <= y + 1; ++ny)
                    {
                        int sampleX = (nx + distortX) & 15;
                        int sampleY = (ny + distortY) & 15;
                        accumulatedHeat += _current[sampleX + sampleY * 16];
                    }
                }

                _next[x + y * 16] = accumulatedHeat / 10.0F +
                                    (_heat[((x + 0) & 15) + ((y + 0) & 15) * 16] + _heat[((x + 1) & 15) + ((y + 0) & 15) * 16] + _heat[((x + 1) & 15) + ((y + 1) & 15) * 16] + _heat[((x + 0) & 15) + ((y + 1) & 15) * 16]) / 4.0F * 0.8F;
                _heat[x + y * 16] += _heatDelta[x + y * 16] * 0.01F;

                if (_heat[x + y * 16] < 0.0F)
                {
                    _heat[x + y * 16] = 0.0F;
                }

                _heatDelta[x + y * 16] -= 0.06F;

                if (Random.Shared.NextDouble() < 0.005D)
                {
                    _heatDelta[x + y * 16] = 1.5F;
                }
            }
        }

        (_next, _current) = (_current, _next);

        for (int pixelIndex = 0; pixelIndex < 256; ++pixelIndex)
        {
            float intensity = _current[pixelIndex] * 2.0F;

            if (intensity > 1.0F)
            {
                intensity = 1.0F;
            }

            if (intensity < 0.0F)
            {
                intensity = 0.0F;
            }

            int r = (int)(intensity * 100.0F + 155.0F);
            int g = (int)(intensity * intensity * 255.0F);
            int b = (int)(intensity * intensity * intensity * intensity * 128.0F);

            Pixels[pixelIndex * 4 + 0] = (byte)r;
            Pixels[pixelIndex * 4 + 1] = (byte)g;
            Pixels[pixelIndex * 4 + 2] = (byte)b;
            Pixels[pixelIndex * 4 + 3] = 255;
        }
    }
}
