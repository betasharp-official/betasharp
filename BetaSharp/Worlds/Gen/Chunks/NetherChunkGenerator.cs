using BetaSharp.Blocks;
using BetaSharp.Util.Maths;
using BetaSharp.Util.Maths.Noise;
using BetaSharp.Worlds.Chunks;
using BetaSharp.Worlds.Gen.Carvers;
using BetaSharp.Worlds.Gen.Features;

namespace BetaSharp.Worlds.Gen.Chunks;

internal class NetherChunkGenerator : ChunkSource
{

    private readonly JavaRandom random;
    private readonly OctavePerlinNoiseSampler minLimitPerlinNoise;
    private readonly OctavePerlinNoiseSampler maxLimitPerlinNoise;
    private readonly OctavePerlinNoiseSampler perlinNoise1;
    private readonly OctavePerlinNoiseSampler perlinNoise2;
    private readonly OctavePerlinNoiseSampler perlinNoise3;
    public OctavePerlinNoiseSampler scaleNoise;
    public OctavePerlinNoiseSampler depthNoise;
    private readonly World world;
    private double[] heightMap;
    private double[] sandBuffer = new double[256];
    private double[] gravelBuffer = new double[256];
    private double[] depthBuffer = new double[256];
    private readonly Carver cave = new NetherCaveCarver();
    private readonly long _seed;
    private NetherLavaSpringFeature _featureNetherLavaSpring;
    private NetherFirePatchFeature _featureNetherFire;
    private GlowstoneClusterFeature _featureGlowstoneFull;
    private GlowstoneClusterFeatureRare _featureGlowstoneRare;
    private PlantPatchFeature _featureBrownMushroom;
    private PlantPatchFeature _featureRedMushroom;
    double[] perlinNoiseBuffer;
    double[] minLimitPerlinNoiseBuffer;
    double[] maxLimitPerlinNoiseBuffer;
    double[] scaleNoiseBuffer;
    double[] depthNoiseBuffer;

    public NetherChunkGenerator(World world, long seed)
    {
        this.world = world;
        random = new(seed);
        minLimitPerlinNoise = new OctavePerlinNoiseSampler(random, 16);
        maxLimitPerlinNoise = new OctavePerlinNoiseSampler(random, 16);
        perlinNoise1 = new OctavePerlinNoiseSampler(random, 8);
        perlinNoise2 = new OctavePerlinNoiseSampler(random, 4);
        perlinNoise3 = new OctavePerlinNoiseSampler(random, 4);
        scaleNoise = new OctavePerlinNoiseSampler(random, 10);
        depthNoise = new OctavePerlinNoiseSampler(random, 16);
        _seed = seed;
        InitFeatures();
    }

    public ChunkSource CreateParallelInstance() => new NetherChunkGenerator(world, _seed);

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
        byte var4 = 4;
        byte var5 = 32;
        int var6 = var4 + 1;
        byte var7 = 17;
        int var8 = var4 + 1;
        heightMap = GenerateHeightMap(heightMap, chunkX * var4, 0, chunkZ * var4, var6, var7, var8);

