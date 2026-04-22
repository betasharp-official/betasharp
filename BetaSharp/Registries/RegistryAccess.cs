using BetaSharp.Network.Packets.S2CPlay;
using BetaSharp.Registries.Data;

namespace BetaSharp.Registries;

/// <summary>
/// A frozen, contextual container of registries — both static built-ins and dynamic
/// data-driven ones. One <see cref="RegistryAccess"/> is built per server instance;
/// world-specific datapacks are layered on top via <see cref="WithWorldDatapacks"/>.
/// </summary>
public sealed class RegistryAccess
{
    // --- Static registry (built-in, never change) ---
    private readonly Dictionary<ResourceLocation, object> _builtIns;

    // --- Dynamic registry (data-driven) ---
    // _serverLoaders: loaded from base + global datapacks only (never has world datapacks)
    // _activeLoaders: what Get<T> actually queries — equals _serverLoaders unless a world is loaded,
    //                 in which case it contains clones of _serverLoaders with world datapacks merged in
    private readonly Dictionary<ResourceLocation, DataAssetLoader> _serverLoaders;
    private readonly Dictionary<ResourceLocation, DataAssetLoader> _activeLoaders;

    private readonly string? _basePath;
    private readonly string? _datapackPath;
    private readonly string? _worldDatapackPath;

    // ---- Static factory registration (called during bootstrap) ----

    private static readonly Dictionary<ResourceLocation, object> s_builtIns = [];
    private static readonly List<IDynamicRegistryEntry> s_dynamicEntries = [];

    private interface IDynamicRegistryEntry
    {
        ResourceLocation Key { get; }
        bool IsReloadable { get; }
        bool CanSync { get; }
        DataAssetLoader CreateLoader();
        DataAssetLoader? CloneForWorld(DataAssetLoader loader, string worldDatapackPath);
        RegistryDataS2CPacket? BuildSyncPacket(RegistryAccess registryAccess);
    }

    private sealed class DynamicRegistryEntry<T>(RegistryDefinition<T> definition) : IDynamicRegistryEntry
        where T : class, IDataAsset
    {
        public ResourceLocation Key => definition.Key.Location;
        public bool IsReloadable => definition.IsReloadable;
        public bool CanSync => definition.CanSync;
        public DataAssetLoader CreateLoader() => definition.CreateLoader();
        public DataAssetLoader? CloneForWorld(DataAssetLoader loader, string worldDatapackPath)
            => ((DataAssetLoader<T>)loader).CloneForWorldDatapacks(worldDatapackPath);
        public RegistryDataS2CPacket? BuildSyncPacket(RegistryAccess registryAccess)
        {
            IReadableRegistry<T>? registry = registryAccess.Get(definition.Key);
            return registry is null ? null : RegistryDataS2CPacket.Get(definition.Key, registry);
        }
    }

    public static void AddBuiltIn<T>(RegistryKey<T> key, IReadableRegistry<T> registry) where T : class
        => s_builtIns[key.Location] = registry;

    public static void AddDynamic<T>(RegistryDefinition<T> definition) where T : class, IDataAsset
        => s_dynamicEntries.Add(new DynamicRegistryEntry<T>(definition));

    /// <summary>For test isolation only — clears all registered dynamic entries.</summary>
    internal static void ClearDynamicEntries() => s_dynamicEntries.Clear();

    // ---- Construction ----

    private RegistryAccess(
        Dictionary<ResourceLocation, object> builtIns,
        Dictionary<ResourceLocation, DataAssetLoader> serverLoaders,
        Dictionary<ResourceLocation, DataAssetLoader> activeLoaders,
        string? basePath,
        string? datapackPath,
        string? worldDatapackPath = null)
    {
        _builtIns = builtIns;
        _serverLoaders = serverLoaders;
        _activeLoaders = activeLoaders;
        _basePath = basePath;
        _datapackPath = datapackPath;
        _worldDatapackPath = worldDatapackPath;
    }

    /// <summary>An empty <see cref="RegistryAccess"/> with no registries. Safe to use as a null-object.</summary>
    public static RegistryAccess Empty { get; } = new([], [], [], null, null);

    // ---- Query ----

