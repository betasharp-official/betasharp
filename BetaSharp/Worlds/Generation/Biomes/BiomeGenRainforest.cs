using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Generation.Generators.Features;

namespace BetaSharp.Worlds.Generation.Biomes;

internal class BiomeGenRainforest : Biome
{
    public override Feature GetRandomWorldGenForTrees(JavaRandom rand) => rand.NextInt(3) == 0 ? new LargeOakTreeFeature() : new OakTreeFeature();
}