        for (int var9 = 0; var9 < var4; ++var9)
        {
            for (int var10 = 0; var10 < var4; ++var10)
            {
                for (int var11 = 0; var11 < 16; ++var11)
                {
                    double var12 = 0.125D;
                    double var14 = heightMap[((var9 + 0) * var8 + var10 + 0) * var7 + var11 + 0];
                    double var16 = heightMap[((var9 + 0) * var8 + var10 + 1) * var7 + var11 + 0];
                    double var18 = heightMap[((var9 + 1) * var8 + var10 + 0) * var7 + var11 + 0];
                    double var20 = heightMap[((var9 + 1) * var8 + var10 + 1) * var7 + var11 + 0];
                    double var22 = (heightMap[((var9 + 0) * var8 + var10 + 0) * var7 + var11 + 1] - var14) * var12;
                    double var24 = (heightMap[((var9 + 0) * var8 + var10 + 1) * var7 + var11 + 1] - var16) * var12;
                    double var26 = (heightMap[((var9 + 1) * var8 + var10 + 0) * var7 + var11 + 1] - var18) * var12;
                    double var28 = (heightMap[((var9 + 1) * var8 + var10 + 1) * var7 + var11 + 1] - var20) * var12;

                    for (int var30 = 0; var30 < 8; ++var30)
                    {
                        double var31 = 0.25D;
                        double var33 = var14;
                        double var35 = var16;
                        double var37 = (var18 - var14) * var31;
                        double var39 = (var20 - var16) * var31;

                        for (int var41 = 0; var41 < 4; ++var41)
                        {
                            int var42 = var41 + var9 * 4 << 11 | 0 + var10 * 4 << 7 | var11 * 8 + var30;
                            short var43 = 128;
                            double var44 = 0.25D;
                            double var46 = var33;
                            double var48 = (var35 - var33) * var44;

                            for (int var50 = 0; var50 < 4; ++var50)
                            {
                                int var51 = 0;
                                if (var11 * 8 + var30 < var5)
                                {
                                    var51 = Block.Lava.id;
                                }

                                if (var46 > 0.0D)
                                {
                                    var51 = Block.Netherrack.id;
                                }

                                blocks[var42] = (byte)var51;
                                var42 += var43;
                                var46 += var48;
                            }

                            var33 += var37;
                            var35 += var39;
                        }

                        var14 += var22;
                        var16 += var24;
                        var18 += var26;
                        var20 += var28;
                    }
                }
            }
        }

    }

    public void BuildSurfaces(int chunkX, int chunkZ, byte[] blocks)
    {
        byte var4 = 64;
        double var5 = 1.0D / 32.0D;
        sandBuffer = perlinNoise2.create(sandBuffer, chunkX * 16, chunkZ * 16, 0.0D, 16, 16, 1, var5, var5, 1.0D);
        gravelBuffer = perlinNoise2.create(gravelBuffer, chunkX * 16, 109.0134D, chunkZ * 16, 16, 1, 16, var5, 1.0D, var5);
        depthBuffer = perlinNoise3.create(depthBuffer, chunkX * 16, chunkZ * 16, 0.0D, 16, 16, 1, var5 * 2.0D, var5 * 2.0D, var5 * 2.0D);

        for (int var7 = 0; var7 < 16; ++var7)
        {
            for (int var8 = 0; var8 < 16; ++var8)
            {
                bool var9 = sandBuffer[var7 + var8 * 16] + random.NextDouble() * 0.2D > 0.0D;
                bool var10 = gravelBuffer[var7 + var8 * 16] + random.NextDouble() * 0.2D > 0.0D;
                int var11 = (int)(depthBuffer[var7 + var8 * 16] / 3.0D + 3.0D + random.NextDouble() * 0.25D);
                int var12 = -1;
                byte var13 = (byte)Block.Netherrack.id;
                byte var14 = (byte)Block.Netherrack.id;

                for (int var15 = 127; var15 >= 0; --var15)
                {
                    int var16 = (var8 * 16 + var7) * 128 + var15;
                    if (var15 >= 127 - random.NextInt(5))
                    {
                        blocks[var16] = (byte)Block.Bedrock.id;
                    }
                    else if (var15 <= 0 + random.NextInt(5))
                    {
                        blocks[var16] = (byte)Block.Bedrock.id;
                    }
                    else
                    {
                        byte var17 = blocks[var16];
                        if (var17 == 0)
                        {
                            var12 = -1;
                        }
                        else if (var17 == Block.Netherrack.id)
                        {
                            if (var12 == -1)
                            {
                                if (var11 <= 0)
                                {
                                    var13 = 0;
                                    var14 = (byte)Block.Netherrack.id;
                                }
                                else if (var15 >= var4 - 4 && var15 <= var4 + 1)
                                {
                                    var13 = (byte)Block.Netherrack.id;
                                    var14 = (byte)Block.Netherrack.id;
                                    if (var10)
                                    {
                                        var13 = (byte)Block.Gravel.id;
                                    }

                                    if (var10)
                                    {
                                        var14 = (byte)Block.Netherrack.id;
                                    }

                                    if (var9)
                                    {
                                        var13 = (byte)Block.Soulsand.id;
                                    }

                                    if (var9)
                                    {
                                        var14 = (byte)Block.Soulsand.id;
                                    }
                                }

                                if (var15 < var4 && var13 == 0)
                                {
                                    var13 = (byte)Block.Lava.id;
                                }

                                var12 = var11;
                                if (var15 >= var4 - 1)
                                {
                                    blocks[var16] = var13;
                                }
                                else
                                {
                                    blocks[var16] = var14;
                                }
                            }
                            else if (var12 > 0)
                            {
                                --var12;
                                blocks[var16] = var14;
                            }
                        }
                    }
                }
            }
        }

    }

    public Chunk LoadChunk(int x, int z)
    {
        return GetChunk(x, z);
    }

    public Chunk GetChunk(int chunkX, int chunkZ)
    {
        random.SetSeed(chunkX * 341873128712L + chunkZ * 132897987541L);
        byte[] var3 = new byte[-short.MinValue];
        BuildTerrain(chunkX, chunkZ, var3);
        BuildSurfaces(chunkX, chunkZ, var3);
        cave.carve(this, world, chunkX, chunkZ, var3);
        Chunk var4 = new Chunk(world, var3, chunkX, chunkZ);
        return var4;
    }

    private double[] GenerateHeightMap(double[] heightMap, int x, int y, int z, int sizeX, int sizeY, int sizeZ)
    {
        if (heightMap == null)
        {
            heightMap = new double[sizeX * sizeY * sizeZ];
        }

        double var8 = 684.412D;
        double var10 = 2053.236D;
        scaleNoiseBuffer = scaleNoise.create(scaleNoiseBuffer, x, y, z, sizeX, 1, sizeZ, 1.0D, 0.0D, 1.0D);
        depthNoiseBuffer = depthNoise.create(depthNoiseBuffer, x, y, z, sizeX, 1, sizeZ, 100.0D, 0.0D, 100.0D);
        perlinNoiseBuffer = perlinNoise1.create(perlinNoiseBuffer, x, y, z, sizeX, sizeY, sizeZ, var8 / 80.0D, var10 / 60.0D, var8 / 80.0D);
        minLimitPerlinNoiseBuffer = minLimitPerlinNoise.create(minLimitPerlinNoiseBuffer, x, y, z, sizeX, sizeY, sizeZ, var8, var10, var8);
        maxLimitPerlinNoiseBuffer = maxLimitPerlinNoise.create(maxLimitPerlinNoiseBuffer, x, y, z, sizeX, sizeY, sizeZ, var8, var10, var8);
        int var12 = 0;
        int var13 = 0;
        double[] var14 = new double[sizeY];

        int var15;
        for (var15 = 0; var15 < sizeY; ++var15)
        {
            var14[var15] = Math.Cos(var15 * Math.PI * 6.0D / sizeY) * 2.0D;
            double var16 = var15;
            if (var15 > sizeY / 2)
            {
                var16 = sizeY - 1 - var15;
            }

            if (var16 < 4.0D)
            {
                var16 = 4.0D - var16;
                var14[var15] -= var16 * var16 * var16 * 10.0D;
            }
        }

        for (var15 = 0; var15 < sizeX; ++var15)
        {
            for (int var36 = 0; var36 < sizeZ; ++var36)
            {
                double var17 = (scaleNoiseBuffer[var13] + 256.0D) / 512.0D;
                if (var17 > 1.0D)
                {
                    var17 = 1.0D;
                }

                double var19 = 0.0D;
                double var21 = depthNoiseBuffer[var13] / 8000.0D;
                if (var21 < 0.0D)
                {
                    var21 = -var21;
                }

                var21 = var21 * 3.0D - 3.0D;
                if (var21 < 0.0D)
                {
                    var21 /= 2.0D;
                    if (var21 < -1.0D)
                    {
                        var21 = -1.0D;
                    }

                    var21 /= 1.4D;
                    var21 /= 2.0D;
                    var17 = 0.0D;
                }
                else
                {
                    if (var21 > 1.0D)
                    {
                        var21 = 1.0D;
                    }

                    var21 /= 6.0D;
                }

                var17 += 0.5D;
                var21 = var21 * sizeY / 16.0D;
                ++var13;

                for (int var23 = 0; var23 < sizeY; ++var23)
                {
                    double var24 = 0.0D;
                    double var26 = var14[var23];
                    double var28 = minLimitPerlinNoiseBuffer[var12] / 512.0D;
                    double var30 = maxLimitPerlinNoiseBuffer[var12] / 512.0D;
                    double var32 = (perlinNoiseBuffer[var12] / 10.0D + 1.0D) / 2.0D;
                    if (var32 < 0.0D)
                    {
                        var24 = var28;
                    }
                    else if (var32 > 1.0D)
                    {
                        var24 = var30;
                    }
                    else
                    {
                        var24 = var28 + (var30 - var28) * var32;
                    }

                    var24 -= var26;
                    double var34;
                    if (var23 > sizeY - 4)
                    {
                        var34 = (double)((var23 - (sizeY - 4)) / 3.0F);
                        var24 = var24 * (1.0D - var34) + -10.0D * var34;
                    }

                    if (var23 < var19)
                    {
                        var34 = (var19 - var23) / 4.0D;
                        if (var34 < 0.0D)
                        {
                            var34 = 0.0D;
                        }

                        if (var34 > 1.0D)
                        {
                            var34 = 1.0D;
                        }

                        var24 = var24 * (1.0D - var34) + -10.0D * var34;
                    }

                    heightMap[var12] = var24;
                    ++var12;
                }
            }
        }

        return heightMap;
    }

    public bool IsChunkLoaded(int x, int z)
    {
        return true;
    }

    public void DecorateTerrain(ChunkSource source, int x, int z)
    {
        BlockSand.fallInstantly = true;
        int var4 = x * 16;
        int var5 = z * 16;

        int var6;
        int var7;
        int var8;
        int var9;
        for (var6 = 0; var6 < 8; ++var6)
        {
            var7 = var4 + random.NextInt(16) + 8;
            var8 = random.NextInt(120) + 4;
            var9 = var5 + random.NextInt(16) + 8;
            _featureNetherLavaSpring.Generate(world, random, var7, var8, var9);
        }

        var6 = random.NextInt(random.NextInt(10) + 1) + 1;

        int var10;
        for (var7 = 0; var7 < var6; ++var7)
        {
            var8 = var4 + random.NextInt(16) + 8;
            var9 = random.NextInt(120) + 4;
            var10 = var5 + random.NextInt(16) + 8;
            _featureNetherFire.Generate(world, random, var8, var9, var10);
        }

        var6 = random.NextInt(random.NextInt(10) + 1);

        for (var7 = 0; var7 < var6; ++var7)
        {
            var8 = var4 + random.NextInt(16) + 8;
            var9 = random.NextInt(120) + 4;
            var10 = var5 + random.NextInt(16) + 8;
            _featureGlowstoneFull.Generate(world, random, var8, var9, var10);
        }

        for (var7 = 0; var7 < 10; ++var7)
        {
            var8 = var4 + random.NextInt(16) + 8;
            var9 = random.NextInt(128);
            var10 = var5 + random.NextInt(16) + 8;
            _featureGlowstoneRare.Generate(world, random, var8, var9, var10);
        }

        if (random.NextInt(1) == 0)
        {
            var7 = var4 + random.NextInt(16) + 8;
            var8 = random.NextInt(128);
            var9 = var5 + random.NextInt(16) + 8;
            _featureBrownMushroom.Generate(world, random, var7, var8, var9);
        }

        if (random.NextInt(1) == 0)
        {
            var7 = var4 + random.NextInt(16) + 8;
            var8 = random.NextInt(128);
            var9 = var5 + random.NextInt(16) + 8;
            _featureRedMushroom.Generate(world, random, var7, var8, var9);
        }

        BlockSand.fallInstantly = false;
    }

    public bool Save(bool bl, LoadingDisplay display)
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
        return "HellRandomLevelSource";
    }

    public void markChunksForUnload(int _)
    {
    }
}
