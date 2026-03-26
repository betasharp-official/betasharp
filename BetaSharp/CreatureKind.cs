using BetaSharp.Blocks.Materials;
using BetaSharp.Entities;
using BetaSharp.Worlds.Core;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp;

public sealed record CreatureKind(Type EntityType, int MobCap, Material SpawnMaterial, bool Peaceful)
{
    public static readonly CreatureKind Monster = new CreatureKind(typeof(Monster), 70, Material.Air, false);
    public static readonly CreatureKind Creature = new CreatureKind(typeof(EntityAnimal), 15, Material.Air, true);
    public static readonly CreatureKind WaterCreature = new CreatureKind(typeof(EntityWaterMob), 5, Material.Water, true);

    public static readonly CreatureKind[] Values = [Monster, Creature, WaterCreature];

    public bool CanSpawnAtLocation(IBlockReader world, int x, int y, int z)
    {
        if (SpawnMaterial == Material.Water)
        {
            return world.GetMaterial(x, y, z).IsFluid && !world.ShouldSuffocate(x, y + 1, z);
        }
        else
        {
            return world.ShouldSuffocate(x, y - 1, z) && !world.ShouldSuffocate(x, y, z) &&
                   !world.GetMaterial(x, y, z).IsFluid && !world.ShouldSuffocate(x, y + 1, z);
        }
    }
}
