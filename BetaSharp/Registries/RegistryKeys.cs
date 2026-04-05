using BetaSharp.Blocks.Entities;
using BetaSharp.Entities;
using BetaSharp.Rules;
using BetaSharp.Worlds.Generation.Biomes;

namespace BetaSharp.Registries;

/// <summary>
/// Well-known <see cref="RegistryKey{T}"/> constants for all built-in registry types.
/// </summary>
public static class RegistryKeys
{
    public static readonly RegistryKey<EntityType> EntityTypes = new("betasharp:entity_type");
    public static readonly RegistryKey<Biome> Biomes = new("betasharp:biome");
    public static readonly RegistryKey<BlockEntityType> BlockEntityTypes = new("betasharp:block_entity_type");
    public static readonly RegistryKey<IGameRule> GameRules = new("betasharp:game_rule");
    public static readonly RegistryKey<GameMode> GameModes = new("betasharp:game_mode");
}
