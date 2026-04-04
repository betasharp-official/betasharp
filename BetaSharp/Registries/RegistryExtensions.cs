using System.Runtime.CompilerServices;

namespace BetaSharp.Registries;

public static class RegistryExtensions
{
    public static void Bootstrap<T>(this IRegistry<T> registry, Type provider) where T : class
    {
        RuntimeHelpers.RunClassConstructor(provider.TypeHandle);
    }
}
