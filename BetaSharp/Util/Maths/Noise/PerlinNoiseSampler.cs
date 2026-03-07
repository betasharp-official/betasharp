namespace BetaSharp.Util.Maths.Noise;

internal class PerlinNoiseSampler : NoiseSampler
{
    private readonly int[] _permutations;
    private readonly double _xCoord;
    private readonly double _yCoord;
    private readonly double _zCoord;

    public PerlinNoiseSampler() : this(new())
    {
    }

    public PerlinNoiseSampler(JavaRandom rand)
    {
        _permutations = new int[512];
        _xCoord = rand.NextDouble() * 256.0D;
        _yCoord = rand.NextDouble() * 256.0D;
        _zCoord = rand.NextDouble() * 256.0D;

        for (int i = 0; i < 256; i++)
        {
            _permutations[i] = i;
        }

        for (int i = 0; i < 256; ++i)
        {
            int j = rand.NextInt(256 - i) + i;
            (_permutations[i], _permutations[j]) = (_permutations[j], _permutations[i]);
            _permutations[i + 256] = _permutations[i];
        }

    }

    public double generateNoise(double x, double y, double z)
    {
        x += _xCoord;
        y += _yCoord;
        z += _zCoord;
        int xInt = (int)x;
        int yInt = (int)y;
        int zInt = (int)z;
        if (x < xInt)
        {
            --xInt;
        }

        if (y < yInt)
        {
            --yInt;
        }

        if (z < zInt)
        {
            --zInt;
        }

        int xMod255 = xInt & 255;
        int yMod255 = yInt & 255;
        int zMod255 = zInt & 255;
        x -= xInt;
        y -= yInt;
        z -= zInt;
        double sX = x * x * x * (x * (x * 6.0D - 15.0D) + 10.0D);
        double sY = y * y * y * (y * (y * 6.0D - 15.0D) + 10.0D);
        double sZ = z * z * z * (z * (z * 6.0D - 15.0D) + 10.0D);
        int a = _permutations[xMod255] + yMod255;
        int aa = _permutations[a] + zMod255;
        int ab = _permutations[a + 1] + zMod255;
        int b = _permutations[xMod255 + 1] + yMod255;
        int ba = _permutations[b] + zMod255;
        int bb = _permutations[b + 1] + zMod255;
        return lerp(sZ, lerp(sY,    lerp(sX,    grad(_permutations[aa],     x,      y,      z),
                                                grad(_permutations[ba],     x-1,    y,      z)),
                                    lerp(sX,    grad(_permutations[ab],     x,      y-1,    z),
                                                grad(_permutations[bb],     x-1,    y-1,    z))),
                        lerp(sY,    lerp(sX,    grad(_permutations[aa+1],   x,      y,      z-1),
                                                grad(_permutations[ba+1],   x-1,    y,      z-1)),
                                    lerp(sX,    grad(_permutations[ab+1],   x,      y-1,    z-1),
                                                grad(_permutations[bb+1],   x-1,    y-1,    z-1))));
    }

    public double lerp(double t, double a, double b)
    {
        return a + t * (b - a);
    }

    public double grad(int hash, double x, double y)
    {
        int h = hash & 15;
        double u = (1 - ((h & 8) >> 3)) * x;
        double v = h < 4 ? 0.0D : h != 12 && h != 14 ? y : x;
        return ((h & 1) == 0 ? u : -u) + ((h & 2) == 0 ? v : -v);
    }

    public double grad(int hash, double x, double y, double z)
    {
        int h = hash & 15;
        double u = h < 8 ? x : y;
        double v = h < 4 ? y : h != 12 && h != 14 ? z : x;
        return ((h & 1) == 0 ? u : -u) + ((h & 2) == 0 ? v : -v);
    }

    public double generateNoise(double x, double y)
    {
        return generateNoise(x, y, 0.0D);
    }

