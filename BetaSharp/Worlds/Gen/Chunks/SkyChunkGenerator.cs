using BetaSharp.Blocks;
using BetaSharp.Blocks.Materials;
using BetaSharp.Util.Maths;
using BetaSharp.Util.Maths.Noise;
using BetaSharp.Worlds.Biomes;
using BetaSharp.Worlds.Chunks;
using BetaSharp.Worlds.Gen.Carvers;
using BetaSharp.Worlds.Gen.Features;

namespace BetaSharp.Worlds.Gen.Chunks;

internal class SkyChunkGenerator : ChunkSource
{
    private readonly JavaRandom _random;
    private readonly OctavePerlinNoiseSampler _minLimitPerlinNoise;
    private readonly OctavePerlinNoiseSampler _maxLimitPerlinNoise;
    private readonly OctavePerlinNoiseSampler _selectorNoise;
    private readonly OctavePerlinNoiseSampler _sandGravelNoise;
    private readonly OctavePerlinNoiseSampler _depthNoise;
    private readonly OctavePerlinNoiseSampler _floatingIslandScale;
    private readonly OctavePerlinNoiseSampler _floatingIslandNoise;
    private readonly OctavePerlinNoiseSampler _forestNoise;
    private readonly World _world;
    private double[] _heightMap;
    private double[] _sandBuffer = new double[256];
    private double[] _gravelBuffer = new double[256];
    private double[] _depthBuffer = new double[256];
    private readonly Carver _carver = new CaveCarver();
    private Biome[] _biomes;
    double[] _selectorNoiseBuffer;
    double[] _minLimitPerlinNoiseBuffer;
    double[] _maxLimitPerlinNoiseBuffer;
    double[] _scaleNoiseBuffer;
    double[] _depthNoiseBuffer;
    private double[] _temperatures;

    public SkyChunkGenerator(World world, long seed)
    {
        _world = world;
        _random = new JavaRandom(seed);
        _minLimitPerlinNoise = new OctavePerlinNoiseSampler(_random, 16);
        _maxLimitPerlinNoise = new OctavePerlinNoiseSampler(_random, 16);
        _selectorNoise = new OctavePerlinNoiseSampler(_random, 8);
        _sandGravelNoise = new OctavePerlinNoiseSampler(_random, 4);
        _depthNoise = new OctavePerlinNoiseSampler(_random, 4);
        _floatingIslandScale = new OctavePerlinNoiseSampler(_random, 10);
        _floatingIslandNoise = new OctavePerlinNoiseSampler(_random, 16);
        _forestNoise = new OctavePerlinNoiseSampler(_random, 8);
    }

