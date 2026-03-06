using BetaSharp.Util.Maths;
using java.util;

namespace BetaSharp.Worlds.Generation.Biomes.Source;

internal class FixedBiomeSource : BiomeSource
{
    private readonly Biome _biome;
    private readonly double _downfall;
    private readonly double _temperature;

    public FixedBiomeSource(Biome biome, double temperature, double downfall)
    {
        _biome = biome;
        _temperature = temperature;
        _downfall = downfall;
    }

    public override Biome GetBiome(ChunkPos chunkPos) => _biome;

    public override Biome GetBiome(int x, int y) => _biome;

    public override double GetTemperature(int x, int y) => _temperature;

    public override Biome[] GetBiomesInArea(int x, int y, int width, int depth)
    {
        Biomes = GetBiomesInArea(Biomes, x, y, width, depth);
        return Biomes;
    }

    public override double[] GetTemperatures(double[] map, int x, int y, int width, int depth)
    {
        int size = width * depth;
        if (map == null || map.Length < size)
        {
            map = new double[size];
        }

        Arrays.fill(map, 0, size, _temperature);
        return map;
    }

    public override Biome[] GetBiomesInArea(Biome[] biomes, int x, int y, int width, int depth)
    {
        int size = width * depth;
        if (biomes == null || biomes.Length < size)
        {
            biomes = new Biome[size];
        }

        if (TemperatureMap == null || TemperatureMap.Length < size)
        {
            TemperatureMap = new double[size];
            DownfallMap = new double[size];
        }

        Arrays.fill(biomes, 0, size, _biome);
        Arrays.fill(DownfallMap, 0, size, _downfall);
        Arrays.fill(TemperatureMap, 0, size, _temperature);

        return biomes;
    }
}
