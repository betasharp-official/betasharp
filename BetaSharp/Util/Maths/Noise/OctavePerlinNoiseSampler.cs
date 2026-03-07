namespace BetaSharp.Util.Maths.Noise;

internal class OctavePerlinNoiseSampler : NoiseSampler
{

    private readonly PerlinNoiseSampler[] _octaves;
    private readonly int _octaveCount;

    public OctavePerlinNoiseSampler(JavaRandom rand, int octaveCount)
    {
        _octaveCount = octaveCount;
        _octaves = new PerlinNoiseSampler[octaveCount];

        for (int i = 0; i < octaveCount; ++i)
        {
            _octaves[i] = new PerlinNoiseSampler(rand);
        }
    }

    public double generateNoise(double x, double y)
    {
        double value = 0.0D;
        double amplitude = 1.0D;

        for (int i = 0; i < _octaveCount; ++i)
        {
            value += _octaves[i].generateNoise(x * amplitude, y * amplitude) / amplitude;
            amplitude /= 2.0D;
        }

        return value;
    }

    public double[] create(double[] buffer, double xStart, double yStart, double zStart, int xSize, int ySize, int zSize, double xFrequency, double yFrequency, double zFrequency)
    {
        if (buffer == null)
        {
            buffer = new double[xSize * ySize * zSize];
        }
        else
        {
            Array.Fill(buffer, 0);
        }

        double octaveMultiplier = 1.0D;

        for (int i = 0; i < _octaveCount; ++i)
        {
            _octaves[i]
                .sample(buffer,
                    xStart,
                    yStart,
                    zStart,
                    xSize,
                    ySize,
                    zSize,
                    xFrequency * octaveMultiplier,
                    yFrequency * octaveMultiplier,
                    zFrequency * octaveMultiplier,
                    octaveMultiplier);
            octaveMultiplier /= 2.0D;
        }

        return buffer;
    }

    // The last argument goes unused, but if it were used, it would definitely be that.
    public double[] create(double[] buffer, int xStart, int zStart, int xSize, int zSize, double xFrequency, double zFrequency, double inverseAmplitude)
    {
        return create(buffer, xStart, 10.0D, zStart, xSize, 1, zSize, xFrequency, 1.0D, zFrequency);
    }
}
