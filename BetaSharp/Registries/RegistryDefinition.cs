using BetaSharp.Registries.Data;

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
    bool isReloadable = true, bool serversideOnly = false) where T : class, IDataAsset
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

    /// <summary>
    /// Data that is serverside don't need to be synced.
    /// Resource packs are client side only, and don't need to be synced either.
    /// </summary>
    public bool CanSync { get; } = !(serversideOnly || locations == LoadLocations.Resourcepack);

    internal DataAssetLoader<T> CreateLoader() => new(AssetPath, Locations , !CanSync);
}
