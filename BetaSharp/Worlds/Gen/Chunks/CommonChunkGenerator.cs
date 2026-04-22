using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Chunks;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Worlds.Gen.Chunks;

public abstract class CommonChunkGenerator
{
    private protected readonly IWorldContext _world;
    private protected readonly JavaRandom _random;
    private protected readonly long _seed;

    public CommonChunkGenerator(IWorldContext world, long seed)
    {
        _world = world;
        _seed = seed;
        _random = new JavaRandom(seed);
    }
}