    public void sample(double[] buffer,
        double xStart,
        double yStart,
        double zStart,
        int xSize,
        int ySize,
        int zSize,
        double xFrequency,
        double yFrequency,
        double zFrequency,
        double inverseAmplitude)
    {
        int counter = 0;
        double amplitude = 1.0D / inverseAmplitude;

        if (ySize == 1) // 2d (xz)
        {
            for (int x = 0; x < xSize; ++x)
            {
                double xCoord = (xStart + x) * xFrequency + _xCoord;
                int xCoordInt = (int)xCoord;
                if (xCoord < xCoordInt)
                {
                    --xCoordInt;
                }

                int xMod255 = xCoordInt & 255;
                xCoord -= xCoordInt;

                double xFinal = xCoord * xCoord * xCoord * (xCoord * (xCoord * 6.0D - 15.0D) + 10.0D);

                for (int z = 0; z < zSize; ++z)
                {
                    double zCoord = (zStart + z) * zFrequency + _zCoord;
                    int zCoordInt = (int)zCoord;
                    if (zCoord < zCoordInt)
                    {
                        --zCoordInt;
                    }

                    int zMod255 = zCoordInt & 255;
                    zCoord -= zCoordInt;

                    double zFinal = zCoord * zCoord * zCoord * (zCoord * (zCoord * 6.0D - 15.0D) + 10.0D);

                    int aa = _permutations[xMod255];
                    int ab = _permutations[aa] + zMod255;
                    int ba = _permutations[xMod255 + 1];
                    int bb = _permutations[ba] + zMod255;
                    double xLerpZ0 = lerp(xFinal,   grad(_permutations[ab],     xCoord,         zCoord),
                                                    grad(_permutations[bb],     xCoord-1,   0,  zCoord));
                    double xLerpZ1 = lerp(xFinal,   grad(_permutations[ab+1],   xCoord,     0,  zCoord-1),
                                                    grad(_permutations[bb+1],   xCoord-1,   0,  zCoord-1));
                    double finalNoise = lerp(zFinal, xLerpZ0, xLerpZ1);
                    buffer[counter++] += finalNoise * amplitude;
                }
            }

        }
        else
        {
            int oldY = -1;
            // Don't move these inside the loop
            double xLerpY0Z0 = 0.0D;
            double xLerpY1Z0 = 0.0D;
            double xLerpY0Z1 = 0.0D;
            double xLerpY1Z1 = 0.0D;

            for (int x = 0; x < xSize; ++x)
            {
                double xCoord = (xStart + x) * xFrequency + _xCoord;
                int xCoordInt = (int)xCoord;
                if (xCoord < xCoordInt)
                {
                    --xCoordInt;
                }

                int xMod255 = xCoordInt & 255;
                xCoord -= xCoordInt;

                double xFinal = xCoord * xCoord * xCoord * (xCoord * (xCoord * 6.0D - 15.0D) + 10.0D);

                for (int z = 0; z < zSize; ++z)
                {
                    double zCoord = (zStart + z) * zFrequency + _zCoord;
                    int zCoordInt = (int)zCoord;
                    if (zCoord < zCoordInt)
                    {
                        --zCoordInt;
                    }

                    int zMod255 = zCoordInt & 255;
                    zCoord -= zCoordInt;

                    double zFinal = zCoord * zCoord * zCoord * (zCoord * (zCoord * 6.0D - 15.0D) + 10.0D);

                    for (int y = 0; y < ySize; ++y)
                    {
                        double yCoord = (yStart + y) * yFrequency + _yCoord;
                        int yCoordInt = (int)yCoord;
                        if (yCoord < yCoordInt)
                        {
                            --yCoordInt;
                        }

                        int yMod255 = yCoordInt & 255;
                        yCoord -= yCoordInt;

                        double yFinal = yCoord * yCoord * yCoord * (yCoord * (yCoord * 6.0D - 15.0D) + 10.0D);

                        if (y == 0 || yMod255 != oldY)
                        {
                            oldY = yMod255;
                            int a = _permutations[xMod255] + yMod255;
                            int aa = _permutations[a] + zMod255;
                            int ab = _permutations[a + 1] + zMod255;
                            int b = _permutations[xMod255 + 1] + yMod255;
                            int ba = _permutations[b] + zMod255;
                            int bb = _permutations[b + 1] + zMod255;
                            xLerpY0Z0 = lerp(xFinal,
                                        grad(_permutations[aa],     xCoord,     yCoord,     zCoord),
                                        grad(_permutations[ba],     xCoord-1,   yCoord,     zCoord));
                            xLerpY1Z0 = lerp(xFinal,
                                        grad(_permutations[ab],     xCoord,     yCoord-1,   zCoord),
                                        grad(_permutations[bb],     xCoord-1,   yCoord-1,   zCoord));
                            xLerpY0Z1 = lerp(xFinal,
                                        grad(_permutations[aa+1],   xCoord,     yCoord,     zCoord-1),
                                        grad(_permutations[ba+1],   xCoord-1,   yCoord,     zCoord-1));
                            xLerpY1Z1 = lerp(xFinal,
                                        grad(_permutations[ab+1],   xCoord,     yCoord-1,   zCoord-1),
                                        grad(_permutations[bb+1],   xCoord-1,   yCoord-1,   zCoord-1));
                        }

                        double yLerpZ0 = lerp(yFinal, xLerpY0Z0, xLerpY1Z0);
                        double yLerpZ1 = lerp(yFinal, xLerpY0Z1, xLerpY1Z1);
                        double finalNoise = lerp(zFinal, yLerpZ0, yLerpZ1);
                        buffer[counter++] += finalNoise * amplitude;
                    }
                }
            }

        }
    }
}
