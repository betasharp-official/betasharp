using BetaSharp.DataAsset;

namespace BetaSharp.Registries;

/// <summary>
/// Describes a data-driven registry: how to create its loader and where its assets live.
/// Register instances with <see cref="RegistryAccess.AddDynamic{T}"/> during bootstrap
/// so that <see cref="RegistryAccess.Build"/> can discover and load them automatically.
/// </summary>
public sealed class RegistryDefinition<T>(RegistryKey<T> key, string assetPath, LoadLocations locations = LoadLocations.AllData) where T : class, IDataAsset
{
    public RegistryKey<T> Key { get; } = key;
    internal string AssetPath { get; } = assetPath;
    internal LoadLocations Locations { get; } = locations;

    internal DataAssetLoader<T> CreateLoader() => new(AssetPath, Locations, allowUnhandled: false);
}
