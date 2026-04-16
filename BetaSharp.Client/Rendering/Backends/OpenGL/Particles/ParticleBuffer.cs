namespace BetaSharp.Client.Rendering.Particles;

public sealed class ParticleBuffer
{
    public const int MaxParticles = 4000;

    public int Count;

    // Position (doubles for world-space accuracy)
    public readonly double[] X = new double[MaxParticles];
    public readonly double[] Y = new double[MaxParticles];
    public readonly double[] Z = new double[MaxParticles];
    public readonly double[] PrevX = new double[MaxParticles];
    public readonly double[] PrevY = new double[MaxParticles];
    public readonly double[] PrevZ = new double[MaxParticles];

    // Velocity
    public readonly double[] VelX = new double[MaxParticles];
    public readonly double[] VelY = new double[MaxParticles];
    public readonly double[] VelZ = new double[MaxParticles];

    // Per-particle visual params
    public readonly float[] Red = new float[MaxParticles];
    public readonly float[] Green = new float[MaxParticles];
    public readonly float[] Blue = new float[MaxParticles];
    public readonly float[] BaseScale = new float[MaxParticles];
    public readonly float[] Gravity = new float[MaxParticles];
    public readonly int[] TextureIndex = new int[MaxParticles];
    public readonly float[] TexJitterX = new float[MaxParticles];
    public readonly float[] TexJitterY = new float[MaxParticles];
    public readonly short[] Age = new short[MaxParticles];
    public readonly short[] MaxAge = new short[MaxParticles];
    public readonly ParticleType[] Type = new ParticleType[MaxParticles];
    public readonly bool[] OnGround = new bool[MaxParticles];
    public readonly bool[] Dead = new bool[MaxParticles];

    // Portal-specific spawn positions
    public readonly double[] SpawnX = new double[MaxParticles];
    public readonly double[] SpawnY = new double[MaxParticles];
    public readonly double[] SpawnZ = new double[MaxParticles];

    public int Add(
        ParticleType type,
        double x, double y, double z,
        double velX, double velY, double velZ,
        float red, float green, float blue,
        float baseScale, float gravity,
        int textureIndex, float texJitterX, float texJitterY,
        short maxAge)
    {
        if (Count >= MaxParticles)
        {
            SwapRemove(0); // evict to make room
        }

        int i = Count++;
        Type[i] = type;
        X[i] = x;
        Y[i] = y;
        Z[i] = z;
        PrevX[i] = x;
        PrevY[i] = y;
        PrevZ[i] = z;
        VelX[i] = velX;
        VelY[i] = velY;
        VelZ[i] = velZ;
        Red[i] = red;
        Green[i] = green;
        Blue[i] = blue;
        BaseScale[i] = baseScale;
        Gravity[i] = gravity;
        TextureIndex[i] = textureIndex;
        TexJitterX[i] = texJitterX;
        TexJitterY[i] = texJitterY;
        Age[i] = 0;
        MaxAge[i] = maxAge;
        OnGround[i] = false;
        Dead[i] = false;
        SpawnX[i] = 0;
        SpawnY[i] = 0;
        SpawnZ[i] = 0;
        return i;
    }

    public void SwapRemove(int i)
    {
        int last = Count - 1;
        if (i != last)
        {
            X[i] = X[last];
            Y[i] = Y[last];
            Z[i] = Z[last];
            PrevX[i] = PrevX[last];
            PrevY[i] = PrevY[last];
            PrevZ[i] = PrevZ[last];
            VelX[i] = VelX[last];
            VelY[i] = VelY[last];
            VelZ[i] = VelZ[last];
            Red[i] = Red[last];
            Green[i] = Green[last];
            Blue[i] = Blue[last];
            BaseScale[i] = BaseScale[last];
            Gravity[i] = Gravity[last];
            TextureIndex[i] = TextureIndex[last];
            TexJitterX[i] = TexJitterX[last];
            TexJitterY[i] = TexJitterY[last];
            Age[i] = Age[last];
            MaxAge[i] = MaxAge[last];
            Type[i] = Type[last];
            OnGround[i] = OnGround[last];
            Dead[i] = Dead[last];
            SpawnX[i] = SpawnX[last];
            SpawnY[i] = SpawnY[last];
            SpawnZ[i] = SpawnZ[last];
        }

        Count--;
    }

    public void Clear()
    {
        Count = 0;
    }
}
