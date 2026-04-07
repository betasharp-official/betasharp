using BetaSharp.Blocks;
using BetaSharp.Blocks.Materials;
using BetaSharp.Util.Maths;
using BetaSharp.Util.Maths.Noise;
using BetaSharp.Worlds.Biomes.Source;
using BetaSharp.Worlds.Chunks;
using BetaSharp.Worlds.Core.Systems;
using BetaSharp.Worlds.Generation.Biomes;
using BetaSharp.Worlds.Generation.Generators.Carvers;
using BetaSharp.Worlds.Generation.Generators.Features;

namespace BetaSharp.Worlds.Gen.Chunks;

internal class OverworldChunkGenerator : IChunkSource
{
    private readonly BiomeSource _biomeSource;
    private readonly Carver _carver = new CaveCarver();
    private readonly OctavePerlinNoiseSampler _depthNoise;
    private readonly OctavePerlinNoiseSampler _floatingIslandNoise;
    private readonly OctavePerlinNoiseSampler _floatingIslandScale;
    private readonly OctavePerlinNoiseSampler _forestNoise;
    private readonly IWorldContext _level;
    private readonly OctavePerlinNoiseSampler _maxLimitPerlinNoise;
    private readonly OctavePerlinNoiseSampler _minLimitPerlinNoise;
    private readonly JavaRandom _random;
    private readonly OctavePerlinNoiseSampler _sandGravelNoise;

    // Seed and per-instance biome source (allows thread-safe parallel generation)
    private readonly long _seed;
    private readonly OctavePerlinNoiseSampler _selectorNoise;
    private Biome[] _biomes;
    private double[] _depthBuffer = new double[256];
    private double[] _depthNoiseBuffer;
    private PlantPatchFeature _featureBrownMushroom;
    private CactusPatchFeature _featureCactus;
    private ClayOreFeature _featureClay;
    private OreFeature _featureCoal;
    private PlantPatchFeature _featureDandelion;
    private DeadBushPatchFeature _featureDeadBush;
    private OreFeature _featureDiamond;
    private OreFeature _featureDirt;
    private DungeonFeature _featureDungeon;
    private OreFeature _featureGold;
    private GrassPatchFeature _featureGrass1;
    private GrassPatchFeature _featureGrass2;
    private OreFeature _featureGravel;
    private OreFeature _featureIron;
    private OreFeature _featureLapis;
    private LakeFeature _featureLavaLake;
    private SpringFeature _featureLavaSpring;
    private PumpkinPatchFeature _featurePumpkin;
    private PlantPatchFeature _featureRedMushroom;
    private OreFeature _featureRedstone;
    private PlantPatchFeature _featureRose;
    private SugarCanePatchFeature _featureSugarcane;

    // Pre-allocated feature instances reused across every decorated chunk
    private LakeFeature _featureWaterLake;
    private SpringFeature _featureWaterSpring;
    private double[] _gravelBuffer = new double[256];
    private double[] _heightMap;
    private double[] _maxLimitPerlinNoiseBuffer;
    private double[] _minLimitPerlinNoiseBuffer;
    private double[] _sandBuffer = new double[256];
    private double[] _scaleNoiseBuffer;
    private double[] _selectorNoiseBuffer;
    private double[] _temperatures;

    public OverworldChunkGenerator(IWorldContext world, long seed)
    {
        _level = world;
        _random = new JavaRandom(seed);
        _minLimitPerlinNoise = new OctavePerlinNoiseSampler(_random, 16);
        _maxLimitPerlinNoise = new OctavePerlinNoiseSampler(_random, 16);
        _selectorNoise = new OctavePerlinNoiseSampler(_random, 8);
        _sandGravelNoise = new OctavePerlinNoiseSampler(_random, 4);
        _depthNoise = new OctavePerlinNoiseSampler(_random, 4);
        _floatingIslandScale = new OctavePerlinNoiseSampler(_random, 10);
        _floatingIslandNoise = new OctavePerlinNoiseSampler(_random, 16);
        _forestNoise = new OctavePerlinNoiseSampler(_random, 8);
        _seed = seed;
        _biomeSource = world.Dimension.BiomeSource;
        InitFeatures();
    }

    private OverworldChunkGenerator(IWorldContext level, long seed, BiomeSource biomeSource)
    {
        _level = level;
        _seed = seed;
        _biomeSource = biomeSource;
        _random = new JavaRandom(seed);
        _minLimitPerlinNoise = new OctavePerlinNoiseSampler(_random, 16);
        _maxLimitPerlinNoise = new OctavePerlinNoiseSampler(_random, 16);
        _selectorNoise = new OctavePerlinNoiseSampler(_random, 8);
        _sandGravelNoise = new OctavePerlinNoiseSampler(_random, 4);
        _depthNoise = new OctavePerlinNoiseSampler(_random, 4);
        _floatingIslandScale = new OctavePerlinNoiseSampler(_random, 10);
        _floatingIslandNoise = new OctavePerlinNoiseSampler(_random, 16);
        _forestNoise = new OctavePerlinNoiseSampler(_random, 8);
        InitFeatures();
    }

