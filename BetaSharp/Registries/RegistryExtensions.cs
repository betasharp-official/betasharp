using System.Runtime.CompilerServices;
using BetaSharp.Registries.Data;

namespace BetaSharp.Registries;

public static class RegistryExtensions
{
    public static void Bootstrap<T>(this IRegistry<T> registry, Type provider) where T : class
    {
        RuntimeHelpers.RunClassConstructor(provider.TypeHandle);
    }

    /// <summary>
    /// Returns the underlying <see cref="DataAssetLoader{T}"/> for this registry.
    /// Use this when you need data-driven string lookup features (<see cref="DataAssetLoader{T}.TryGet"/>
    /// or <see cref="DataAssetLoader{T}.TryGetByPrefix"/>) that are not part of
    /// <see cref="IReadableRegistry{T}"/>.
    /// Throws if the registry is not data-driven.
    /// </summary>
    public static DataAssetLoader<T> AsAssetLoader<T>(this IReadableRegistry<T> registry)
        where T : class, IDataAsset
        => registry as DataAssetLoader<T>
            ?? throw new InvalidOperationException(
                $"Registry '{registry.RegistryKey}' is not a data-driven registry.");
}