    /// <summary>
    /// Looks up a registry by its typed key. Returns <c>null</c> if not registered.
    /// </summary>
    public IReadableRegistry<T>? Get<T>(RegistryKey<T> key) where T : class
    {
        if (_activeLoaders.TryGetValue(key.Location, out DataAssetLoader? loader))
            return (IReadableRegistry<T>)loader;
        if (_builtIns.TryGetValue(key.Location, out object? builtin))
            return (IReadableRegistry<T>)builtin;
        return null;
    }

    /// <summary>
    /// Looks up a registry by its typed key. Throws if not registered.
    /// </summary>
    public IReadableRegistry<T> GetOrThrow<T>(RegistryKey<T> key) where T : class
        => Get(key) ?? throw new InvalidOperationException(
            $"Registry '{key}' not found. Ensure RegistryAccess.Build() has been called.");

    // ---- Build ----

    /// <summary>
    /// Builds a new <see cref="RegistryAccess"/> by loading all dynamic registries from
    /// the specified paths and combining them with the static built-in registries.
    /// </summary>
    /// <param name="basePath">
    /// Root directory that contains an <c>assets/</c> subdirectory.
    /// Pass <c>null</c> to use the current working directory.
    /// </param>
    /// <param name="datapackPath">
    /// Directory that contains a <c>datapacks/</c> subdirectory for server-wide packs.
    /// Pass <c>null</c> to skip global datapack loading.
    /// </param>
    /// <param name="worldDatapackPath">
    /// World directory that contains a <c>datapacks/</c> subdirectory for world-specific packs.
    /// Pass <c>null</c> to skip world datapack loading.
    /// </param>
    public static RegistryAccess Build(
        string? basePath = null,
        string? datapackPath = null,
        string? worldDatapackPath = null)
    {
        bool hadAnyErrors = false;
        string? error = null;

        var builtIns = new Dictionary<ResourceLocation, object>(s_builtIns);

        // Dynamic (data-driven) registries — load from base + global datapacks
        var serverLoaders = new Dictionary<ResourceLocation, DataAssetLoader>();
        foreach (IDynamicRegistryEntry entry in s_dynamicEntries)
        {
            DataAssetLoader loader = entry.CreateLoader();
            loader.LoadFromPaths(basePath, datapackPath, null);  // no world datapacks here

            if (loader.HasErrors) hadAnyErrors = true;

            serverLoaders[entry.Key] = loader;
        }

        // Active loaders: clone with world datapacks if provided, otherwise same as server loaders
        Dictionary<ResourceLocation, DataAssetLoader> activeLoaders;
        if (worldDatapackPath != null)
        {
            activeLoaders = [];
            foreach (IDynamicRegistryEntry entry in s_dynamicEntries)
            {
                DataAssetLoader? worldLoader = entry.CloneForWorld(serverLoaders[entry.Key], worldDatapackPath);

                if (worldLoader == null) continue;

                if (worldLoader.HasErrors) hadAnyErrors = true;

                activeLoaders[entry.Key] = worldLoader;
            }
        }
        else
        {
            activeLoaders = serverLoaders;
        }

        foreach (DataAssetLoader loader in serverLoaders.Values)
        {
            loader.Freeze();
        }

        if (activeLoaders != serverLoaders)
        {
            foreach (DataAssetLoader loader in activeLoaders.Values)
            {
                loader.Freeze();
            }
        }

        if (hadAnyErrors)
        {
            foreach (DataAssetLoader loader in serverLoaders.Values)
            {
                error ??= loader.FirstErrorMessage;
            }

            if (worldDatapackPath != null)
            {
                foreach (DataAssetLoader loader in activeLoaders.Values)
                {
                    error ??= loader.FirstErrorMessage;
                }
            }

            throw new AssetLoadException(error ?? "One or more registries failed to load.");
        }

        return new RegistryAccess(builtIns, serverLoaders, activeLoaders, basePath, datapackPath, worldDatapackPath);
    }

