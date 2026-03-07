using BetaSharp.Util.Maths;
using BetaSharp.Util.Maths.Noise;

namespace BetaSharp.Worlds.Biomes.Source;

public class BiomeSource
{
    private readonly OctaveSimplexNoiseSampler _temperatureSampler;
    private readonly OctaveSimplexNoiseSampler _downfallSampler;
    private readonly OctaveSimplexNoiseSampler _weirdnessSampler;
    public double[] TemperatureMap;
    public double[] DownfallMap;
    public double[] WeirdnessMap;
    public Biome[] Biomes;

    protected BiomeSource()
    {
    }

    public BiomeSource(World world)
    {
        _temperatureSampler = new OctaveSimplexNoiseSampler(new JavaRandom(world.getSeed() * 9871L), 4);
        _downfallSampler = new OctaveSimplexNoiseSampler(new JavaRandom(world.getSeed() * 39811L), 4);
        _weirdnessSampler = new OctaveSimplexNoiseSampler(new JavaRandom(world.getSeed() * 543321L), 2);
    }

    public virtual Biome GetBiome(ChunkPos chunkPos)
    {
        return GetBiome(chunkPos.X << 4, chunkPos.Z << 4);
    }

    public virtual Biome GetBiome(int x, int z)
    {
        return GetBiomesInArea(x, z, 1, 1)[0];
    }

    public virtual double GetTemperature(int x, int z)
    {
        TemperatureMap = _temperatureSampler.sample(TemperatureMap, x, z, 1, 1, (double)0.025F, (double)0.025F, 0.5D);
        return TemperatureMap[0];
    }

    public virtual Biome[] GetBiomesInArea(int x, int z, int width, int depth)
    {
        Biomes = GetBiomesInArea(Biomes, x, z, width, depth);
        return Biomes;
    }

    public virtual double[] GetTemperatures(double[] map, int x, int z, int width, int depth)
    {
        int size = width * depth;
        if (map == null || map.Length < size)
        {
            map = new double[size];
        }

        map = _temperatureSampler.sample(map, x, z, width, depth, (double)0.025F, (double)0.025F, 0.25D);
        WeirdnessMap = _weirdnessSampler.sample(WeirdnessMap, x, z, width, depth, 0.25D, 0.25D, 10 / 17d);
        int index = 0;

        for (int i = 0; i < width; ++i)
        {
            for (int j = 0; j < depth; ++j)
            {
                double weirdness = WeirdnessMap[index] * 1.1D + 0.5D;
                double weight = 0.01D;
                double oneMinusWeight = 1.0D - weight;
                double temperature = (map[index] * 0.15D + 0.7D) * oneMinusWeight + weirdness * weight;
                temperature = 1.0D - (1.0D - temperature) * (1.0D - temperature);
                if (temperature < 0.0D)
                {
                    temperature = 0.0D;
                }

                if (temperature > 1.0D)
                {
                    temperature = 1.0D;
                }

                map[index] = temperature;
                ++index;
            }
        }

        return map;
    }

    public virtual Biome[] GetBiomesInArea(Biome[] biomes, int x, int z, int width, int depth)
    {
        int size = width * depth;
        if (biomes == null || biomes.Length < size)
        {
            biomes = new Biome[size];
        }

        TemperatureMap = _temperatureSampler.sample(TemperatureMap, x, z, width, width, (double)0.025F, (double)0.025F, 0.25D);
        DownfallMap = _downfallSampler.sample(DownfallMap, x, z, width, width, (double)0.05F, (double)0.05F, 1.0D / 3.0D);
        WeirdnessMap = _weirdnessSampler.sample(WeirdnessMap, x, z, width, width, 0.25D, 0.25D, 0.5882352941176471D);
        int index = 0;

        for (int i = 0; i < width; ++i)
        {
            for (int j = 0; j < depth; ++j)
            {
                double weirdness = WeirdnessMap[index] * 1.1D + 0.5D;
                double weight = 0.01D;
                double oneMinusWeight = 1.0D - weight;
                double temperature = (TemperatureMap[index] * 0.15D + 0.7D) * oneMinusWeight + weirdness * weight;
                weight = 0.002D;
                oneMinusWeight = 1.0D - weight;
                double downfall = (DownfallMap[index] * 0.15D + 0.5D) * oneMinusWeight + weirdness * weight;
                temperature = 1.0D - (1.0D - temperature) * (1.0D - temperature);
                if (temperature < 0.0D)
                {
                    temperature = 0.0D;
                }

                if (downfall < 0.0D)
                {
                    downfall = 0.0D;
                }

                if (temperature > 1.0D)
                {
                    temperature = 1.0D;
                }

                if (downfall > 1.0D)
                {
                    downfall = 1.0D;
                }

                TemperatureMap[index] = temperature;
                DownfallMap[index] = downfall;
                biomes[index++] = Biome.GetBiome(temperature, downfall);
            }
        }

        return biomes;
    }
}