    public void BuildTerrain(int chunkX, int chunkZ, byte[] blocks, Biome[] biomes, double[] temperatures)
    {
        byte horiScale = 2;
        int xMax = horiScale + 1;
        byte yMax = 33;
        int zMax = horiScale + 1;
        _heightMap = GenerateHeightMap(_heightMap, chunkX * horiScale, 0, chunkZ * horiScale, xMax, yMax, zMax);

        for (int sampleX = 0; sampleX < horiScale; ++sampleX)
        {
            for (int sampleZ = 0; sampleZ < horiScale; ++sampleZ)
            {
                for (int sampleY = 0; sampleY < 32; ++sampleY)
                {
                    double verticalLerpStep = 0.25D;
                    double corner000 = _heightMap[((sampleX + 0) * zMax + sampleZ + 0) * yMax + sampleY + 0];
                    double corner010 = _heightMap[((sampleX + 0) * zMax + sampleZ + 1) * yMax + sampleY + 0];
                    double corner100 = _heightMap[((sampleX + 1) * zMax + sampleZ + 0) * yMax + sampleY + 0];
                    double corner110 = _heightMap[((sampleX + 1) * zMax + sampleZ + 1) * yMax + sampleY + 0];
                    double corner001 = (_heightMap[((sampleX + 0) * zMax + sampleZ + 0) * yMax + sampleY + 1] - corner000) * verticalLerpStep;
                    double corner011 = (_heightMap[((sampleX + 0) * zMax + sampleZ + 1) * yMax + sampleY + 1] - corner010) * verticalLerpStep;
                    double corner101 = (_heightMap[((sampleX + 1) * zMax + sampleZ + 0) * yMax + sampleY + 1] - corner100) * verticalLerpStep;
                    double corner111 = (_heightMap[((sampleX + 1) * zMax + sampleZ + 1) * yMax + sampleY + 1] - corner110) * verticalLerpStep;

                    for (int subY = 0; subY < 4; ++subY)
                    {
                        double horizontalLerpStep = 0.125D;
                        double terrainX0 = corner000;
                        double terrainX1 = corner010;
                        double terrainStepX0 = (corner100 - corner000) * horizontalLerpStep;
                        double terrainStepX1 = (corner110 - corner010) * horizontalLerpStep;

                        for (int subX = 0; subX < 8; ++subX)
                        {
                            int blockIndex = subX + sampleX * 8 << 11 | 0 + sampleZ * 8 << 7 | sampleY * 4 + subY;
                            short chunkHeight = 128;
                            double horizontalLerpStepZ = 0.125D;
                            double terrainDensity = terrainX0;
                            double densityStepZ = (terrainX1 - terrainX0) * horizontalLerpStepZ;

                            for (int subZ = 0; subZ < 8; ++subZ)
                            {
                                int blockType = 0;
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

    public void BuildSurfaces(int chunkX, int chunkZ, byte[] blocks, Biome[] biomes)
    {
        double chunkBiome = 1.0D / 32.0D;
        _sandBuffer = _sandGravelNoise.create(_sandBuffer, chunkX * 16, chunkZ * 16, 0.0D, 16, 16, 1, chunkBiome, chunkBiome, 1.0D);
        _gravelBuffer = _sandGravelNoise.create(_gravelBuffer, chunkX * 16, 109.0134D, chunkZ * 16, 16, 1, 16, chunkBiome, 1.0D, chunkBiome);
        _depthBuffer = _depthNoise.create(_depthBuffer, chunkX * 16, chunkZ * 16, 0.0D, 16, 16, 1, chunkBiome * 2.0D, chunkBiome * 2.0D, chunkBiome * 2.0D);

        for (int localX = 0; localX < 16; ++localX)
        {
            for (int localZ = 0; localZ < 16; ++localZ)
            {
                Biome localBiome = biomes[localX + localZ * 16];
                int surfaceDepth = (int)(_depthBuffer[localX + localZ * 16] / 3.0D + 3.0D + _random.NextDouble() * 0.25D);
                int currentDepth = -1;
                byte topBlock = localBiome.TopBlockId;
                byte soilBlock = localBiome.SoilBlockId;

                for (int blockY = 127; blockY >= 0; --blockY)
                {
                    int blockIndex = (localZ * 16 + localX) * 128 + blockY;
                    byte currentBlock = blocks[blockIndex];
                    if (currentBlock == 0)
                    {
                        currentDepth = -1;
                    }
                    else if (currentBlock == Block.Stone.id)
                    {
                        if (currentDepth == -1)
                        {
                            if (surfaceDepth <= 0)
                            {
                                topBlock = 0;
                                soilBlock = (byte)Block.Stone.id;
                            }

                            currentDepth = surfaceDepth;
                            if (blockY >= 0)
                            {
                                blocks[blockIndex] = topBlock;
                            }
                            else
                            {
                                blocks[blockIndex] = soilBlock;
                            }
                        }
                        else if (currentDepth > 0)
                        {
                            --currentDepth;
                            blocks[blockIndex] = soilBlock;
                            if (currentDepth == 0 && soilBlock == Block.Sand.id)
                            {
                                currentDepth = _random.NextInt(4);
                                soilBlock = (byte)Block.Sandstone.id;
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

    public Chunk GetChunk(int chunkX, int chunkZ)
    {
        _random.SetSeed(chunkX * 341873128712L + chunkZ * 132897987541L);
        byte[] blocks = new byte[-short.MinValue];
        Chunk chunk = new Chunk(_world, blocks, chunkX, chunkZ);
        _biomes = _world.getBiomeSource().GetBiomesInArea(_biomes, chunkX * 16, chunkZ * 16, 16, 16);
        double[] temperatureMap = _world.getBiomeSource().TemperatureMap;
        BuildTerrain(chunkX, chunkZ, blocks, _biomes, temperatureMap);
        BuildSurfaces(chunkX, chunkZ, blocks, _biomes);
        _carver.carve(this, _world, chunkX, chunkZ, blocks);
        chunk.PopulateHeightMap();
        return chunk;
    }

    private double[] GenerateHeightMap(double[]? heightMap, int x, int y, int z, int sizeX, int sizeY, int sizeZ)
    {
        if (heightMap == null)
        {
            heightMap = new double[sizeX * sizeY * sizeZ];
        }

        double horizontalScale = 684.412D;
        double verticalScale = 684.412D;
        double[] temperatureBuffer = _world.getBiomeSource().TemperatureMap;
        double[] downfallBuffer = _world.getBiomeSource().DownfallMap;
        _scaleNoiseBuffer = _floatingIslandScale.create(_scaleNoiseBuffer, x, z, sizeX, sizeZ, 1.121D, 1.121D, 0.5D);
        _depthNoiseBuffer = _floatingIslandNoise.create(_depthNoiseBuffer, x, z, sizeX, sizeZ, 200.0D, 200.0D, 0.5D);
        horizontalScale *= 2.0D;
        _selectorNoiseBuffer = _selectorNoise.create(_selectorNoiseBuffer, x, y, z, sizeX, sizeY, sizeZ, horizontalScale / 80.0D, verticalScale / 160.0D, horizontalScale / 80.0D);
        _minLimitPerlinNoiseBuffer = _minLimitPerlinNoise.create(_minLimitPerlinNoiseBuffer, x, y, z, sizeX, sizeY, sizeZ, horizontalScale, verticalScale, horizontalScale);
        _maxLimitPerlinNoiseBuffer = _maxLimitPerlinNoise.create(_maxLimitPerlinNoiseBuffer, x, y, z, sizeX, sizeY, sizeZ, horizontalScale, verticalScale, horizontalScale);
        int xyzIndex = 0;
        int xzIndex = 0;
        int scaleFraction = 16 / sizeX;

        for (int iX = 0; iX < sizeX; ++iX)
        {
            int sampleX = iX * scaleFraction + scaleFraction / 2;

            for (int iZ = 0; iZ < sizeZ; ++iZ)
            {
                int sampleZ = iZ * scaleFraction + scaleFraction / 2;
                double temperatureSample = temperatureBuffer[sampleX * 16 + sampleZ];
                double downfallSample = downfallBuffer[sampleX * 16 + sampleZ] * temperatureSample;
                double downfallSampleMod = 1.0D - downfallSample;
                downfallSampleMod *= downfallSampleMod;
                downfallSampleMod *= downfallSampleMod;
                downfallSampleMod = 1.0D - downfallSampleMod;
                double scaleNoiseSample = (_scaleNoiseBuffer[xzIndex] + 256.0D) / 512.0D;
                scaleNoiseSample *= downfallSampleMod;
                if (scaleNoiseSample > 1.0D)
                {
                    scaleNoiseSample = 1.0D;
                }

                double depthNoiseSample = _depthNoiseBuffer[xzIndex] / 8000.0D;
                if (depthNoiseSample < 0.0D)
                {
                    depthNoiseSample = -depthNoiseSample * 0.3D;
                }

                depthNoiseSample = depthNoiseSample * 3.0D - 2.0D;
                if (depthNoiseSample > 1.0D)
                {
                    depthNoiseSample = 1.0D;
                }

                depthNoiseSample /= 8.0D;
                depthNoiseSample = 0.0D;
                if (scaleNoiseSample < 0.0D)
                {
                    scaleNoiseSample = 0.0D;
                }

                scaleNoiseSample += 0.5D;
                depthNoiseSample = depthNoiseSample * sizeY / 16.0D;
                ++xzIndex;
                double elevationOffset = sizeY / 2.0D;

                for (int iY = 0; iY < sizeY; ++iY)
                {
                    double terrainDensity = 0.0D;
                    double densityOffset = (iY - elevationOffset) * 8.0D / scaleNoiseSample;
                    if (densityOffset < 0.0D)
                    {
                        densityOffset *= -1.0D;
                    }

                    double lowNoiseSample = _minLimitPerlinNoiseBuffer[xyzIndex] / 512.0D;
                    double highNoiseSample = _maxLimitPerlinNoiseBuffer[xyzIndex] / 512.0D;
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

                    terrainDensity -= 8.0D;
                    byte yMaxFade = 32;
                    double fadeout;
                    if (iY > sizeY - yMaxFade)
                    {
                        fadeout = (iY - (sizeY - yMaxFade)) / (yMaxFade - 1.0F);
                        terrainDensity = terrainDensity * (1.0D - fadeout) + -30.0D * fadeout;
                    }

                    yMaxFade = 8;
                    if (iY < yMaxFade)
                    {
                        fadeout = (yMaxFade - iY) / (yMaxFade - 1.0F);
                        terrainDensity = terrainDensity * (1.0D - fadeout) + -30.0D * fadeout;
                    }

                    heightMap[xyzIndex] = terrainDensity;
                    ++xyzIndex;
                }
            }
        }

        return heightMap;
    }

    public bool IsChunkLoaded(int chunkX, int chunkZ)
    {
        return true;
    }

    public void DecorateTerrain(ChunkSource source, int chunkX, int chunkZ)
    {
        BlockSand.fallInstantly = true;
        int blockX = chunkX * 16;
        int blockZ = chunkZ * 16;
        Biome chunkBiome = _world.getBiomeSource().GetBiome(blockX + 16, blockZ + 16);
        _random.SetSeed(_world.getSeed());
        long xOffset = _random.NextLong() / 2L * 2L + 1L;
        long zOffset = _random.NextLong() / 2L * 2L + 1L;
        _random.SetSeed(chunkX * xOffset + chunkZ * zOffset ^ _world.getSeed());
        double fraction = 0.25D;
        int featureX;
        int featureY;
        int featureZ;

        if (_random.NextInt(4) == 0)
        {
            featureX = blockX + _random.NextInt(16) + 8;
            featureY = _random.NextInt(128);
            featureZ = blockZ + _random.NextInt(16) + 8;
            new LakeFeature(Block.Water.id).Generate(_world, _random, featureX, featureY, featureZ);
        }

        if (_random.NextInt(8) == 0)
        {
            featureX = blockX + _random.NextInt(16) + 8;
            featureY = _random.NextInt(_random.NextInt(120) + 8);
            featureZ = blockZ + _random.NextInt(16) + 8;
            if (featureY < 64 || _random.NextInt(10) == 0)
            {
                new LakeFeature(Block.Lava.id).Generate(_world, _random, featureX, featureY, featureZ);
            }
        }

        for (int i = 0; i < 8; ++i)
        {
            featureX = blockX + _random.NextInt(16) + 8;
            featureY = _random.NextInt(128);
            featureZ = blockZ + _random.NextInt(16) + 8;
            new DungeonFeature().Generate(_world, _random, featureX, featureY, featureZ);
        }

        for (int i = 0; i < 10; ++i)
        {
            featureX = blockX + _random.NextInt(16);
            featureY = _random.NextInt(128);
            featureZ = blockZ + _random.NextInt(16);
            new ClayOreFeature(32).Generate(_world, _random, featureX, featureY, featureZ);
        }

        for (int i = 0; i < 20; ++i)
        {
            featureX = blockX + _random.NextInt(16);
            featureY = _random.NextInt(128);
            featureZ = blockZ + _random.NextInt(16);
            new OreFeature(Block.Dirt.id, 32).Generate(_world, _random, featureX, featureY, featureZ);
        }

        for (int i = 0; i < 10; ++i)
        {
            featureX = blockX + _random.NextInt(16);
            featureY = _random.NextInt(128);
            featureZ = blockZ + _random.NextInt(16);
            new OreFeature(Block.Gravel.id, 32).Generate(_world, _random, featureX, featureY, featureZ);
        }

        for (int i = 0; i < 20; ++i)
        {
            featureX = blockX + _random.NextInt(16);
            featureY = _random.NextInt(128);
            featureZ = blockZ + _random.NextInt(16);
            new OreFeature(Block.CoalOre.id, 16).Generate(_world, _random, featureX, featureY, featureZ);
        }

        for (int i = 0; i < 20; ++i)
        {
            featureX = blockX + _random.NextInt(16);
            featureY = _random.NextInt(64);
            featureZ = blockZ + _random.NextInt(16);
            new OreFeature(Block.IronOre.id, 8).Generate(_world, _random, featureX, featureY, featureZ);
        }

        for (int i = 0; i < 2; ++i)
        {
            featureX = blockX + _random.NextInt(16);
            featureY = _random.NextInt(32);
            featureZ = blockZ + _random.NextInt(16);
            new OreFeature(Block.GoldOre.id, 8).Generate(_world, _random, featureX, featureY, featureZ);
        }

        for (int i = 0; i < 8; ++i)
        {
            featureX = blockX + _random.NextInt(16);
            featureY = _random.NextInt(16);
            featureZ = blockZ + _random.NextInt(16);
            new OreFeature(Block.RedstoneOre.id, 7).Generate(_world, _random, featureX, featureY, featureZ);
        }

        for (int i = 0; i < 1; ++i)
        {
            featureX = blockX + _random.NextInt(16);
            featureY = _random.NextInt(16);
            featureZ = blockZ + _random.NextInt(16);
            new OreFeature(Block.DiamondOre.id, 7).Generate(_world, _random, featureX, featureY, featureZ);
        }

        for (int i = 0; i < 1; ++i)
        {
            featureX = blockX + _random.NextInt(16);
            featureY = _random.NextInt(16) + _random.NextInt(16);
            featureZ = blockZ + _random.NextInt(16);
            new OreFeature(Block.LapisOre.id, 6).Generate(_world, _random, featureX, featureY, featureZ);
        }

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
            treeFeature.Generate(_world, _random, featureX, _world.getTopY(featureX, featureZ), featureZ);
        }

        for (int i = 0; i < 2; ++i)
        {
            featureX = blockX + _random.NextInt(16) + 8;
            featureY = _random.NextInt(128);
            featureZ = blockZ + _random.NextInt(16) + 8;
            new PlantPatchFeature(Block.Dandelion.id).Generate(_world, _random, featureX, featureY, featureZ);
        }

        if (_random.NextInt(2) == 0)
        {
            featureX = blockX + _random.NextInt(16) + 8;
            featureY = _random.NextInt(128);
            featureZ = blockZ + _random.NextInt(16) + 8;
            new PlantPatchFeature(Block.Rose.id).Generate(_world, _random, featureX, featureY, featureZ);
        }

        if (_random.NextInt(4) == 0)
        {
            featureX = blockX + _random.NextInt(16) + 8;
            featureY = _random.NextInt(128);
            featureZ = blockZ + _random.NextInt(16) + 8;
            new PlantPatchFeature(Block.BrownMushroom.id).Generate(_world, _random, featureX, featureY, featureZ);
        }

        if (_random.NextInt(8) == 0)
        {
            featureX = blockX + _random.NextInt(16) + 8;
            featureY = _random.NextInt(128);
            featureZ = blockZ + _random.NextInt(16) + 8;
            new PlantPatchFeature(Block.RedMushroom.id).Generate(_world, _random, featureX, featureY, featureZ);
        }

        for (int i = 0; i < 10; ++i)
        {
            featureX = blockX + _random.NextInt(16) + 8;
            featureY = _random.NextInt(128);
            featureZ = blockZ + _random.NextInt(16) + 8;
            new SugarCanePatchFeature().Generate(_world, _random, featureX, featureY, featureZ);
        }

        if (_random.NextInt(32) == 0)
        {
            featureX = blockX + _random.NextInt(16) + 8;
            featureY = _random.NextInt(128);
            featureZ = blockZ + _random.NextInt(16) + 8;
            new PumpkinPatchFeature().Generate(_world, _random, featureX, featureY, featureZ);
        }

        int amountOfCacti = 0;
        if (chunkBiome == Biome.Desert)
        {
            amountOfCacti += 10;
        }

        for (int i = 0; i < amountOfCacti; ++i)
        {
            featureX = blockX + _random.NextInt(16) + 8;
            featureY = _random.NextInt(128);
            featureZ = blockZ + _random.NextInt(16) + 8;
            new CactusPatchFeature().Generate(_world, _random, featureX, featureY, featureZ);
        }

        for (int i = 0; i < 50; ++i)
        {
            featureX = blockX + _random.NextInt(16) + 8;
            featureY = _random.NextInt(_random.NextInt(120) + 8);
            featureZ = blockZ + _random.NextInt(16) + 8;
            new SpringFeature(Block.FlowingWater.id).Generate(_world, _random, featureX, featureY, featureZ);
        }

        for (int i = 0; i < 20; ++i)
        {
            featureX = blockX + _random.NextInt(16) + 8;
            featureY = _random.NextInt(_random.NextInt(_random.NextInt(112) + 8) + 8);
            featureZ = blockZ + _random.NextInt(16) + 8;
            new SpringFeature(Block.FlowingLava.id).Generate(_world, _random, featureX, featureY, featureZ);
        }

        _temperatures = _world.getBiomeSource().GetTemperatures(_temperatures, blockX + 8, blockZ + 8, 16, 16);

        for (int x = blockX + 8; x < blockX + 8 + 16; ++x)
        {
            for (int z = blockZ + 8; z < blockZ + 8 + 16; ++z)
            {
                int offsetX = x - (blockX + 8);
                int offsetZ = z - (blockZ + 8);
                int topBlockY = _world.getTopSolidBlockY(x, z);
                double temperatureSample = _temperatures[offsetX * 16 + offsetZ] - (topBlockY - 64) / 64.0D * 0.3D;

                if (temperatureSample < 0.5D && topBlockY > 0 && topBlockY < 128 && _world.isAir(x, topBlockY, z) && _world.getMaterial(x, topBlockY - 1, z).BlocksMovement && _world.getMaterial(x, topBlockY - 1, z) != Material.Ice)
                {
                    _world.setBlock(x, topBlockY, z, Block.Snow.id);
                }
            }
        }

        BlockSand.fallInstantly = false;
    }

    public bool Save(bool var1, LoadingDisplay display) => true;

    public bool Tick() => false;

    public bool CanSave() => true;

    public string GetDebugInfo() => "RandomLevelSource";

    public void markChunksForUnload(int _)
    {
    }
}
