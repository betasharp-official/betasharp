using BetaSharp.Blocks;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Chunks;
using BetaSharp.Worlds.Core;
using BetaSharp.Worlds.Core.Systems;
using BetaSharp.Worlds.Generation.Biomes;
using BetaSharp.Worlds.Generation.Generators.Features;

namespace BetaSharp.Worlds.Gen.Flat;

internal class FlatChunkGenerator : IChunkSource
{
    private readonly IWorldContext _world;
    private readonly FlatGeneratorInfo _generatorInfo;
    private readonly JavaRandom _random;

    private readonly LakeFeature _featureWaterLake = new(Block.Water.ID);
    private readonly LakeFeature _featureLavaLake = new(Block.Lava.ID);
    private readonly DungeonFeature _featureDungeon = new();
    private readonly ClayOreFeature _featureClay = new(32);
    private readonly OreFeature _featureDirt = new(Block.Dirt.ID, 32);
    private readonly OreFeature _featureGravel = new(Block.Gravel.ID, 32);
    private readonly OreFeature _featureCoal = new(Block.CoalOre.ID, 16);
    private readonly OreFeature _featureIron = new(Block.IronOre.ID, 8);
    private readonly OreFeature _featureGold = new(Block.GoldOre.ID, 8);
    private readonly OreFeature _featureRedstone = new(Block.RedstoneOre.ID, 7);
    private readonly OreFeature _featureDiamond = new(Block.DiamondOre.ID, 7);
    private readonly OreFeature _featureLapis = new(Block.LapisOre.ID, 6);
    private readonly PlantPatchFeature _featureDandelion = new(Block.Dandelion.ID);
    private readonly PlantPatchFeature _featureRose = new(Block.Rose.ID);
    private readonly PlantPatchFeature _featureBrownMushroom = new(Block.BrownMushroom.ID);
    private readonly PlantPatchFeature _featureRedMushroom = new(Block.RedMushroom.ID);
    private readonly SugarCanePatchFeature _featureSugarcane = new();
    private readonly PumpkinPatchFeature _featurePumpkin = new();
    private readonly CactusPatchFeature _featureCactus = new();
    private readonly DeadBushPatchFeature _featureDeadBush = new(Block.DeadBush.ID);
    private readonly GrassPatchFeature _featureGrass = new(Block.Grass.ID, 1);
    private readonly SpringFeature _featureWaterSpring = new(Block.FlowingWater.ID);
    private readonly SpringFeature _featureLavaSpring = new(Block.FlowingLava.ID);

    public FlatChunkGenerator(IWorldContext world)
    {
        _world = world;
        _generatorInfo = FlatGeneratorInfo.CreateFromString(world.Properties.GeneratorOptions);
        _random = new JavaRandom(world.Seed);
    }

    public IChunkSource CreateParallelInstance() => new FlatChunkGenerator(_world);

    public Chunk GetChunk(int chunkX, int chunkZ)
    {
        int blockL = ChuckFormat.ChunkSize;
        byte[] blocks = new byte[blockL];
        ChunkNibbleArray meta = new(blockL);

        foreach (FlatLayerInfo layer in _generatorInfo.FlatLayers)
        {
            int blockId = layer.FillBlock;
            int blockMeta = layer.FillBlockMeta;

            for (int y = layer.MinY; y < layer.MinY + layer.LayerCount & y < ChuckFormat.WorldHeight; ++y)
            {

                for (int x = 0; x < 16; ++x)
                {
                    for (int z = 0; z < 16; ++z)
                    {
                        int index = ChuckFormat.GetIndex(x, y, z);
                        blocks[index] = (byte)blockId;
                        if (blockMeta > 0)
                        {
                            meta.SetNibble(x, y, z, blockMeta);
                        }
                    }
                }
            }
        }

        Chunk chunk = new(_world, blocks, chunkX, chunkZ)
        {
            Meta = meta
        };

        chunk.PopulateHeightMap();
        return chunk;
    }

    public bool Save(bool bl, LoadingDisplay loadingDisplay) => true;

    public bool CanSave() => true;

    public bool IsChunkLoaded(int x, int z) => true;
    public Chunk LoadChunk(int x, int z) => GetChunk(x, z);