    // Creates a thread-safe parallel generator with its own BiomeSource and _random state.
    // All noise samplers are deterministically equivalent (same seed), so chunk output is identical.
    public IChunkSource CreateParallelInstance()
        => new OverworldChunkGenerator(_level, _seed, new BiomeSource(_level));

    public Chunk LoadChunk(int chunkX, int chunkZ) => GetChunk(chunkX, chunkZ);

    /// <summary>
    ///     Generates a chunk at the given coordinates. The chunk is generated by first creating a low-resolution height map,
    ///     then interpolating it to determine the base terrain, and finally carving caves and adding features to it.
    /// </summary>
    /// <param name="chunkX">The x-coordinate of the chunk</param>
    /// <param name="chunkZ">The z-coordinate of the chunk</param>
    /// <returns>The generated chunk</returns>
    public Chunk GetChunk(int chunkX, int chunkZ)
    {
        _random.SetSeed(chunkX * 341873128712L + chunkZ * 132897987541L);
        byte[] blocks = new byte[-short.MinValue];
        Chunk chunk = new(_level, blocks, chunkX, chunkZ);
        _biomes = _biomeSource.GetBiomesInArea(_biomes, chunkX * 16, chunkZ * 16, 16, 16);
        double[] temperatureMap = _biomeSource.TemperatureMap;
        BuildTerrain(chunkX, chunkZ, blocks, _biomes, temperatureMap);
        BuildSurfaces(chunkX, chunkZ, blocks, _biomes);
        _carver.carve(this, _level, chunkX, chunkZ, blocks);
        chunk.PopulateHeightMap();
        return chunk;
    }

    public bool IsChunkLoaded(int x, int z) => true;

