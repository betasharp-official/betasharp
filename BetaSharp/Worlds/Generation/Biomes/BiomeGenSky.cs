using BetaSharp.Entities;

namespace BetaSharp.Worlds.Generation.Biomes;

internal class BiomeGenSky : Biome
{
    public BiomeGenSky()
    {
        MonsterList.Clear();
        CreatureList.Clear();
        WaterCreatureList.Clear();

        CreatureList.Add(new SpawnListEntry(w => new EntityChicken(w)), 10);
    }

    public override int GetSkyColorByTemp(float rand) => 0xC0C0FF;
}
