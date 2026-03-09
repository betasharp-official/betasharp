using BetaSharp.Worlds.Core;

namespace BetaSharp.Worlds.Generation.Generators.Features;

public abstract class Feature
{
    public abstract bool Generate(IBlockWorldContext level, int x, int y, int z);

    public virtual void prepare(double d0, double d1, double d2)
    {
    }
}