    public void DecorateTerrain(IChunkSource chunkSource, int chunkX, int chunkZ)
    {
        int blockX = chunkX * 16;
        int blockZ = chunkZ * 16;
        Biome chunkBiome = Biome.Plains; // Default

        _random.SetSeed(_world.Seed);
        long xOffset = _random.NextLong() / 2L * 2L + 1L;
        long zOffset = _random.NextLong() / 2L * 2L + 1L;
        _random.SetSeed(chunkX * xOffset + chunkZ * zOffset ^ _world.Seed);

        int featureX;
        int featureY;
        int featureZ;

        bool hasLakes = _generatorInfo.WorldFeatures.ContainsKey("lake");
        bool hasLavaLakes = _generatorInfo.WorldFeatures.ContainsKey("lava_lake");
        bool hasDungeons = _generatorInfo.WorldFeatures.ContainsKey("dungeon");
        bool hasDecoration = _generatorInfo.WorldFeatures.ContainsKey("decoration");

        if (hasLakes && _random.NextInt(4) == 0)
        {
            featureX = blockX + _random.NextInt(16) + 8;
            featureY = _random.NextInt(128);
            featureZ = blockZ + _random.NextInt(16) + 8;
            _featureWaterLake.Generate(_world, _random, featureX, featureY, featureZ);
        }

        if (hasLavaLakes && _random.NextInt(8) == 0)
        {
            featureX = blockX + _random.NextInt(16) + 8;
            featureY = _random.NextInt(_random.NextInt(120) + 8);
            featureZ = blockZ + _random.NextInt(16) + 8;
            if (featureY < 64 || _random.NextInt(10) == 0)
            {
                _featureLavaLake.Generate(_world, _random, featureX, featureY, featureZ);
            }
        }

        if (hasDungeons)
        {
            for (int i = 0; i < 8; ++i)
            {
                featureX = blockX + _random.NextInt(16) + 8;
                featureY = _random.NextInt(128);
                featureZ = blockZ + _random.NextInt(16) + 8;
                _featureDungeon.Generate(_world, _random, featureX, featureY, featureZ);
            }
        }

        if (hasDecoration)
        {
            // Ores
            for (int i = 0; i < 10; ++i)
            {
                featureX = blockX + _random.NextInt(16);
                featureY = _random.NextInt(128);
                featureZ = blockZ + _random.NextInt(16);
                _featureClay.Generate(_world, _random, featureX, featureY, featureZ);
            }

            for (int i = 0; i < 20; ++i)
            {
                featureX = blockX + _random.NextInt(16);
                featureY = _random.NextInt(128);
                featureZ = blockZ + _random.NextInt(16);
                _featureDirt.Generate(_world, _random, featureX, featureY, featureZ);
            }

            for (int i = 0; i < 10; ++i)
            {
                featureX = blockX + _random.NextInt(16);
                featureY = _random.NextInt(128);
                featureZ = blockZ + _random.NextInt(16);
                _featureGravel.Generate(_world, _random, featureX, featureY, featureZ);
            }

            for (int i = 0; i < 20; ++i)
            {
                featureX = blockX + _random.NextInt(16);
                featureY = _random.NextInt(128);
                featureZ = blockZ + _random.NextInt(16);
                _featureCoal.Generate(_world, _random, featureX, featureY, featureZ);
            }

            for (int i = 0; i < 20; ++i)
            {
                featureX = blockX + _random.NextInt(16);
                featureY = _random.NextInt(64);
                featureZ = blockZ + _random.NextInt(16);
                _featureIron.Generate(_world, _random, featureX, featureY, featureZ);
            }

            for (int i = 0; i < 2; ++i)
            {
                featureX = blockX + _random.NextInt(16);
                featureY = _random.NextInt(32);
                featureZ = blockZ + _random.NextInt(16);
                _featureGold.Generate(_world, _random, featureX, featureY, featureZ);
            }

            for (int i = 0; i < 8; ++i)
            {
                featureX = blockX + _random.NextInt(16);
                featureY = _random.NextInt(16);
                featureZ = blockZ + _random.NextInt(16);
                _featureRedstone.Generate(_world, _random, featureX, featureY, featureZ);
            }

            for (int i = 0; i < 1; ++i)
            {
                featureX = blockX + _random.NextInt(16);
                featureY = _random.NextInt(16);
                featureZ = blockZ + _random.NextInt(16);
                _featureDiamond.Generate(_world, _random, featureX, featureY, featureZ);
            }

            for (int i = 0; i < 1; ++i)
            {
                featureX = blockX + _random.NextInt(16);
                featureY = _random.NextInt(16) + _random.NextInt(16);
                featureZ = blockZ + _random.NextInt(16);
                _featureLapis.Generate(_world, _random, featureX, featureY, featureZ);
            }

            // Trees
            int numberOfTrees = 0;
            if (_random.NextInt(10) == 0) numberOfTrees++;

            for (int i = 0; i < numberOfTrees; ++i)
            {
                featureX = blockX + _random.NextInt(16) + 8;
                featureZ = blockZ + _random.NextInt(16) + 8;
                Feature treeFeature = chunkBiome.GetRandomWorldGenForTrees(_random);
                treeFeature.prepare(1.0D, 1.0D, 1.0D);
                treeFeature.Generate(_world, _random, featureX, _world.Reader.GetTopY(featureX, featureZ), featureZ);
            }

            // Flowers and Mushrooms
            for (int i = 0; i < 2; ++i)
            {
                featureX = blockX + _random.NextInt(16) + 8;
                featureY = _random.NextInt(128);
                featureZ = blockZ + _random.NextInt(16) + 8;
                _featureDandelion.Generate(_world, _random, featureX, featureY, featureZ);
            }

            if (_random.NextInt(2) == 0)
            {
                featureX = blockX + _random.NextInt(16) + 8;
                featureY = _random.NextInt(128);
                featureZ = blockZ + _random.NextInt(16) + 8;
                _featureRose.Generate(_world, _random, featureX, featureY, featureZ);
            }

            if (_random.NextInt(4) == 0)
            {
                featureX = blockX + _random.NextInt(16) + 8;
                featureY = _random.NextInt(128);
                featureZ = blockZ + _random.NextInt(16) + 8;
                _featureBrownMushroom.Generate(_world, _random, featureX, featureY, featureZ);
            }

            if (_random.NextInt(8) == 0)
            {
                featureX = blockX + _random.NextInt(16) + 8;
                featureY = _random.NextInt(128);
                featureZ = blockZ + _random.NextInt(16) + 8;
                _featureRedMushroom.Generate(_world, _random, featureX, featureY, featureZ);
            }

            // Sugarcane, Pumpkins
            for (int i = 0; i < 10; ++i)
            {
                featureX = blockX + _random.NextInt(16) + 8;
                featureY = _random.NextInt(128);
                featureZ = blockZ + _random.NextInt(16) + 8;
                _featureSugarcane.Generate(_world, _random, featureX, featureY, featureZ);
            }

            if (_random.NextInt(32) == 0)
            {
                featureX = blockX + _random.NextInt(16) + 8;
                featureY = _random.NextInt(128);
                featureZ = blockZ + _random.NextInt(16) + 8;
                _featurePumpkin.Generate(_world, _random, featureX, featureY, featureZ);
            }
            // Grass, Dead Bush, Cactus
            for (int i = 0; i < 20; ++i)
            {
                featureX = blockX + _random.NextInt(16) + 8;
                featureY = _random.NextInt(128);
                featureZ = blockZ + _random.NextInt(16) + 8;
                _featureGrass.Generate(_world, _random, featureX, featureY, featureZ);
            }

            for (int i = 0; i < 2; ++i)
            {
                featureX = blockX + _random.NextInt(16) + 8;
                featureY = _random.NextInt(128);
                featureZ = blockZ + _random.NextInt(16) + 8;
                _featureDeadBush.Generate(_world, _random, featureX, featureY, featureZ);
            }

            for (int i = 0; i < 10; ++i)
            {
                featureX = blockX + _random.NextInt(16) + 8;
                featureY = _random.NextInt(128);
                featureZ = blockZ + _random.NextInt(16) + 8;
                _featureCactus.Generate(_world, _random, featureX, featureY, featureZ);
            }

            // Spring Features
            for (int i = 0; i < 50; ++i)
            {
                featureX = blockX + _random.NextInt(16) + 8;
                featureY = _random.NextInt(_random.NextInt(120) + 8);
                featureZ = blockZ + _random.NextInt(16) + 8;
                _featureWaterSpring.Generate(_world, _random, featureX, featureY, featureZ);
            }

            for (int i = 0; i < 20; ++i)
            {
                featureX = blockX + _random.NextInt(16) + 8;
                featureY = _random.NextInt(_random.NextInt(_random.NextInt(112) + 8) + 8);
                featureZ = blockZ + _random.NextInt(16) + 8;
                _featureLavaSpring.Generate(_world, _random, featureX, featureY, featureZ);
            }
        }
    }

    public bool Tick() => false;
    public string GetDebugInfo() => "FlatLevelSource";
}