    /// <summary>
    ///     Generates the features of the chunk, such as ores, trees, lakes, etc. The features that are generated depend on the
    ///     biome of the chunk and some _random factors.
    /// </summary>
    /// <param name="source">The chunk source that is generating the chunk</param>
    /// <param name="chunkX">The x-coordinate of the chunk</param>
    /// <param name="chunkZ">The z-coordinate of the chunk</param>
    public void DecorateTerrain(IChunkSource source, int chunkX, int chunkZ)
    {
        BlockSand.FallInstantly = true;
        int blockX = chunkX * 16;
        int blockZ = chunkZ * 16;
        Biome chunkBiome = _biomeSource.GetBiome(blockX + 16, blockZ + 16);
        _random.SetSeed(_level.Seed);
        long xOffset = _random.NextLong() / 2L * 2L + 1L;
        long zOffset = _random.NextLong() / 2L * 2L + 1L;
        _random.SetSeed((chunkX * xOffset + chunkZ * zOffset) ^ _level.Seed);
        double fraction;
        int featureX;
        int featureY;
        int featureZ;

        // Generate lakes
        if (_random.NextInt(4) == 0)
        {
            featureX = blockX + _random.NextInt(16) + 8;
            featureY = _random.NextInt(128);
            featureZ = blockZ + _random.NextInt(16) + 8;
            _featureWaterLake.Generate(_level, _random, featureX, featureY, featureZ);
        }

        // Generate lava lakes
        if (_random.NextInt(8) == 0)
        {
            featureX = blockX + _random.NextInt(16) + 8;
            featureY = _random.NextInt(_random.NextInt(120) + 8);
            featureZ = blockZ + _random.NextInt(16) + 8;
            if (featureY < 64 || _random.NextInt(10) == 0)
            {
                _featureLavaLake.Generate(_level, _random, featureX, featureY, featureZ);
            }
        }

        // Generate Dungeons
        for (int i = 0; i < 8; ++i)
        {
            featureX = blockX + _random.NextInt(16) + 8;
            featureY = _random.NextInt(128);
            featureZ = blockZ + _random.NextInt(16) + 8;
            _featureDungeon.Generate(_level, _random, featureX, featureY, featureZ);
        }

        // Generate Clay patches
        for (int i = 0; i < 10; ++i)
        {
            featureX = blockX + _random.NextInt(16);
            featureY = _random.NextInt(128);
            featureZ = blockZ + _random.NextInt(16);
            _featureClay.Generate(_level, _random, featureX, featureY, featureZ);
        }

        // Generate Dirt blobs
        for (int i = 0; i < 20; ++i)
        {
            featureX = blockX + _random.NextInt(16);
            featureY = _random.NextInt(128);
            featureZ = blockZ + _random.NextInt(16);
            _featureDirt.Generate(_level, _random, featureX, featureY, featureZ);
        }

        // Generate Gravel blobs
        for (int i = 0; i < 10; ++i)
        {
            featureX = blockX + _random.NextInt(16);
            featureY = _random.NextInt(128);
            featureZ = blockZ + _random.NextInt(16);
            _featureGravel.Generate(_level, _random, featureX, featureY, featureZ);
        }

        // Generate Coal Ore Veins
        for (int i = 0; i < 20; ++i)
        {
            featureX = blockX + _random.NextInt(16);
            featureY = _random.NextInt(128);
            featureZ = blockZ + _random.NextInt(16);
            _featureCoal.Generate(_level, _random, featureX, featureY, featureZ);
        }

        // Generate Iron Ore Veins
        for (int i = 0; i < 20; ++i)
        {
            featureX = blockX + _random.NextInt(16);
            featureY = _random.NextInt(64);
            featureZ = blockZ + _random.NextInt(16);
            _featureIron.Generate(_level, _random, featureX, featureY, featureZ);
        }

        // Generate Gold Ore Veins
        for (int i = 0; i < 2; ++i)
        {
            featureX = blockX + _random.NextInt(16);
            featureY = _random.NextInt(32);
            featureZ = blockZ + _random.NextInt(16);
            _featureGold.Generate(_level, _random, featureX, featureY, featureZ);
        }

        // Generate Redstone Ore Veins
        for (int i = 0; i < 8; ++i)
        {
            featureX = blockX + _random.NextInt(16);
            featureY = _random.NextInt(16);
            featureZ = blockZ + _random.NextInt(16);
            _featureRedstone.Generate(_level, _random, featureX, featureY, featureZ);
        }

        // Generate Diamond Ore Veins
        for (int i = 0; i < 1; ++i)
        {
            featureX = blockX + _random.NextInt(16);
            featureY = _random.NextInt(16);
            featureZ = blockZ + _random.NextInt(16);
            _featureDiamond.Generate(_level, _random, featureX, featureY, featureZ);
        }

        // Generate Lapis Lazuli Ore Veins
        for (int i = 0; i < 1; ++i)
        {
            featureX = blockX + _random.NextInt(16);
            featureY = _random.NextInt(16);
            featureZ = blockZ + _random.NextInt(16);
            _featureLapis.Generate(_level, _random, featureX, featureY, featureZ);
        }

        // Determine the number and type of trees that should be generated
        fraction = 0.5D;
        int treeDensitySample = (int)((_forestNoise.generateNoise(blockX * fraction, blockZ * fraction) / 8.0D + _random.NextDouble() * 4.0D + 4.0D) / 3.0D);
        int numberOfTrees = 0;
        if (_random.NextInt(10) == 0)
        {
            ++numberOfTrees;
        }

        if (chunkBiome == Biome.Forest)
        {
            numberOfTrees += treeDensitySample + 5;
        }

        if (chunkBiome == Biome.Rainforest)
        {
            numberOfTrees += treeDensitySample + 5;
        }

        if (chunkBiome == Biome.SeasonalForest)
        {
            numberOfTrees += treeDensitySample + 2;
        }

        if (chunkBiome == Biome.Taiga)
        {
            numberOfTrees += treeDensitySample + 5;
        }

        if (chunkBiome == Biome.Desert)
        {
            numberOfTrees -= 20;
        }

        if (chunkBiome == Biome.Tundra)
        {
            numberOfTrees -= 20;
        }

        if (chunkBiome == Biome.Plains)
        {
            numberOfTrees -= 20;
        }

        for (int i = 0; i < numberOfTrees; ++i)
        {
            featureX = blockX + _random.NextInt(16) + 8;
            featureZ = blockZ + _random.NextInt(16) + 8;
            Feature treeFeature = chunkBiome.GetRandomWorldGenForTrees(_random);
            treeFeature.prepare(1.0D, 1.0D, 1.0D);
            treeFeature.Generate(_level, _random, featureX, _level.Reader.GetTopY(featureX, featureZ), featureZ);
        }

        // Choose an appropriate amount of Dandelions
        byte amountOfDandelions = 0;
        if (chunkBiome == Biome.Forest)
        {
            amountOfDandelions = 2;
        }

        if (chunkBiome == Biome.SeasonalForest)
        {
            amountOfDandelions = 4;
        }

        if (chunkBiome == Biome.Taiga)
        {
            amountOfDandelions = 2;
        }

        if (chunkBiome == Biome.Plains)
        {
            amountOfDandelions = 3;
        }


        // Generate Dandelions
        for (byte i = 0; i < amountOfDandelions; ++i)
        {
            featureX = blockX + _random.NextInt(16) + 8;
            featureY = _random.NextInt(128);
            featureZ = blockZ + _random.NextInt(16) + 8;
            _featureDandelion.Generate(_level, _random, featureX, featureY, featureZ);
        }

        byte amountOfTallgrass = 0;
        if (chunkBiome == Biome.Forest)
        {
            amountOfTallgrass = 2;
        }

        if (chunkBiome == Biome.Rainforest)
        {
            amountOfTallgrass = 10;
        }

        if (chunkBiome == Biome.SeasonalForest)
        {
            amountOfTallgrass = 2;
        }

        if (chunkBiome == Biome.Taiga)
        {
            amountOfTallgrass = 1;
        }

        if (chunkBiome == Biome.Plains)
        {
            amountOfTallgrass = 10;
        }

        // Generate Tallgrass and Ferns
        for (byte i = 0; i < amountOfTallgrass; ++i)
        {
            byte grassMeta = 1;
            if (chunkBiome == Biome.Rainforest && _random.NextInt(3) != 0)
            {
                // Fern
                grassMeta = 2;
            }

            featureX = blockX + _random.NextInt(16) + 8;
            featureY = _random.NextInt(128);
            featureZ = blockZ + _random.NextInt(16) + 8;
            (grassMeta == 1 ? _featureGrass1 : _featureGrass2).Generate(_level, _random, featureX, featureY, featureZ);
        }

        // Generate Deadbushes
        byte amountOfDeadBushes = 0;
        if (chunkBiome == Biome.Desert)
        {
            amountOfDeadBushes = 2;
        }

        for (byte i = 0; i < amountOfDeadBushes; ++i)
        {
            featureX = blockX + _random.NextInt(16) + 8;
            featureY = _random.NextInt(128);
            featureZ = blockZ + _random.NextInt(16) + 8;
            _featureDeadBush.Generate(_level, _random, featureX, featureY, featureZ);
        }

        // Generate Roses
        if (_random.NextInt(2) == 0)
        {
            featureX = blockX + _random.NextInt(16) + 8;
            featureY = _random.NextInt(128);
            featureZ = blockZ + _random.NextInt(16) + 8;
            _featureRose.Generate(_level, _random, featureX, featureY, featureZ);
        }

        // Generate Brown Mushrooms
        if (_random.NextInt(4) == 0)
        {
            featureX = blockX + _random.NextInt(16) + 8;
            featureY = _random.NextInt(128);
            featureZ = blockZ + _random.NextInt(16) + 8;
            _featureBrownMushroom.Generate(_level, _random, featureX, featureY, featureZ);
        }

        // Generate Red Mushrooms
        if (_random.NextInt(8) == 0)
        {
            featureX = blockX + _random.NextInt(16) + 8;
            featureY = _random.NextInt(128);
            featureZ = blockZ + _random.NextInt(16) + 8;
            _featureRedMushroom.Generate(_level, _random, featureX, featureY, featureZ);
        }

        // Generate Sugarcane
        for (int i = 0; i < 10; ++i)
        {
            featureX = blockX + _random.NextInt(16) + 8;
            featureY = _random.NextInt(128);
            featureZ = blockZ + _random.NextInt(16) + 8;
            _featureSugarcane.Generate(_level, _random, featureX, featureY, featureZ);
        }

        // Generate Pumpkin Patches
        if (_random.NextInt(32) == 0)
        {
            featureX = blockX + _random.NextInt(16) + 8;
            featureY = _random.NextInt(128);
            featureZ = blockZ + _random.NextInt(16) + 8;
            _featurePumpkin.Generate(_level, _random, featureX, featureY, featureZ);
        }

        // Generate Cacti
        byte amountOfCacti = 0;
        if (chunkBiome == Biome.Desert)
        {
            amountOfCacti += 10;
        }

        for (int i = 0; i < amountOfCacti; ++i)
        {
            featureX = blockX + _random.NextInt(16) + 8;
            featureY = _random.NextInt(128);
            featureZ = blockZ + _random.NextInt(16) + 8;
            _featureCactus.Generate(_level, _random, featureX, featureY, featureZ);
        }

        // Generate one-block water sources
        for (int i = 0; i < 50; ++i)
        {
            featureX = blockX + _random.NextInt(16) + 8;
            featureY = _random.NextInt(_random.NextInt(120) + 8);
            featureZ = blockZ + _random.NextInt(16) + 8;
            _featureWaterSpring.Generate(_level, _random, featureX, featureY, featureZ);
        }

        // Generate one-block lava sources
        for (int x = 0; x < 20; ++x)
        {
            featureX = blockX + _random.NextInt(16) + 8;
            featureY = _random.NextInt(_random.NextInt(_random.NextInt(112) + 8) + 8);
            featureZ = blockZ + _random.NextInt(16) + 8;
            _featureLavaSpring.Generate(_level, _random, featureX, featureY, featureZ);
        }

        // Place Snow in cold regions
        _temperatures = _biomeSource.GetTemperatures(_temperatures, blockX + 8, blockZ + 8, 16, 16);

        for (int x = blockX + 8; x < blockX + 8 + 16; ++x)
        {
            for (int z = blockZ + 8; z < blockZ + 8 + 16; ++z)
            {
                int offsetX = x - (blockX + 8);
                int offsetZ = z - (blockZ + 8);
                int var22 = _level.Reader.GetTopSolidBlockY(x, z);
                double temperatureSample = _temperatures[offsetX * 16 + offsetZ] - (var22 - 64) / 64.0D * 0.3D;
                if (temperatureSample < 0.5D && var22 > 0 && var22 < 128 && _level.Reader.IsAir(x, var22, z) && _level.Reader.GetMaterial(x, var22 - 1, z).BlocksMovement &&
                    _level.Reader.GetMaterial(x, var22 - 1, z) != Material.Ice)
                {
                    _level.Writer.SetBlock(x, var22, z, Block.Snow.id, 0, doUpdate: false);
                }
            }
        }

        BlockSand.FallInstantly = false;
    }

