namespace BetaSharp.Util.Maths.Noise;

internal class SimplexNoiseSampler
{
    private static readonly int[][] grads = [[1, 1, 0], [-1, 1, 0], [1, -1, 0], [-1, -1, 0], [1, 0, 1], [-1, 0, 1], [1, 0, -1], [-1, 0, -1], [0, 1, 1], [0, -1, 1], [0, 1, -1], [0, -1, -1]];
    private readonly int[] _permutations;
    private readonly double _xCoord;
    private readonly double _yCoord;
    private readonly double _zCoord;
    private static readonly double F2 = 0.5D * (Math.Sqrt(3.0D) - 1.0D);
    private static readonly double G2 = (3.0D - Math.Sqrt(3.0D)) / 6.0D;

    public SimplexNoiseSampler() : this(new())
    {
    }

    public SimplexNoiseSampler(JavaRandom rand)
    {
        _permutations = new int[512];
        _xCoord = rand.NextDouble() * 256.0D;
        _yCoord = rand.NextDouble() * 256.0D;
        _zCoord = rand.NextDouble() * 256.0D;

        // Fill perm with values from 0 to 255 in random order, duplicating the first 256 values to the end of the array
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

    private static int floor(double num)
    {
        return num > 0.0D ? (int)num : (int)num - 1;
    }

    private static double dot(int[] gradient, double dx, double dy)
    {
        return gradient[0] * dx + gradient[1] * dy;
    }

    public void sample(double[] buffer, double x, double z, int width, int depth, double xFrequency, double zFrequency, double amplitude)
    {
        int counter = 0;

        for (int x1 = 0; x1 < width; ++x1)
        {
            double x2 = (x + x1) * xFrequency + _xCoord;

            for (int z1 = 0; z1 < depth; ++z1)
            {
                double z2 = (z + z1) * zFrequency + _yCoord;
                double s = (x2 + z2) * F2;
                int i = floor(x2 + s);
                int j = floor(z2 + s);
                double t = (i + j) * G2;
                double x3 = i - t;
                double z3 = j - t;
                double x4 = x2 - x3;
                double z4 = z2 - z3;
                byte i1;
                byte j1;
                if (x4 > z4)
                {
                    i1 = 1;
                    j1 = 0;
                }
                else
                {
                    i1 = 0;
                    j1 = 1;
                }

                double x5 = x4 - i1 + G2;
                double z5 = z4 - j1 + G2;
                double x6 = x4 - 1.0D + 2.0D * G2;
                double z6 = z4 - 1.0D + 2.0D * G2;
                int ii = i & 255;
                int jj = j & 255;
                int gi0 = _permutations[ii + _permutations[jj]] % 12;
                int gi1 = _permutations[ii + i1 + _permutations[jj + j1]] % 12;
                int gi2 = _permutations[ii + 1 + _permutations[jj + 1]] % 12;
                double t0 = 0.5D - x4 * x4 - z4 * z4;
                double n0;
                if (t0 < 0.0D)
                {
                    n0 = 0.0D;
                }
                else
                {
                    t0 *= t0;
                    n0 = t0 * t0 * dot(grads[gi0], x4, z4);
                }

                double t1 = 0.5D - x5 * x5 - z5 * z5;
                double n1;
                if (t1 < 0.0D)
                {
                    n1 = 0.0D;
                }
                else
                {
                    t1 *= t1;
                    n1 = t1 * t1 * dot(grads[gi1], x5, z5);
                }

                double t2 = 0.5D - x6 * x6 - z6 * z6;
                double n2;
                if (t2 < 0.0D)
                {
                    n2 = 0.0D;
                }
                else
                {
                    t2 *= t2;
                    n2 = t2 * t2 * dot(grads[gi2], x6, z6);
                }

                buffer[counter++] += 70.0D * (n0 + n1 + n2) * amplitude;
            }
        }
    }
}
