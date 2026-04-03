namespace BetaSharp.Util.Maths.Noise;

internal class OctaveSimplexNoiseSampler : NoiseSampler
{
    private readonly SimplexNoiseSampler[] _octaves;
    private readonly int _octaveCount;

    public OctaveSimplexNoiseSampler(JavaRandom rand, int octaveCount)
    {
        _octaveCount = octaveCount;
        _octaves = new SimplexNoiseSampler[octaveCount];

        for (int i = 0; i < octaveCount; ++i)
        {
            _octaves[i] = new SimplexNoiseSampler(rand);
        }

    }

    public double[] sample(double[] buffer, double x, double z, int width, int depth, double xFrequency, double zFrequency, double frequencyScaler)
    {
        return sample(buffer, x, z, width, depth, xFrequency, zFrequency, frequencyScaler, 0.5D);
    }

    public double[] sample(double[] buffer, double x, double z, int width, int depth, double xFrequency, double zFrequency, double frequencyScaler, double amplitudeScaler)
    {
        xFrequency /= 1.5D;
        zFrequency /= 1.5D;
        if (buffer != null && buffer.Length >= width * depth)
        {
            for (int i = 0; i < buffer.Length; ++i)
            {
                buffer[i] = 0.0D;
            }
        }
        else
        {
            buffer = new double[width * depth];
        }

        double amplitudeDivisor = 1.0D;
        double frequencyMultiplier = 1.0D;

        for (int i = 0; i < _octaveCount; ++i)
        {
            _octaves[i].sample(buffer,
                x, z, width, depth,
                xFrequency * frequencyMultiplier,
                zFrequency * frequencyMultiplier,
                0.55D / amplitudeDivisor);
            frequencyMultiplier *= frequencyScaler;
            amplitudeDivisor *= amplitudeScaler;
        }

        return buffer;
    }
}
