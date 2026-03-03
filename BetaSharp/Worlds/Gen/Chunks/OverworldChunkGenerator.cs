using BetaSharp.Blocks;
using BetaSharp.Blocks.Materials;
using BetaSharp.Util.Maths;
using BetaSharp.Util.Maths.Noise;
using BetaSharp.Worlds.Biomes;
using BetaSharp.Worlds.Biomes.Source;
using BetaSharp.Worlds.Chunks;
using BetaSharp.Worlds.Gen.Carvers;
using BetaSharp.Worlds.Gen.Features;

namespace BetaSharp.Worlds.Gen.Chunks;

internal class OverworldChunkGenerator : ChunkSource
{

    private readonly JavaRandom random;
    private readonly OctavePerlinNoiseSampler minLimitPerlinNoise;
    private readonly OctavePerlinNoiseSampler maxLimitPerlinNoise;
    private readonly OctavePerlinNoiseSampler selectorNoise;
    private readonly OctavePerlinNoiseSampler sandGravelNoise;
    private readonly OctavePerlinNoiseSampler depthNoise;
    public OctavePerlinNoiseSampler floatingIslandScale;
    public OctavePerlinNoiseSampler floatingIslandNoise;
    public OctavePerlinNoiseSampler forestNoise;
    private readonly World world;
    private double[] heightMap;
    private double[] sandBuffer = new double[256];
    private double[] gravelBuffer = new double[256];
    private double[] depthBuffer = new double[256];
    private readonly Carver cave = new CaveCarver();
    private Biome[] biomes;
    double[] selectorNoiseBuffer;
    double[] minLimitPerlinNoiseBuffer;
    double[] maxLimitPerlinNoiseBuffer;
    double[] scaleNoiseBuffer;
    double[] depthNoiseBuffer;
    private double[] temperatures;

    // Seed and per-instance biome source (allows thread-safe parallel generation)
    private readonly long _seed;
    private readonly BiomeSource _biomeSource;

    // Pre-allocated feature instances reused across every decorated chunk
    private LakeFeature _featureWaterLake;
    private LakeFeature _featureLavaLake;
    private DungeonFeature _featureDungeon;
    private ClayOreFeature _featureClay;
    private OreFeature _featureDirt;
    private OreFeature _featureGravel;
    private OreFeature _featureCoal;
    private OreFeature _featureIron;
    private OreFeature _featureGold;
    private OreFeature _featureRedstone;
    private OreFeature _featureDiamond;
    private OreFeature _featureLapis;
    private PlantPatchFeature _featureDandelion;
    private GrassPatchFeature _featureGrass1;
    private GrassPatchFeature _featureGrass2;
    private DeadBushPatchFeature _featureDeadBush;
    private PlantPatchFeature _featureRose;
    private PlantPatchFeature _featureBrownMushroom;
    private PlantPatchFeature _featureRedMushroom;
    private SugarCanePatchFeature _featureSugarcane;
    private PumpkinPatchFeature _featurePumpkin;
    private CactusPatchFeature _featureCactus;
    private SpringFeature _featureWaterSpring;
    private SpringFeature _featureLavaSpring;

    public OverworldChunkGenerator(World world, long seed)
    {
        this.world = world;
        random = new JavaRandom(seed);
        minLimitPerlinNoise = new OctavePerlinNoiseSampler(random, 16);
        maxLimitPerlinNoise = new OctavePerlinNoiseSampler(random, 16);
        selectorNoise = new OctavePerlinNoiseSampler(random, 8);
        sandGravelNoise = new OctavePerlinNoiseSampler(random, 4);
        depthNoise = new OctavePerlinNoiseSampler(random, 4);
        floatingIslandScale = new OctavePerlinNoiseSampler(random, 10);
        floatingIslandNoise = new OctavePerlinNoiseSampler(random, 16);
        forestNoise = new OctavePerlinNoiseSampler(random, 8);
        _seed = seed;
        _biomeSource = world.getBiomeSource();
        InitFeatures();
    }

    // Creates a thread-safe parallel generator with its own BiomeSource and random state.
    // All noise samplers are deterministically equivalent (same seed), so chunk output is identical.
    public ChunkSource CreateParallelInstance()
        => new OverworldChunkGenerator(world, _seed, new BiomeSource(world));

