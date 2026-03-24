using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Worlds.Generation.Generators.Features;

public abstract class Feature
{
    public abstract bool Generate(IWorldContext level, JavaRandom rand, int x, int y, int z);

    public virtual void prepare(double d0, double d1, double d2)
    {
    }
}
