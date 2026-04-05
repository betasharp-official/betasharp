using BetaSharp.DataAsset;

namespace BetaSharp.Registries;

/// <summary>
/// Describes a data-driven registry: how to create its loader and where its assets live.
/// Register instances with <see cref="RegistryAccess.AddDynamic{T}"/> during bootstrap
/// so that <see cref="RegistryAccess.Build"/> can discover and load them automatically.
/// </summary>
public sealed class RegistryDefinition<T>(
    RegistryKey<T> key,
    string assetPath,
    LoadLocations locations = LoadLocations.AllData,
    bool isReloadable = true) where T : class, IDataAsset
{
    public RegistryKey<T> Key { get; } = key;
    internal string AssetPath { get; } = assetPath;
    internal LoadLocations Locations { get; } = locations;

    /// <summary>
    /// When <c>false</c>, this registry is locked after world creation and skipped during
    /// <c>/reload</c>. Use for baked data that cannot safely
    /// change while a world is loaded.
    /// </summary>
    public bool IsReloadable { get; } = isReloadable;

    internal DataAssetLoader<T> CreateLoader() => new(AssetPath, Locations, allowUnhandled: false);
}
