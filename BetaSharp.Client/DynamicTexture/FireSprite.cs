using BetaSharp.Blocks;

namespace BetaSharp.Client.DynamicTexture;

internal class FireSprite(int index) : Rendering.Core.Textures.DynamicTexture(Block.Fire.textureId + index * 16)
{
    private float[] _current = new float[320];
    private float[] _next = new float[320];

    public override void Setup(BetaSharp game)
    {
        Array.Clear(_current);
        Array.Clear(_next);
        TryLoadCustomTexture(game, index == 0 ? "custom_fire_e_w.png" : "custom_fire_n_s.png");
    }

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
            for (int y = 0; y < 20; ++y)
            {
                int weight = 18;
                float heat = _current[x + (y + 1) % 20 * 16] * weight;

                for (int nx = x - 1; nx <= x + 1; ++nx)
                {
                    for (int ny = y; ny <= y + 1; ++ny)
                    {
                        if (nx >= 0 && ny >= 0 && nx < 16 && ny < 20)
                        {
                            heat += _current[nx + ny * 16];
                        }

                        ++weight;
                    }
                }

                _next[x + y * 16] = heat / (weight * 1.06F);

                if (y >= 19)
                {
                    _next[x + y * 16] = (float)(Random.Shared.NextDouble() * Random.Shared.NextDouble() * Random.Shared.NextDouble() * 4.0D + Random.Shared.NextDouble() * 0.1F + 0.2F);
                }
            }
        }

        (_next, _current) = (_current, _next);

        for (int pixelIndex = 0; pixelIndex < 256; ++pixelIndex)
        {
            float intensity = _current[pixelIndex] * 1.8F;

            if (intensity > 1.0F)
            {
                intensity = 1.0F;
            }

            if (intensity < 0.0F)
            {
                intensity = 0.0F;
            }

            int r = (int)(intensity * 155.0F + 100.0F);
            int g = (int)(intensity * intensity * 255.0F);
            int b = (int)(intensity * intensity * intensity * intensity * intensity * intensity * intensity * intensity * intensity * intensity * 255.0F);
            short a = 255;

            if (intensity < 0.5F)
            {
                a = 0;
            }

            Pixels[pixelIndex * 4 + 0] = (byte)r;
            Pixels[pixelIndex * 4 + 1] = (byte)g;
            Pixels[pixelIndex * 4 + 2] = (byte)b;
            Pixels[pixelIndex * 4 + 3] = (byte)a;
        }
    }
}