    private OverworldChunkGenerator(World world, long seed, BiomeSource biomeSource)
    {
        this.world = world;
        _seed = seed;
        _biomeSource = biomeSource;
        random = new JavaRandom(seed);
        minLimitPerlinNoise = new OctavePerlinNoiseSampler(random, 16);
        maxLimitPerlinNoise = new OctavePerlinNoiseSampler(random, 16);
        selectorNoise = new OctavePerlinNoiseSampler(random, 8);
        sandGravelNoise = new OctavePerlinNoiseSampler(random, 4);
        depthNoise = new OctavePerlinNoiseSampler(random, 4);
        floatingIslandScale = new OctavePerlinNoiseSampler(random, 10);
        floatingIslandNoise = new OctavePerlinNoiseSampler(random, 16);
        forestNoise = new OctavePerlinNoiseSampler(random, 8);
        InitFeatures();
    }

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
    /// Generate the base terrain
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
        const int  xMax = horiScale + 1; // ChunkWidth / 4 + 1
        const byte yMax = 17; // ChunkHeight / 8 + 1
        const int  zMax = horiScale + 1; // ChunkWidth / 4 + 1

	    // Generate 4x16x4 low resolution noise map
        heightMap = GenerateHeightMap(heightMap, chunkX * horiScale, 0, chunkZ * horiScale, xMax, yMax, zMax);

