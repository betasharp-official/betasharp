using BetaSharp.Blocks;
using BetaSharp.Util.Maths;
using BetaSharp.Util.Maths.Noise;
using BetaSharp.Worlds.Chunks;
using BetaSharp.Worlds.Core.Systems;
using BetaSharp.Worlds.Generation.Generators.Carvers;
using BetaSharp.Worlds.Generation.Generators.Features;

namespace BetaSharp.Worlds.Gen.Chunks;

internal class NetherChunkGenerator : CommonChunkGenerator, IChunkSource
{
    private readonly Carver _cave = new NetherCaveCarver();
    private readonly OctavePerlinNoiseSampler _depthNoise;
    private readonly OctavePerlinNoiseSampler _maxLimitPerlinNoise;
    private readonly OctavePerlinNoiseSampler _minLimitPerlinNoise;
    private readonly OctavePerlinNoiseSampler _perlinNoise1;
    private readonly OctavePerlinNoiseSampler _perlinNoise2;
    private readonly OctavePerlinNoiseSampler _perlinNoise3;
    private readonly OctavePerlinNoiseSampler _scaleNoise;
    private double[] _depthBuffer = new double[256];
    private double[] _depthNoiseBuffer;
    private PlantPatchFeature _featureBrownMushroom;
    private GlowstoneClusterFeature _featureGlowstoneFull;
    private GlowstoneClusterFeatureRare _featureGlowstoneRare;
    private NetherFirePatchFeature _featureNetherFire;
    private NetherLavaSpringFeature _featureNetherLavaSpring;
    private PlantPatchFeature _featureRedMushroom;
    private double[] _gravelBuffer = new double[256];
    private double[] _heightMap;
    private double[] _maxLimitPerlinNoiseBuffer;
    private double[] _minLimitPerlinNoiseBuffer;
    private double[] _perlinNoiseBuffer;
    private double[] _sandBuffer = new double[256];
    private double[] _scaleNoiseBuffer;

    public NetherChunkGenerator(IWorldContext world, long seed) : base(world, seed)
    {
        _minLimitPerlinNoise = new OctavePerlinNoiseSampler(_random, 16);
        _maxLimitPerlinNoise = new OctavePerlinNoiseSampler(_random, 16);
        _perlinNoise1 = new OctavePerlinNoiseSampler(_random, 8);
        _perlinNoise2 = new OctavePerlinNoiseSampler(_random, 4);
        _perlinNoise3 = new OctavePerlinNoiseSampler(_random, 4);
        _scaleNoise = new OctavePerlinNoiseSampler(_random, 10);
        _depthNoise = new OctavePerlinNoiseSampler(_random, 16);
        InitFeatures();
    }

    public IChunkSource CreateParallelInstance() => new NetherChunkGenerator(_world, _seed);

    public Chunk LoadChunk(int x, int z) => GetChunk(x, z);

    public Chunk GetChunk(int chunkX, int chunkZ)
    {
        _random.SetSeed(chunkX * 341873128712L + chunkZ * 132897987541L);
        byte[] blocks = new byte[ChuckFormat.ChunkSize];
        BuildTerrain(chunkX, chunkZ, blocks);
        BuildSurfaces(chunkX, chunkZ, blocks);
        _cave.carve(this, _world, chunkX, chunkZ, blocks);
        Chunk chunk = new(_world, blocks, chunkX, chunkZ);
        return chunk;
    }

    public bool IsChunkLoaded(int x, int z) => true;

