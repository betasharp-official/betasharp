using BetaSharp.Blocks.Entities;
using BetaSharp.Entities;
using BetaSharp.Rules;
using BetaSharp.Worlds.Biomes;
using BetaSharp.Worlds.Generation.Biomes;

namespace BetaSharp.Registries;

public static class DefaultRegistries
{
    public static readonly IRegistry<EntityType> EntityTypes =
        new IndexedRegistry<EntityType>(ResourceLocation.Parse("entity_types"));

    public static readonly IRegistry<Biome> Biomes =
        new IndexedRegistry<Biome>(ResourceLocation.Parse("biomes"));

    public static readonly IRegistry<BlockEntityType> BlockEntityTypes =
        new IndexedRegistry<BlockEntityType>(ResourceLocation.Parse("block_entity_types"));

    //TODO: Implement this properly
    public static readonly IRegistry<IGameRule> GameRules =
        new IndexedRegistry<IGameRule>(ResourceLocation.Parse("game_rules"));

    public static void Initialize()
    {
        EntityTypes.Bootstrap(typeof(EntityRegistry));
        Biomes.Bootstrap(typeof(Biome));
        BlockEntityTypes.Bootstrap(typeof(BlockEntity));

        FreezeAll();
    }

    private static void FreezeAll()
    {
        EntityTypes.Freeze();
        Biomes.Freeze();
        BlockEntityTypes.Freeze();
    }
}
