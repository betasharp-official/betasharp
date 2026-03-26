using BetaSharp.Blocks;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Biomes.Source;
using BetaSharp.Worlds.Chunks;
using BetaSharp.Worlds.Core.Systems;
using BetaSharp.Worlds.Gen.Chunks;
using Silk.NET.Maths;

namespace BetaSharp.Worlds.Dimensions;

public abstract class Dimension
{
    private readonly float[] _backgroundColor = new float[4];
    public bool EvaporatesWater = false;
    public bool HasCeiling = false;
    public int Id = 0;
    public bool IsNether = false;

    public float[] LightLevelToLuminance = new float[16];
    public IWorldContext World { get; set; } = null!;
    public BiomeSource BiomeSource { get; set; } = null!;

    public virtual float CloudHeight { get; } = 108.0F;
    public virtual bool HasGround => true;
    public virtual bool HasWorldSpawn => true;

    public void SetWorld(IWorldContext world)
    {
        World = world;
        InitBiomeSource();
        InitBrightnessTable();
    }

    protected virtual void InitBrightnessTable()
    {
        float offset = 0.05F;

        for (int i = 0; i <= 15; ++i)
        {
            float factor = 1.0F - i / 15.0F;
            LightLevelToLuminance[i] = (1.0F - factor) / (factor * 3.0F + 1.0F) * (1.0F - offset) + offset;
        }
    }

    public virtual void InitBiomeSource() => BiomeSource = new BiomeSource(World);

    public virtual IChunkSource CreateChunkGenerator() => new OverworldChunkGenerator(World, World.Seed);

    public virtual bool IsValidSpawnPoint(int x, int z) {
        int y = World.Reader.GetTopY(x, z);
        int topBlockId = World.Reader.GetBlockId(x, y, z);

        return topBlockId != 0 && Block.Blocks[topBlockId] != null && Block.Blocks[topBlockId].material.BlocksMovement;
    }

    public virtual float GetTimeOfDay(long time, float tickDelta)
    {
        int ticks = (int)(time % 24000L);
        float phase = (ticks + tickDelta) / 24000.0F - 0.25F;

        if (phase < 0.0F)
        {
            phase++;
        }

        if (phase > 1.0F)
        {
            phase--;
        }

        float phaseCopy = phase;

        phase = 1.0F - (float)((Math.Cos(phase * Math.PI) + 1.0D) / 2.0D);
        phase = phaseCopy + (phase - phaseCopy) / 3.0F;

        return phase;
    }

    public virtual float[]? GetBackgroundColor(float celestialAngle, float partialTicks)
    {
        float offset = 0.4F;
        float cosAngle = MathHelper.Cos(celestialAngle * (float)Math.PI * 2.0F);

        if (cosAngle is >= -0.4F and <= 0.4F)
        {
            float fade = cosAngle / offset * 0.5F + 0.5F;
            float multiplier = 1.0F - (1.0F - MathHelper.Sin(fade * (float)Math.PI)) * 0.99F;
            multiplier *= multiplier;

            _backgroundColor[0] = fade * 0.3F + 0.7F;
            _backgroundColor[1] = fade * fade * 0.7F + 0.2F;
            _backgroundColor[2] = fade * fade * 0.0F + 0.2F;
            _backgroundColor[3] = multiplier;

            return _backgroundColor;
        }

        return null;
    }

    public virtual Vector3D<double> GetFogColor(float celestialAngle, float partialTicks)
    {
        float cosAngle = MathHelper.Cos(celestialAngle * (float)Math.PI * 2.0F) * 2.0F + 0.5F;

        cosAngle = Math.Clamp(cosAngle, 0.0F, 1.0F);

        float r = 192.0F / 255.0F;
        float g = 216.0F / 255.0F;
        float b = 1.0F;

        r *= cosAngle * 0.94F + 0.06F;
        g *= cosAngle * 0.94F + 0.06F;
        b *= cosAngle * 0.91F + 0.09F;

        return new Vector3D<double>(r, g, b);
    }

    public static Dimension FromId(int id) => id switch
    {
        -1 => new NetherDimension(),
        0 => new OverworldDimension(),
        _ => throw new ArgumentOutOfRangeException($"Invalid Dimension:{id}")
    };
}
