using BetaSharp.Entities;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Generation.Generators.Features;

namespace BetaSharp.Worlds.Generation.Biomes;

internal class BiomeGenTaiga : Biome
{
    public BiomeGenTaiga() => CreatureList.Add(new SpawnListEntry(w => new EntityWolf(w)), 2);

    public override Feature GetRandomWorldGenForTrees(JavaRandom rand) => rand.NextInt(3) == 0 ? new PineTreeFeature() : new SpruceTreeFeature();
}