    public bool Save(bool saveEntities, LoadingDisplay display) => true;

    public bool Tick() => false;

    public bool CanSave() => true;

    public string GetDebugInfo() => "RandomLevelSource";

    private void InitFeatures()
    {
        _featureWaterLake = new LakeFeature(Block.Water.id);
        _featureLavaLake = new LakeFeature(Block.Lava.id);
        _featureDungeon = new DungeonFeature();
        _featureClay = new ClayOreFeature(32);
        _featureDirt = new OreFeature(Block.Dirt.id, 32);
        _featureGravel = new OreFeature(Block.Gravel.id, 32);
        _featureCoal = new OreFeature(Block.CoalOre.id, 16);
        _featureIron = new OreFeature(Block.IronOre.id, 8);
        _featureGold = new OreFeature(Block.GoldOre.id, 8);
        _featureRedstone = new OreFeature(Block.RedstoneOre.id, 7);
        _featureDiamond = new OreFeature(Block.DiamondOre.id, 7);
        _featureLapis = new OreFeature(Block.LapisOre.id, 6);
        _featureDandelion = new PlantPatchFeature(Block.Dandelion.id);
        _featureGrass1 = new GrassPatchFeature(Block.Grass.id, 1);
        _featureGrass2 = new GrassPatchFeature(Block.Grass.id, 2);
        _featureDeadBush = new DeadBushPatchFeature(Block.DeadBush.id);
        _featureRose = new PlantPatchFeature(Block.Rose.id);
        _featureBrownMushroom = new PlantPatchFeature(Block.BrownMushroom.id);
        _featureRedMushroom = new PlantPatchFeature(Block.RedMushroom.id);
        _featureSugarcane = new SugarCanePatchFeature();
        _featurePumpkin = new PumpkinPatchFeature();
        _featureCactus = new CactusPatchFeature();
        _featureWaterSpring = new SpringFeature(Block.FlowingWater.id);
        _featureLavaSpring = new SpringFeature(Block.FlowingLava.id);
    }

