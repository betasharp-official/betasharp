using BetaSharp.Util.Maths;

namespace BetaSharp.Worlds.Biomes.Source;

internal class FixedBiomeSource : BiomeSource
{

    private Biome _biome;
    private double _temperature;
    private double _downfall;

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

        Array.Fill(map, _temperature);
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

        Array.Fill(biomes, _biome);
        Array.Fill(DownfallMap, _downfall);
        Array.Fill(TemperatureMap, _temperature);

        return biomes;
    }
}
