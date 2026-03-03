using BetaSharp.Blocks;

namespace BetaSharp.Client.DynamicTexture;

internal class WaterSideSprite : Rendering.Core.Textures.DynamicTexture
{
    private readonly float[] _heat = new float[256];
    private readonly float[] _heatDelta = new float[256];
    private float[] _current = new float[256];
    private float[] _next = new float[256];
    private int _ticks;

    public WaterSideSprite() : base(Block.FlowingWater.textureId + 1) => Replicate = 2;

    public override void Setup(BetaSharp game) => TryLoadCustomTexture(game, "custom_water_flowing.png");

    public override void tick()
    {
        if (CustomFrames != null)
        {
            Buffer.BlockCopy(CustomFrames[CustomFrameIndex], 0, Pixels, 0, Pixels.Length);
            CustomFrameIndex = (CustomFrameIndex + 1) % CustomFrameCount;
            return;
        }

        ++_ticks;

        for (int x = 0; x < 16; ++x)
        {
            for (int y = 0; y < 16; ++y)
            {
                float accumulatedFlow = 0.0F;

                for (int ny = y - 2; ny <= y; ++ny)
                {
                    int sampleX = x & 15;
                    int sampleY = ny & 15;
                    accumulatedFlow += _current[sampleX + sampleY * 16];
                }

                _next[x + y * 16] = accumulatedFlow / 3.2F + _heat[x + y * 16] * 0.8F;
            }
        }

        for (int x = 0; x < 16; ++x)
        {
            for (int y = 0; y < 16; ++y)
            {
                _heat[x + y * 16] += _heatDelta[x + y * 16] * 0.05F;

                if (_heat[x + y * 16] < 0.0F)
                {
                    _heat[x + y * 16] = 0.0F;
                }

                _heatDelta[x + y * 16] -= 0.3F;

                if (Random.Shared.NextDouble() < 0.2D)
                {
                    _heatDelta[x + y * 16] = 0.5F;
                }
            }
        }

        (_next, _current) = (_current, _next);

        for (int pixelIndex = 0; pixelIndex < 256; ++pixelIndex)
        {
            // The "- _ticks * 16" offset animates the downward flow
            float intensity = _current[(pixelIndex - _ticks * 16) & 255];

            if (intensity > 1.0F)
            {
                intensity = 1.0F;
            }

            if (intensity < 0.0F)
            {
                intensity = 0.0F;
            }

            float intensitySq = intensity * intensity;
            int r = (int)(32.0F + intensitySq * 32.0F);
            int g = (int)(50.0F + intensitySq * 64.0F);
            int b = 255;
            int a = (int)(146.0F + intensitySq * 50.0F);

            Pixels[pixelIndex * 4 + 0] = (byte)r;
            Pixels[pixelIndex * 4 + 1] = (byte)g;
            Pixels[pixelIndex * 4 + 2] = (byte)b;
            Pixels[pixelIndex * 4 + 3] = (byte)a;
        }
    }
}