    /// <summary>
    ///     Generate the base terrain
    /// </summary>
    /// <param name="chunkX">X-Coordinate of this chunk</param>
    /// <param name="chunkZ">Z-Coordinate of this chunk</param>
    /// <param name="blocks">1D Array of Blocks within this chunk</param>
    /// <param name="biomes">1D Array of Biome values within this chunk</param>
    /// <param name="temperatures">1D Array of Temperature values within this chunk</param>
    /// <returns>The interpolated result.</returns>
    public void BuildTerrain(int chunkX, int chunkZ, byte[] blocks, Biome[] biomes, double[] temperatures)
    {
        // TODO: Replace some of these with global-constants
        //const byte vertScale = 8; // ChunkHeight / 8 = 16 (?)
        const byte horiScale = 4; // ChunkWidth / 4 = 4
        const byte halfChunkHeight = 64;
        const int xMax = horiScale + 1; // ChunkWidth / 4 + 1
        const byte yMax = 17; // ChunkHeight / 8 + 1
        const int zMax = horiScale + 1; // ChunkWidth / 4 + 1

        // Generate 4x16x4 low resolution noise map
        _heightMap = GenerateHeightMap(_heightMap, chunkX * horiScale, 0, chunkZ * horiScale, xMax, yMax, zMax);

        // Terrain noise is trilinearly interpolated and only sampled every 4 blocks
        for (int sampleX = 0; sampleX < horiScale; ++sampleX)
        {
            for (int sampleZ = 0; sampleZ < horiScale; ++sampleZ)
            {
                // Chunk Height / 8 = 16
                for (int sampleY = 0; sampleY < 16; ++sampleY)
                {
                    const double verticalLerpStep = 0.125D;
                    double corner000 = _heightMap[((sampleX + 0) * zMax + sampleZ + 0) * yMax + sampleY + 0];
                    double corner010 = _heightMap[((sampleX + 0) * zMax + sampleZ + 1) * yMax + sampleY + 0];
                    double corner100 = _heightMap[((sampleX + 1) * zMax + sampleZ + 0) * yMax + sampleY + 0];
                    double corner110 = _heightMap[((sampleX + 1) * zMax + sampleZ + 1) * yMax + sampleY + 0];
                    double corner001 = (_heightMap[((sampleX + 0) * zMax + sampleZ + 0) * yMax + sampleY + 1] - corner000) * verticalLerpStep;
                    double corner011 = (_heightMap[((sampleX + 0) * zMax + sampleZ + 1) * yMax + sampleY + 1] - corner010) * verticalLerpStep;
                    double corner101 = (_heightMap[((sampleX + 1) * zMax + sampleZ + 0) * yMax + sampleY + 1] - corner100) * verticalLerpStep;
                    double corner111 = (_heightMap[((sampleX + 1) * zMax + sampleZ + 1) * yMax + sampleY + 1] - corner110) * verticalLerpStep;

                    // Interpolate the 1/4th scale noise
                    for (int subY = 0; subY < 8; ++subY)
                    {
                        const double horizontalLerpStep = 0.25D; // 1.0 / horiScale
                        double terrainX0 = corner000;
                        double terrainX1 = corner010;
                        double terrainStepX0 = (corner100 - corner000) * horizontalLerpStep;
                        double terrainStepX1 = (corner110 - corner010) * horizontalLerpStep;

                        for (int subX = 0; subX < 4; ++subX)
                        {
                            int blockIndex = ((subX + sampleX * 4) << 11) | ((sampleZ * 4) << 7) | (sampleY * 8 + subY);
                            const short chunkHeight = 128; // Chunk Height
                            double terrainDensity = terrainX0;
                            double densityStepZ = (terrainX1 - terrainX0) * horizontalLerpStep;

                            for (int subZ = 0; subZ < 4; ++subZ)
                            {
                                // Here the actual block is determined
                                // Default to air block
                                int blockType = 0;

                                // If water is too cold, turn into ice
                                double temp = temperatures[(sampleX * 4 + subX) * 16 + sampleZ * 4 + subZ];
                                if (sampleY * 8 + subY < halfChunkHeight)
                                {
                                    if (temp < 0.5D && sampleY * 8 + subY >= halfChunkHeight - 1)
                                    {
                                        blockType = Block.Ice.id;
                                    }
                                    else
                                    {
                                        blockType = Block.Water.id;
                                    }
                                }

                                // If the terrain density is above 0.0,
                                // turn it into stone
                                if (terrainDensity > 0.0D)
                                {
                                    blockType = Block.Stone.id;
                                }

                                blocks[blockIndex] = (byte)blockType;
                                blockIndex += chunkHeight;
                                terrainDensity += densityStepZ;
                            }

                            terrainX0 += terrainStepX0;
                            terrainX1 += terrainStepX1;
                        }

                        corner000 += corner001;
                        corner010 += corner011;
                        corner100 += corner101;
                        corner110 += corner111;
                    }
                }
            }
        }
    }