	    // Terrain noise is trilinearly interpolated and only sampled every 4 blocks
        for (int sampleX = 0; sampleX < horiScale; ++sampleX)
        {
            for (int sampleZ = 0; sampleZ < horiScale; ++sampleZ)
            {
                // Chunk Height / 8 = 16
                for (int sampleY = 0; sampleY < 16; ++sampleY)
                {
                    const double verticalLerpStep = 0.125D;
                    double corner000 = heightMap[((sampleX + 0) * zMax + sampleZ + 0) * yMax + sampleY + 0];
                    double corner010 = heightMap[((sampleX + 0) * zMax + sampleZ + 1) * yMax + sampleY + 0];
                    double corner100 = heightMap[((sampleX + 1) * zMax + sampleZ + 0) * yMax + sampleY + 0];
                    double corner110 = heightMap[((sampleX + 1) * zMax + sampleZ + 1) * yMax + sampleY + 0];
                    double corner001 = (heightMap[((sampleX + 0) * zMax + sampleZ + 0) * yMax + sampleY + 1] - corner000) * verticalLerpStep;
                    double corner011 = (heightMap[((sampleX + 0) * zMax + sampleZ + 1) * yMax + sampleY + 1] - corner010) * verticalLerpStep;
                    double corner101 = (heightMap[((sampleX + 1) * zMax + sampleZ + 0) * yMax + sampleY + 1] - corner100) * verticalLerpStep;
                    double corner111 = (heightMap[((sampleX + 1) * zMax + sampleZ + 1) * yMax + sampleY + 1] - corner110) * verticalLerpStep;

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
                            int blockIndex = (((subX + sampleX * 4) << 11) | ((sampleZ * 4) << 7) | ((sampleY * 8) + subY));
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
    /// Generate the base terrain
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
        sandBuffer = sandGravelNoise.create(sandBuffer, chunkX * 16, chunkZ * 16, 0.0D, 16, 16, 1, chunkBiome, chunkBiome, 1.0D);
        gravelBuffer = sandGravelNoise.create(gravelBuffer, chunkX * 16, 109.0134D, chunkZ * 16, 16, 1, 16, chunkBiome, 1.0D, chunkBiome);
        depthBuffer = depthNoise.create(depthBuffer, chunkX * 16, chunkZ * 16, 0.0D, 16, 16, 1, chunkBiome * 2.0D, chunkBiome * 2.0D, chunkBiome * 2.0D);

        for (int horizontalScale = 0; horizontalScale < 16; ++horizontalScale)
        {
            for (int zOffset = 0; zOffset < 16; ++zOffset)
            {
                Biome verticalScale = biomes[horizontalScale + zOffset * 16];
                bool fraction = sandBuffer[horizontalScale + zOffset * 16] + random.NextDouble() * 0.2D > 0.0D;
                bool temperatureBuffer = gravelBuffer[horizontalScale + zOffset * 16] + random.NextDouble() * 0.2D > 3.0D;
                int featureX = (int)(depthBuffer[horizontalScale + zOffset * 16] / 3.0D + 3.0D + random.NextDouble() * 0.25D);
                int featureY = -1;
                byte featureZ = verticalScale.TopBlockId;
                byte scaleFraction = verticalScale.SoilBlockId;

                for (int iX = 127; iX >= 0; --iX)
                {
                    int treeFeature = (zOffset * 16 + horizontalScale) * 128 + iX;
                    if (iX <= 0 + random.NextInt(5))
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
                                    featureY = random.NextInt(4);
                                    scaleFraction = (byte)Block.Sandstone.id;
                                }
                            }
                        }
                    }
                }
            }
        }

    }

    public Chunk LoadChunk(int chunkX, int chunkZ)
    {
        return GetChunk(chunkX, chunkZ);
    }

    /// <summary>
    /// Generates a chunk at the given coordinates. The chunk is generated by first creating a low-resolution height map, then interpolating it to determine the base terrain, and finally carving caves and adding features to it.
    /// </summary>
    /// <param name="chunkX">The x-coordinate of the chunk</param>
    /// <param name="chunkZ">The z-coordinate of the chunk</param>
    /// <returns>The generated chunk</returns>
    public Chunk GetChunk(int chunkX, int chunkZ)
    {
        random.SetSeed(chunkX * 341873128712L + chunkZ * 132897987541L);
        byte[] blocks = new byte[-short.MinValue];
        Chunk chunk = new Chunk(world, blocks, chunkX, chunkZ);
        biomes = _biomeSource.GetBiomesInArea(biomes, chunkX * 16, chunkZ * 16, 16, 16);
        double[] temperatureMap = _biomeSource.TemperatureMap;
        BuildTerrain(chunkX, chunkZ, blocks, biomes, temperatureMap);
        BuildSurfaces(chunkX, chunkZ, blocks, biomes);
        cave.carve(this, world, chunkX, chunkZ, blocks);
        chunk.PopulateHeightMap();
        return chunk;
    }

    /**
    * @brief Generates the low-resolution height map that is used to generate the terrain of the overworld. The height map is generated by sampling 5 different noise maps and applying biome-dependent modifications to them.
    *
    * @param terrainMap The terrain map that the scaled-down terrain values will be written to
    * @param chunkPos The x,y,z coordinate of the sub-chunk
    * @param max Defines the area of the terrainMap
    */
    /// <summary>
    /// Generates the low-resolution height map that is used to generate the terrain of the overworld. The height map is generated by sampling 5 different noise maps and applying biome-dependent modifications to them.
    /// </summary>
    /// <param name="heightMap">The terrain map that the scaled-down terrain values will be written to</param>
    /// <param name="x">The x-coordinate of the sub-chunk</param>
    /// <param name="y">The y-coordinate of the sub-chunk</param>
    /// <param name="z">The z-coordinate of the sub-chunk</param>
    /// <param name="sizeX">The x-size of the terrainMap</param>
    /// <param name="sizeY">The y-size of the terrainMap</param>
    /// <param name="sizeZ">The z-size of the terrainMap</param>
    /// <returns>The generated height map</returns>
    private double[] GenerateHeightMap(double[] heightMap, int x, int y, int z, int sizeX, int sizeY, int sizeZ)
    {
        if (heightMap == null)
        {
            heightMap = new double[sizeX * sizeY * sizeZ];
        }

        double horizontalScale = 684.412D;
        double verticalScale = 684.412D;
        double[] temperatureBuffer = _biomeSource.TemperatureMap;
        double[] downfallBuffer = _biomeSource.DownfallMap;
        scaleNoiseBuffer = floatingIslandScale.create(scaleNoiseBuffer, x, z, sizeX, sizeZ, 1.121D, 1.121D, 0.5D);
        depthNoiseBuffer = floatingIslandNoise.create(depthNoiseBuffer, x, z, sizeX, sizeZ, 200.0D, 200.0D, 0.5D);
        selectorNoiseBuffer = selectorNoise.create(selectorNoiseBuffer, x, y, z, sizeX, sizeY, sizeZ, horizontalScale / 80.0D, verticalScale / 160.0D, horizontalScale / 80.0D);
        minLimitPerlinNoiseBuffer = minLimitPerlinNoise.create(minLimitPerlinNoiseBuffer, x, y, z, sizeX, sizeY, sizeZ, horizontalScale, verticalScale, horizontalScale);
        maxLimitPerlinNoiseBuffer = maxLimitPerlinNoise.create(maxLimitPerlinNoiseBuffer, x, y, z, sizeX, sizeY, sizeZ, horizontalScale, verticalScale, horizontalScale);
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
                double scaleNoiseSample = (scaleNoiseBuffer[xzIndex] + 256.0D) / 512.0D;
                scaleNoiseSample *= downfallSample;
                if (scaleNoiseSample > 1.0D)
                {
                    scaleNoiseSample = 1.0D;
                }
			    // Sample depth noise
                double depthNoiseSample = depthNoiseBuffer[xzIndex] / 8000.0D;
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
                    double terrainDensity = 0.0D;
                    double densityOffset = (iY - elevationOffset) * 12.0D / scaleNoiseSample;
                    if (densityOffset < 0.0D)
                    {
                        densityOffset *= 4.0D;
                    }

				    // Sample low noise
                    double lowNoiseSample = minLimitPerlinNoiseBuffer[xyzIndex] / 512.0D;
				    // Sample high noise
                    double highNoiseSample = maxLimitPerlinNoiseBuffer[xyzIndex] / 512.0D;
				    // Sample selector noise
                    double selectorNoiseSample = (selectorNoiseBuffer[xyzIndex] / 10.0D + 1.0D) / 2.0D;
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
                        double var44 = (double)((iY - (sizeY - 4)) / 3.0F);
                        terrainDensity = terrainDensity * (1.0D - var44) + -10.0D * var44;
                    }

                    heightMap[xyzIndex] = terrainDensity;
                    ++xyzIndex;
                }
            }
        }

        return heightMap;
    }

    public bool IsChunkLoaded(int x, int z)
    {
        return true;
    }

    /// <summary>
    /// Generates the features of the chunk, such as ores, trees, lakes, etc. The features that are generated depend on the biome of the chunk and some random factors.
    /// </summary>
    /// <param name="source">The chunk source that is generating the chunk</param>
    /// <param name="chunkX">The x-coordinate of the chunk</param>
    /// <param name="chunkZ">The z-coordinate of the chunk</param>
    public void DecorateTerrain(ChunkSource source, int chunkX, int chunkZ)
    {
        BlockSand.fallInstantly = true;
        int blockX = chunkX * 16;
        int blockZ = chunkZ * 16;
        Biome chunkBiome = _biomeSource.GetBiome(blockX + 16, blockZ + 16);
        random.SetSeed(world.getSeed());
        long xOffset = random.NextLong() / 2L * 2L + 1L;
        long zOffset = random.NextLong() / 2L * 2L + 1L;
        random.SetSeed(chunkX * xOffset + chunkZ * zOffset ^ world.getSeed());
        double fraction = 0.25D;
        int featureX;
        int featureY;
        int featureZ;

	    // Generate lakes
        if (random.NextInt(4) == 0)
        {
            featureX = blockX + random.NextInt(16) + 8;
            featureY = random.NextInt(128);
            featureZ = blockZ + random.NextInt(16) + 8;
            _featureWaterLake.Generate(world, random, featureX, featureY, featureZ);
        }

	    // Generate lava lakes
        if (random.NextInt(8) == 0)
        {
            featureX = blockX + random.NextInt(16) + 8;
            featureY = random.NextInt(random.NextInt(120) + 8);
            featureZ = blockZ + random.NextInt(16) + 8;
            if (featureY < 64 || random.NextInt(10) == 0)
            {
                _featureLavaLake.Generate(world, random, featureX, featureY, featureZ);
            }
        }

	    // Generate Dungeons
        for (int i = 0; i < 8; ++i)
        {
            featureX = blockX + random.NextInt(16) + 8;
            featureY = random.NextInt(128);
            featureZ = blockZ + random.NextInt(16) + 8;
            _featureDungeon.Generate(world, random, featureX, featureY, featureZ);
        }

	    // Generate Clay patches
        for (int i = 0; i < 10; ++i)
        {
            featureX = blockX + random.NextInt(16);
            featureY = random.NextInt(128);
            featureZ = blockZ + random.NextInt(16);
            _featureClay.Generate(world, random, featureX, featureY, featureZ);
        }

	    // Generate Dirt blobs
        for (int i = 0; i < 20; ++i)
        {
            featureX = blockX + random.NextInt(16);
            featureY = random.NextInt(128);
            featureZ = blockZ + random.NextInt(16);
            _featureDirt.Generate(world, random, featureX, featureY, featureZ);
        }

	    // Generate Gravel blobs
        for (int i = 0; i < 10; ++i)
        {
            featureX = blockX + random.NextInt(16);
            featureY = random.NextInt(128);
            featureZ = blockZ + random.NextInt(16);
            _featureGravel.Generate(world, random, featureX, featureY, featureZ);
        }

	    // Generate Coal Ore Veins
        for (int i = 0; i < 20; ++i)
        {
            featureX = blockX + random.NextInt(16);
            featureY = random.NextInt(128);
            featureZ = blockZ + random.NextInt(16);
            _featureCoal.Generate(world, random, featureX, featureY, featureZ);
        }

	    // Generate Iron Ore Veins
        for (int i = 0; i < 20; ++i)
        {
            featureX = blockX + random.NextInt(16);
            featureY = random.NextInt(64);
            featureZ = blockZ + random.NextInt(16);
            _featureIron.Generate(world, random, featureX, featureY, featureZ);
        }

	    // Generate Gold Ore Veins
        for (int i = 0; i < 2; ++i)
        {
            featureX = blockX + random.NextInt(16);
            featureY = random.NextInt(32);
            featureZ = blockZ + random.NextInt(16);
            _featureGold.Generate(world, random, featureX, featureY, featureZ);
        }

	    // Generate Redstone Ore Veins
        for (int i = 0; i < 8; ++i)
        {
            featureX = blockX + random.NextInt(16);
            featureY = random.NextInt(16);
            featureZ = blockZ + random.NextInt(16);
            _featureRedstone.Generate(world, random, featureX, featureY, featureZ);
        }

	    // Generate Diamond Ore Veins
        for (int i = 0; i < 1; ++i)
        {
            featureX = blockX + random.NextInt(16);
            featureY = random.NextInt(16);
            featureZ = blockZ + random.NextInt(16);
            _featureDiamond.Generate(world, random, featureX, featureY, featureZ);
        }

	    // Generate Lapis Lazuli Ore Veins
        for (int i = 0; i < 1; ++i)
        {
            featureX = blockX + random.NextInt(16);
            featureY = random.NextInt(16);
            featureZ = blockZ + random.NextInt(16);
            _featureLapis.Generate(world, random, featureX, featureY, featureZ);
        }

	    // Determine the number and type of trees that should be generated
        fraction = 0.5D;
        int treeDensitySample = (int)((forestNoise.generateNoise(blockX * fraction, blockZ * fraction) / 8.0D + random.NextDouble() * 4.0D + 4.0D) / 3.0D);
        int numberOfTrees = 0;
        if (random.NextInt(10) == 0)
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
            featureX = blockX + random.NextInt(16) + 8;
            featureZ = blockZ + random.NextInt(16) + 8;
            Feature treeFeature = chunkBiome.GetRandomWorldGenForTrees(random);
            treeFeature.prepare(1.0D, 1.0D, 1.0D);
            treeFeature.Generate(world, random, featureX, world.getTopY(featureX, featureZ), featureZ);
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
            featureX = blockX + random.NextInt(16) + 8;
            featureY = random.NextInt(128);
            featureZ = blockZ + random.NextInt(16) + 8;
            _featureDandelion.Generate(world, random, featureX, featureY, featureZ);
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
            if (chunkBiome == Biome.Rainforest && random.NextInt(3) != 0)
            {
                // Fern
                grassMeta = 2;
            }

            featureX = blockX + random.NextInt(16) + 8;
            featureY = random.NextInt(128);
            featureZ = blockZ + random.NextInt(16) + 8;
            (grassMeta == 1 ? _featureGrass1 : _featureGrass2).Generate(world, random, featureX, featureY, featureZ);
        }

	    // Generate Deadbushes
        byte amountOfDeadBushes = 0;
        if (chunkBiome == Biome.Desert)
        {
            amountOfDeadBushes = 2;
        }

        for (byte i = 0; i < amountOfDeadBushes; ++i)
        {
            featureX = blockX + random.NextInt(16) + 8;
            featureY = random.NextInt(128);
            featureZ = blockZ + random.NextInt(16) + 8;
            _featureDeadBush.Generate(world, random, featureX, featureY, featureZ);
        }

        // Generate Roses
        if (random.NextInt(2) == 0)
        {
            featureX = blockX + random.NextInt(16) + 8;
            featureY = random.NextInt(128);
            featureZ = blockZ + random.NextInt(16) + 8;
            _featureRose.Generate(world, random, featureX, featureY, featureZ);
        }

	    // Generate Brown Mushrooms
        if (random.NextInt(4) == 0)
        {
            featureX = blockX + random.NextInt(16) + 8;
            featureY = random.NextInt(128);
            featureZ = blockZ + random.NextInt(16) + 8;
            _featureBrownMushroom.Generate(world, random, featureX, featureY, featureZ);
        }

	    // Generate Red Mushrooms
        if (random.NextInt(8) == 0)
        {
            featureX = blockX + random.NextInt(16) + 8;
            featureY = random.NextInt(128);
            featureZ = blockZ + random.NextInt(16) + 8;
            _featureRedMushroom.Generate(world, random, featureX, featureY, featureZ);
        }

	    // Generate Sugarcane
        for (int i = 0; i < 10; ++i)
        {
            featureX = blockX + random.NextInt(16) + 8;
            featureY = random.NextInt(128);
            featureZ = blockZ + random.NextInt(16) + 8;
            _featureSugarcane.Generate(world, random, featureX, featureY, featureZ);
        }

	    // Generate Pumpkin Patches
        if (random.NextInt(32) == 0)
        {
            featureX = blockX + random.NextInt(16) + 8;
            featureY = random.NextInt(128);
            featureZ = blockZ + random.NextInt(16) + 8;
            _featurePumpkin.Generate(world, random, featureX, featureY, featureZ);
        }

	    // Generate Cacti
        byte amountOfCacti = 0;
        if (chunkBiome == Biome.Desert)
        {
            amountOfCacti += 10;
        }

        for (int i = 0; i < amountOfCacti; ++i)
        {
            featureX = blockX + random.NextInt(16) + 8;
            featureY = random.NextInt(128);
            featureZ = blockZ + random.NextInt(16) + 8;
            _featureCactus.Generate(world, random, featureX, featureY, featureZ);
        }

        // Generate one-block water sources
        for (int i = 0; i < 50; ++i)
        {
            featureX = blockX + random.NextInt(16) + 8;
            featureY = random.NextInt(random.NextInt(120) + 8);
            featureZ = blockZ + random.NextInt(16) + 8;
            _featureWaterSpring.Generate(world, random, featureX, featureY, featureZ);
        }

        // Generate one-block lava sources
        for (int x = 0; x < 20; ++x)
        {
            featureX = blockX + random.NextInt(16) + 8;
            featureY = random.NextInt(random.NextInt(random.NextInt(112) + 8) + 8);
            featureZ = blockZ + random.NextInt(16) + 8;
            _featureLavaSpring.Generate(world, random, featureX, featureY, featureZ);
        }

        // Place Snow in cold regions
        temperatures = _biomeSource.GetTemperatures(temperatures, blockX + 8, blockZ + 8, 16, 16);

        for (int x = blockX + 8; x < blockX + 8 + 16; ++x)
        {
            for (int z = blockZ + 8; z < blockZ + 8 + 16; ++z)
            {
                int offsetX = x - (blockX + 8);
                int offsetZ = z - (blockZ + 8);
                int var22 = world.getTopSolidBlockY(x, z);
                double temperatureSample = temperatures[offsetX * 16 + offsetZ] - (var22 - 64) / 64.0D * 0.3D;
                if (temperatureSample < 0.5D && var22 > 0 && var22 < 128 && world.isAir(x, var22, z) && world.getMaterial(x, var22 - 1, z).BlocksMovement && world.getMaterial(x, var22 - 1, z) != Material.Ice)
                {
                    world.setBlock(x, var22, z, Block.Snow.id);
                }
            }
        }

        BlockSand.fallInstantly = false;
    }

    public bool Save(bool saveEntities, LoadingDisplay display)
    {
        return true;
    }

    public bool Tick()
    {
        return false;
    }

    public bool CanSave()
    {
        return true;
    }

    public string GetDebugInfo()
    {
        return "RandomLevelSource";
    }

    public void markChunksForUnload(int _)
    {
    }
}