    public void DecorateTerrain(IChunkSource source, int x, int z)
    {
        BlockSand.FallInstantly = true;
        int blockX = x * 16;
        int blockZ = z * 16;

        int numIterations;
        int featureX;
        int featureY;
        int featureZ;
        for (numIterations = 0; numIterations < 8; ++numIterations)
        {
            featureX = blockX + _random.NextInt(16) + 8;
            featureY = _random.NextInt(120) + 4;
            featureZ = blockZ + _random.NextInt(16) + 8;
            _featureNetherLavaSpring.Generate(_world, _random, featureX, featureY, featureZ);
        }

        numIterations = _random.NextInt(_random.NextInt(10) + 1) + 1;

        int featureZFallback;
        for (featureX = 0; featureX < numIterations; ++featureX)
        {
            featureY = blockX + _random.NextInt(16) + 8;
            featureZ = _random.NextInt(120) + 4;
            featureZFallback = blockZ + _random.NextInt(16) + 8;
            _featureNetherFire.Generate(_world, _random, featureY, featureZ, featureZFallback);
        }

        numIterations = _random.NextInt(_random.NextInt(10) + 1);

        for (featureX = 0; featureX < numIterations; ++featureX)
        {
            featureY = blockX + _random.NextInt(16) + 8;
            featureZ = _random.NextInt(120) + 4;
            featureZFallback = blockZ + _random.NextInt(16) + 8;
            _featureGlowstoneFull.Generate(_world, _random, featureY, featureZ, featureZFallback);
        }

        for (featureX = 0; featureX < 10; ++featureX)
        {
            featureY = blockX + _random.NextInt(16) + 8;
            featureZ = _random.NextInt(128);
            featureZFallback = blockZ + _random.NextInt(16) + 8;
            _featureGlowstoneRare.Generate(_world, _random, featureY, featureZ, featureZFallback);
        }

        if (_random.NextInt(1) == 0)
        {
            featureX = blockX + _random.NextInt(16) + 8;
            featureY = _random.NextInt(128);
            featureZ = blockZ + _random.NextInt(16) + 8;
            _featureBrownMushroom.Generate(_world, _random, featureX, featureY, featureZ);
        }

        if (_random.NextInt(1) == 0)
        {
            featureX = blockX + _random.NextInt(16) + 8;
            featureY = _random.NextInt(128);
            featureZ = blockZ + _random.NextInt(16) + 8;
            _featureRedMushroom.Generate(_world, _random, featureX, featureY, featureZ);
        }

        BlockSand.FallInstantly = false;
    }

    public bool Save(bool bl, LoadingDisplay display) => true;
    public bool Tick() => false;
    public bool CanSave() => true;
    public string GetDebugInfo() => "HellRandomLevelSource";

    private void InitFeatures()
    {
        _featureNetherLavaSpring = new NetherLavaSpringFeature(Block.FlowingLava.id);
        _featureNetherFire = new NetherFirePatchFeature();
        _featureGlowstoneFull = new GlowstoneClusterFeature();
        _featureGlowstoneRare = new GlowstoneClusterFeatureRare();
        _featureBrownMushroom = new PlantPatchFeature(Block.BrownMushroom.id);
        _featureRedMushroom = new PlantPatchFeature(Block.RedMushroom.id);
    }

