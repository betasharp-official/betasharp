using BetaSharp.Entities;

namespace BetaSharp.Worlds.Generation.Biomes;

internal class BiomeGenHell : Biome
{
    public BiomeGenHell()
    {
        MonsterList.Clear();
        CreatureList.Clear();
        WaterCreatureList.Clear();

        MonsterList.Add(new SpawnListEntry(w => new EntityGhast(w)), 10);
        MonsterList.Add(new SpawnListEntry(w => new EntityPigZombie(w)), 10);
    }
}