    /// <summary>
    ///     Generate the base terrain
    /// </summary>
    /// <param name="chunkX">X-Coordinate of this chunk</param>
    /// <param name="chunkZ">Z-Coordinate of this chunk</param>
    /// <param name="blocks">1D Array of Blocks within this chunk</param>
    /// <param name="biomes">1D Array of Biome values within this chunk</param>
    /// <returns>The interpolated result.</returns>
    public void BuildSurfaces(int chunkX, int chunkZ, byte[] blocks, Biome[] biomes)
    {
        byte blockZ = 64;
        double chunkBiome = 1.0D / 32.0D;
        _sandBuffer = _sandGravelNoise.create(_sandBuffer, chunkX * 16, chunkZ * 16, 0.0D, 16, 16, 1, chunkBiome, chunkBiome, 1.0D);
        _gravelBuffer = _sandGravelNoise.create(_gravelBuffer, chunkX * 16, 109.0134D, chunkZ * 16, 16, 1, 16, chunkBiome, 1.0D, chunkBiome);
        _depthBuffer = _depthNoise.create(_depthBuffer, chunkX * 16, chunkZ * 16, 0.0D, 16, 16, 1, chunkBiome * 2.0D, chunkBiome * 2.0D, chunkBiome * 2.0D);

        for (int horizontalScale = 0; horizontalScale < 16; ++horizontalScale)
        {
            for (int zOffset = 0; zOffset < 16; ++zOffset)
            {
                Biome verticalScale = biomes[horizontalScale + zOffset * 16];
                bool fraction = _sandBuffer[horizontalScale + zOffset * 16] + _random.NextDouble() * 0.2D > 0.0D;
                bool temperatureBuffer = _gravelBuffer[horizontalScale + zOffset * 16] + _random.NextDouble() * 0.2D > 3.0D;
                int featureX = (int)(_depthBuffer[horizontalScale + zOffset * 16] / 3.0D + 3.0D + _random.NextDouble() * 0.25D);
                int featureY = -1;
                byte featureZ = verticalScale.TopBlockId;
                byte scaleFraction = verticalScale.SoilBlockId;

                for (int iX = 127; iX >= 0; --iX)
                {
                    int treeFeature = (zOffset * 16 + horizontalScale) * 128 + iX;
                    if (iX <= 0 + _random.NextInt(5))
                    {
                        blocks[treeFeature] = (byte)Block.Bedrock.id;
                    }
                    else
                    {
                        byte z = blocks[treeFeature];
                        if (z == 0)
                        {
                            featureY = -1;
                        }
                        else if (z == Block.Stone.id)
                        {
                            if (featureY == -1)
                            {
                                if (featureX <= 0)
                                {
                                    featureZ = 0;
                                    scaleFraction = (byte)Block.Stone.id;
                                }
                                else if (iX >= blockZ - 4 && iX <= blockZ + 1)
                                {
                                    featureZ = verticalScale.TopBlockId;
                                    scaleFraction = verticalScale.SoilBlockId;
                                    if (temperatureBuffer)
                                    {
                                        featureZ = 0;
                                    }

                                    if (temperatureBuffer)
                                    {
                                        scaleFraction = (byte)Block.Gravel.id;
                                    }

                                    if (fraction)
                                    {
                                        featureZ = (byte)Block.Sand.id;
                                    }

                                    if (fraction)
                                    {
                                        scaleFraction = (byte)Block.Sand.id;
                                    }
                                }

                                if (iX < blockZ && featureZ == 0)
                                {
                                    featureZ = (byte)Block.Water.id;
                                }

                                featureY = featureX;
                                if (iX >= blockZ - 1)
                                {
                                    blocks[treeFeature] = featureZ;
                                }
                                else
                                {
                                    blocks[treeFeature] = scaleFraction;
                                }
                            }
                            else if (featureY > 0)
                            {
                                --featureY;
                                blocks[treeFeature] = scaleFraction;
                                if (featureY == 0 && scaleFraction == Block.Sand.id)
                                {
                                    featureY = _random.NextInt(4);
                                    scaleFraction = (byte)Block.Sandstone.id;
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    /// @brief Generates the low-resolution height map that is used to generate the terrain of the overworld. The height map is generated by sampling 5 different noise maps and applying biome-dependent modifications to them.
    ///
    /// @param terrainMap The terrain map that the scaled-down terrain values will be written to
    /// @param chunkPos The x,y,z coordinate of the sub-chunk
    /// @param max Defines the area of the terrainMap
    /// <summary>
    ///     Generates the low-resolution height map that is used to generate the terrain of the overworld. The height map is
    ///     generated by sampling 5 different noise maps and applying biome-dependent modifications to them.
    /// </summary>
    /// <param name="heightMap">The terrain map that the scaled-down terrain values will be written to</param>
    /// <param name="x">The x-coordinate of the sub-chunk</param>
    /// <param name="y">The y-coordinate of the sub-chunk</param>
    /// <param name="z">The z-coordinate of the sub-chunk</param>
    /// <param name="sizeX">The x-size of the terrainMap</param>
    /// <param name="sizeY">The y-size of the terrainMap</param>
    /// <param name="sizeZ">The z-size of the terrainMap</param>
    /// <returns>The generated height map</returns>
    private double[] GenerateHeightMap(double[]? heightMap, int x, int y, int z, int sizeX, int sizeY, int sizeZ)
    {
        if (heightMap == null)
        {
            heightMap = new double[sizeX * sizeY * sizeZ];
        }

        double horizontalScale = 684.412D;
        double verticalScale = 684.412D;
        double[] temperatureBuffer = _biomeSource.TemperatureMap;
        double[] downfallBuffer = _biomeSource.DownfallMap;
        _scaleNoiseBuffer = _floatingIslandScale.create(_scaleNoiseBuffer, x, z, sizeX, sizeZ, 1.121D, 1.121D, 0.5D);
        _depthNoiseBuffer = _floatingIslandNoise.create(_depthNoiseBuffer, x, z, sizeX, sizeZ, 200.0D, 200.0D, 0.5D);
        _selectorNoiseBuffer = _selectorNoise.create(_selectorNoiseBuffer, x, y, z, sizeX, sizeY, sizeZ, horizontalScale / 80.0D, verticalScale / 160.0D, horizontalScale / 80.0D);
        _minLimitPerlinNoiseBuffer = _minLimitPerlinNoise.create(_minLimitPerlinNoiseBuffer, x, y, z, sizeX, sizeY, sizeZ, horizontalScale, verticalScale, horizontalScale);
        _maxLimitPerlinNoiseBuffer = _maxLimitPerlinNoise.create(_maxLimitPerlinNoiseBuffer, x, y, z, sizeX, sizeY, sizeZ, horizontalScale, verticalScale, horizontalScale);
        // Used to iterate 3D noise maps (low, high, selector)
        int xyzIndex = 0;
        // Used to iterate 2D Noise maps (depth, continentalness)
        int xzIndex = 0;
        int scaleFraction = 16 / sizeX;

        for (int iX = 0; iX < sizeX; ++iX)
        {
            int sampleX = iX * scaleFraction + scaleFraction / 2;

            for (int iZ = 0; iZ < sizeZ; ++iZ)
            {
                // Sample 2D noises
                int sampleZ = iZ * scaleFraction + scaleFraction / 2;
                // Apply biome-noise-dependent variety
                double temperatureSample = temperatureBuffer[sampleX * 16 + sampleZ];
                double downfallSample = downfallBuffer[sampleX * 16 + sampleZ] * temperatureSample;
                downfallSample = 1.0D - downfallSample;
                downfallSample *= downfallSample;
                downfallSample *= downfallSample;
                downfallSample = 1.0D - downfallSample;
                // Sample scale/contientalness noise
                double scaleNoiseSample = (_scaleNoiseBuffer[xzIndex] + 256.0D) / 512.0D;
                scaleNoiseSample *= downfallSample;
                if (scaleNoiseSample > 1.0D)
                {
                    scaleNoiseSample = 1.0D;
                }

                // Sample depth noise
                double depthNoiseSample = _depthNoiseBuffer[xzIndex] / 8000.0D;
                if (depthNoiseSample < 0.0D)
                {
                    depthNoiseSample = -depthNoiseSample * 0.3D;
                }

                depthNoiseSample = depthNoiseSample * 3.0D - 2.0D;
                if (depthNoiseSample < 0.0D)
                {
                    depthNoiseSample /= 2.0D;
                    if (depthNoiseSample < -1.0D)
                    {
                        depthNoiseSample = -1.0D;
                    }

                    depthNoiseSample /= 1.4D;
                    depthNoiseSample /= 2.0D;
                    scaleNoiseSample = 0.0D;
                }
                else
                {
                    if (depthNoiseSample > 1.0D)
                    {
                        depthNoiseSample = 1.0D;
                    }

                    depthNoiseSample /= 8.0D;
                }

                if (scaleNoiseSample < 0.0D)
                {
                    scaleNoiseSample = 0.0D;
                }

                scaleNoiseSample += 0.5D;
                depthNoiseSample = depthNoiseSample * sizeY / 16.0D;
                double elevationOffset = sizeY / 2.0D + depthNoiseSample * 4.0D;
                ++xzIndex;

                for (int iY = 0; iY < sizeY; ++iY)
                {
                    double terrainDensity;
                    double densityOffset = (iY - elevationOffset) * 12.0D / scaleNoiseSample;
                    if (densityOffset < 0.0D)
                    {
                        densityOffset *= 4.0D;
                    }

                    // Sample low noise
                    double lowNoiseSample = _minLimitPerlinNoiseBuffer[xyzIndex] / 512.0D;
                    // Sample high noise
                    double highNoiseSample = _maxLimitPerlinNoiseBuffer[xyzIndex] / 512.0D;
                    // Sample selector noise
                    double selectorNoiseSample = (_selectorNoiseBuffer[xyzIndex] / 10.0D + 1.0D) / 2.0D;
                    if (selectorNoiseSample < 0.0D)
                    {
                        terrainDensity = lowNoiseSample;
                    }
                    else if (selectorNoiseSample > 1.0D)
                    {
                        terrainDensity = highNoiseSample;
                    }
                    else
                    {
                        terrainDensity = lowNoiseSample + (highNoiseSample - lowNoiseSample) * selectorNoiseSample;
                    }

                    terrainDensity -= densityOffset;
                    // Reduce density towards max height
                    if (iY > sizeY - 4)
                    {
                        double var44 = (iY - (sizeY - 4)) / 3.0F;
                        terrainDensity = terrainDensity * (1.0D - var44) + -10.0D * var44;
                    }

                    heightMap[xyzIndex] = terrainDensity;
                    ++xyzIndex;
                }
            }
        }

        return heightMap;
    }
}