    /// <summary>
    /// Returns a new <see cref="RegistryAccess"/> where all dynamic registries are cloned
    /// from the server-level (base + global datapack) state and then have
    /// <paramref name="worldDatapackPath"/> merged on top.
    /// Does NOT re-read base or global datapacks from disk.
    /// </summary>
    public RegistryAccess WithWorldDatapacks(string worldDatapackPath)
    {
        var activeLoaders = new Dictionary<ResourceLocation, DataAssetLoader>();
        foreach (IDynamicRegistryEntry entry in s_dynamicEntries)
        {
            if (_serverLoaders.TryGetValue(entry.Key, out DataAssetLoader? serverLoader))
            {
                DataAssetLoader? assetLoader = entry.CloneForWorld(serverLoader, worldDatapackPath);

                if (assetLoader == null) continue;

                activeLoaders[entry.Key] = assetLoader;
            }
        }

        foreach (DataAssetLoader loader in activeLoaders.Values)
        {
            loader.Freeze();
        }

        return new RegistryAccess(_builtIns, _serverLoaders, activeLoaders, _basePath, _datapackPath, worldDatapackPath);
    }

    /// <summary>
    /// Returns a new <see cref="RegistryAccess"/> using only the server-level (base + global
    /// datapack) state
    /// </summary>
    public RegistryAccess WithoutWorldDatapacks()
        => new(_builtIns, _serverLoaders, _serverLoaders, _basePath, _datapackPath);

    /// <summary>
    /// Builds a <see cref="RegistryDataS2CPacket"/> for each reloadable dynamic registry.
    /// </summary>
    public IEnumerable<RegistryDataS2CPacket> BuildSyncPackets()
    {
        foreach (IDynamicRegistryEntry entry in s_dynamicEntries)
        {
            if (!entry.IsReloadable || !entry.CanSync) continue;
            RegistryDataS2CPacket? packet = entry.BuildSyncPacket(this);
            if (packet is not null) yield return packet;
        }
    }

    /// <summary>
    /// Rebuilds this <see cref="RegistryAccess"/> from disk, reloading only registries whose
    /// <see cref="RegistryDefinition{T}.IsReloadable"/> is <c>true</c>.
    /// </summary>
    public RegistryAccess Rebuild()
    {
        bool hadAnyErrors = false;

        // Server loaders: fresh load for reloadable entries, reuse frozen loader for the rest.
        var serverLoaders = new Dictionary<ResourceLocation, DataAssetLoader>();
        foreach (IDynamicRegistryEntry entry in s_dynamicEntries)
        {
            if (entry.IsReloadable)
            {
                DataAssetLoader loader = entry.CreateLoader();
                loader.LoadFromPaths(_basePath, _datapackPath, null);

                if (loader.HasErrors) hadAnyErrors = true;

                loader.Freeze();
                serverLoaders[entry.Key] = loader;
            }
            else if (_serverLoaders.TryGetValue(entry.Key, out DataAssetLoader? existing))
            {
                serverLoaders[entry.Key] = existing;
            }
        }

        // Active loaders: reloadable entries get a fresh world-datapack clone;
        // non-reloadable entries carry forward whatever was already active (world datapacks included).
        Dictionary<ResourceLocation, DataAssetLoader> activeLoaders;
        if (_worldDatapackPath != null)
        {
            activeLoaders = [];
            foreach (IDynamicRegistryEntry entry in s_dynamicEntries)
            {
                if (entry.IsReloadable && serverLoaders.TryGetValue(entry.Key, out DataAssetLoader? serverLoader))
                {
                    DataAssetLoader? worldLoader = entry.CloneForWorld(serverLoader, _worldDatapackPath);

                    if (worldLoader == null) continue;

                    worldLoader.Freeze();

                    if (worldLoader.HasErrors) hadAnyErrors = true;

                    activeLoaders[entry.Key] = worldLoader;
                }
                else if (_activeLoaders.TryGetValue(entry.Key, out DataAssetLoader? existing))
                {
                    activeLoaders[entry.Key] = existing;
                }
            }
        }
        else
        {
            activeLoaders = serverLoaders;
        }

        if (hadAnyErrors)
        {
            string? firstError = null;
            foreach (DataAssetLoader loader in serverLoaders.Values)
            {
                firstError ??= loader.FirstErrorMessage;
            }

            foreach (DataAssetLoader loader in activeLoaders.Values)
            {
                firstError ??= loader.FirstErrorMessage;
            }

            throw new AssetLoadException(firstError ?? "One or more registries failed to reload.");
        }

        return new RegistryAccess(_builtIns, serverLoaders, activeLoaders, _basePath, _datapackPath, _worldDatapackPath);
    }
}
