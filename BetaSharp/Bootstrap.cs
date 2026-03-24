using BetaSharp.Registries;

namespace BetaSharp;

public class Bootstrap
{
    public static void Initialize()
    {
        DefaultRegistries.Initialize();
    }
}