    public void BuildTerrain(int chunkX, int chunkZ, byte[] blocks)
    {
        byte horiScale = 4;
        byte lavaLevel = 32;
        int xMax = horiScale + 1;
        byte yMax = 17;
        int zMax = horiScale + 1;

        _heightMap = GenerateHeightMap(_heightMap, chunkX * horiScale, 0, chunkZ * horiScale, xMax, yMax, zMax);

        for (int sampleX = 0; sampleX < horiScale; ++sampleX)
        {
            for (int sampleZ = 0; sampleZ < horiScale; ++sampleZ)
            {
                for (int sampleY = 0; sampleY < 16; ++sampleY)
                {
                    double verticalLerpStep = 0.125D;
                    double corner000 = _heightMap[((sampleX + 0) * zMax + sampleZ + 0) * yMax + sampleY + 0];
                    double corner010 = _heightMap[((sampleX + 0) * zMax + sampleZ + 1) * yMax + sampleY + 0];
                    double corner100 = _heightMap[((sampleX + 1) * zMax + sampleZ + 0) * yMax + sampleY + 0];
                    double corner110 = _heightMap[((sampleX + 1) * zMax + sampleZ + 1) * yMax + sampleY + 0];
                    double corner001 = (_heightMap[((sampleX + 0) * zMax + sampleZ + 0) * yMax + sampleY + 1] - corner000) * verticalLerpStep;
                    double corner011 = (_heightMap[((sampleX + 0) * zMax + sampleZ + 1) * yMax + sampleY + 1] - corner010) * verticalLerpStep;
                    double corner101 = (_heightMap[((sampleX + 1) * zMax + sampleZ + 0) * yMax + sampleY + 1] - corner100) * verticalLerpStep;
                    double corner111 = (_heightMap[((sampleX + 1) * zMax + sampleZ + 1) * yMax + sampleY + 1] - corner110) * verticalLerpStep;

                    for (int subY = 0; subY < 8; ++subY)
                    {
                        double horizontalLerpStep = 0.25D;
                        double terrainX0 = corner000;
                        double terrainX1 = corner010;
                        double terrainStepX0 = (corner100 - corner000) * horizontalLerpStep;
                        double terrainStepX1 = (corner110 - corner010) * horizontalLerpStep;

                        for (int subX = 0; subX < 4; ++subX)
                        {
                            int blockIndex = ChuckFormat.GetIndex(subX + sampleX * 4, sampleY * 8 + subY, sampleZ * 4);
                            double horizontalLerpStepZ = 0.25D;
                            double terrainDensity = terrainX0;
                            double densityStepZ = (terrainX1 - terrainX0) * horizontalLerpStepZ;

                            for (int subZ = 0; subZ < 4; ++subZ)
                            {
                                int blockType = 0;
                                if (sampleY * 8 + subY < lavaLevel)
                                {
                                    blockType = Block.Lava.id;
                                }

                                if (terrainDensity > 0.0D)
                                {
                                    blockType = Block.Netherrack.id;
                                }

                                blocks[blockIndex] = (byte)blockType;
                                blockIndex += ChuckFormat.ChunkHeight;
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

    public void BuildSurfaces(int chunkX, int chunkZ, byte[] blocks)
    {
        byte seaLevel = 64;
        double noiseScale = 1.0D / 32.0D;
        _sandBuffer = _perlinNoise2.create(_sandBuffer, chunkX * 16, chunkZ * 16, 0.0D, 16, 16, 1, noiseScale, noiseScale, 1.0D);
        _gravelBuffer = _perlinNoise2.create(_gravelBuffer, chunkX * 16, 109.0134D, chunkZ * 16, 16, 1, 16, noiseScale, 1.0D, noiseScale);
        _depthBuffer = _perlinNoise3.create(_depthBuffer, chunkX * 16, chunkZ * 16, 0.0D, 16, 16, 1, noiseScale * 2.0D, noiseScale * 2.0D, noiseScale * 2.0D);

        for (int localX = 0; localX < 16; ++localX)
        {
            for (int localZ = 0; localZ < 16; ++localZ)
            {
                bool isSoulsand = _sandBuffer[localX + localZ * 16] + _random.NextDouble() * 0.2D > 0.0D;
                bool isGravel = _gravelBuffer[localX + localZ * 16] + _random.NextDouble() * 0.2D > 0.0D;
                int surfaceDepth = (int)(_depthBuffer[localX + localZ * 16] / 3.0D + 3.0D + _random.NextDouble() * 0.25D);
                int currentDepth = -1;
                byte topBlock = (byte)Block.Netherrack.id;
                byte soilBlock = (byte)Block.Netherrack.id;

                for (int blockY = 127; blockY >= 0; --blockY)
                {
                    int blockIndex = (localZ * 16 + localX) * 128 + blockY;
                    if (blockY >= 127 - _random.NextInt(5))
                    {
                        blocks[blockIndex] = (byte)Block.Bedrock.id;
                    }
                    else if (blockY <= 0 + _random.NextInt(5))
                    {
                        blocks[blockIndex] = (byte)Block.Bedrock.id;
                    }
                    else
                    {
                        byte currentBlock = blocks[blockIndex];
                        if (currentBlock == 0)
                        {
                            currentDepth = -1;
                        }
                        else if (currentBlock == Block.Netherrack.id)
                        {
                            if (currentDepth == -1)
                            {
                                if (surfaceDepth <= 0)
                                {
                                    topBlock = 0;
                                    soilBlock = (byte)Block.Netherrack.id;
                                }
                                else if (blockY >= seaLevel - 4 && blockY <= seaLevel + 1)
                                {
                                    topBlock = (byte)Block.Netherrack.id;
                                    soilBlock = (byte)Block.Netherrack.id;
                                    if (isGravel)
                                    {
                                        topBlock = (byte)Block.Gravel.id;
                                    }

                                    if (isGravel)
                                    {
                                        soilBlock = (byte)Block.Netherrack.id;
                                    }

                                    if (isSoulsand)
                                    {
                                        topBlock = (byte)Block.Soulsand.id;
                                    }

                                    if (isSoulsand)
                                    {
                                        soilBlock = (byte)Block.Soulsand.id;
                                    }
                                }

                                if (blockY < seaLevel && topBlock == 0)
                                {
                                    topBlock = (byte)Block.Lava.id;
                                }

                                currentDepth = surfaceDepth;
                                if (blockY >= seaLevel - 1)
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
                            }
                        }
                    }
                }
            }
        }
    }

    private double[] GenerateHeightMap(double[]? heightMap, int x, int y, int z, int sizeX, int sizeY, int sizeZ)
    {
        if (heightMap == null)
        {
            heightMap = new double[sizeX * sizeY * sizeZ];
        }

        double horizontalScale = 684.412D;
        double verticalScale = 2053.236D;
        _scaleNoiseBuffer = _scaleNoise.create(_scaleNoiseBuffer, x, y, z, sizeX, 1, sizeZ, 1.0D, 0.0D, 1.0D);
        _depthNoiseBuffer = _depthNoise.create(_depthNoiseBuffer, x, y, z, sizeX, 1, sizeZ, 100.0D, 0.0D, 100.0D);
        _perlinNoiseBuffer = _perlinNoise1.create(_perlinNoiseBuffer, x, y, z, sizeX, sizeY, sizeZ, horizontalScale / 80.0D, verticalScale / 60.0D, horizontalScale / 80.0D);
        _minLimitPerlinNoiseBuffer = _minLimitPerlinNoise.create(_minLimitPerlinNoiseBuffer, x, y, z, sizeX, sizeY, sizeZ, horizontalScale, verticalScale, horizontalScale);
        _maxLimitPerlinNoiseBuffer = _maxLimitPerlinNoise.create(_maxLimitPerlinNoiseBuffer, x, y, z, sizeX, sizeY, sizeZ, horizontalScale, verticalScale, horizontalScale);
        int xyzIndex = 0;
        int xzIndex = 0;
        double[] heightModifiers = new double[sizeY];

        int iY;
        for (iY = 0; iY < sizeY; ++iY)
        {
            heightModifiers[iY] = Math.Cos(iY * Math.PI * 6.0D / sizeY) * 2.0D;
            double modifier = iY;
            if (iY > sizeY / 2)
            {
                modifier = sizeY - 1 - iY;
            }

            if (modifier < 4.0D)
            {
                modifier = 4.0D - modifier;
                heightModifiers[iY] -= modifier * modifier * modifier * 10.0D;
            }
        }

        for (int iX = 0; iX < sizeX; ++iX)
        {
            for (int iZ = 0; iZ < sizeZ; ++iZ)
            {
                double scaleNoiseSample = (_scaleNoiseBuffer[xzIndex] + 256.0D) / 512.0D;
                if (scaleNoiseSample > 1.0D)
                {
                    scaleNoiseSample = 1.0D;
                }

                double densityOffset = 0.0D;
                double depthNoiseSample = _depthNoiseBuffer[xzIndex] / 8000.0D;
                if (depthNoiseSample < 0.0D)
                {
                    depthNoiseSample = -depthNoiseSample;
                }

                depthNoiseSample = depthNoiseSample * 3.0D - 3.0D;
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

                    depthNoiseSample /= 6.0D;
                }

                scaleNoiseSample += 0.5D;
                depthNoiseSample = depthNoiseSample * sizeY / 16.0D;
                ++xzIndex;

                for (iY = 0; iY < sizeY; ++iY)
                {
                    double terrainDensity = 0.0D;
                    double shapeModifier = heightModifiers[iY];
                    double lowNoiseSample = _minLimitPerlinNoiseBuffer[xyzIndex] / 512.0D;
                    double highNoiseSample = _maxLimitPerlinNoiseBuffer[xyzIndex] / 512.0D;
                    double selectorNoiseSample = (_perlinNoiseBuffer[xyzIndex] / 10.0D + 1.0D) / 2.0D;
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

                    terrainDensity -= shapeModifier;
                    double fadeout;
                    if (iY > sizeY - 4)
                    {
                        fadeout = (iY - (sizeY - 4)) / 3.0F;
                        terrainDensity = terrainDensity * (1.0D - fadeout) + -10.0D * fadeout;
                    }

                    if (iY < densityOffset)
                    {
                        fadeout = (densityOffset - iY) / 4.0D;
                        if (fadeout < 0.0D)
                        {
                            fadeout = 0.0D;
                        }

                        if (fadeout > 1.0D)
                        {
                            fadeout = 1.0D;
                        }

                        terrainDensity = terrainDensity * (1.0D - fadeout) + -10.0D * fadeout;
                    }

                    heightMap[xyzIndex] = terrainDensity;
                    ++xyzIndex;
                }
            }
        }

        return heightMap;
    }

    public static void markChunksForUnload(int _)
    {
    }
}
