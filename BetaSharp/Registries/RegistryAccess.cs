using BetaSharp.DataAsset;

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

    // ---- Static factory registration (called during bootstrap) ----

    private static readonly List<IDynamicRegistryEntry> s_dynamicEntries = [];

    private interface IDynamicRegistryEntry
    {
        ResourceLocation Key { get; }
        DataAssetLoader CreateLoader();
        DataAssetLoader CloneForWorld(DataAssetLoader loader, string worldDatapackPath);
    }

    private sealed class DynamicRegistryEntry<T>(RegistryDefinition<T> definition) : IDynamicRegistryEntry
        where T : class, IDataAsset
    {
        public ResourceLocation Key => definition.Key.Location;
        public DataAssetLoader CreateLoader() => definition.CreateLoader();
        public DataAssetLoader CloneForWorld(DataAssetLoader loader, string worldDatapackPath)
            => ((DataAssetLoader<T>)loader).CloneForWorldDatapacks(worldDatapackPath);
    }

    public static void AddDynamic<T>(RegistryDefinition<T> definition) where T : class, IDataAsset
    {
        s_dynamicEntries.Add(new DynamicRegistryEntry<T>(definition));
    }

    /// <summary>For test isolation only — clears all registered dynamic entries.</summary>
    internal static void ClearDynamicEntries() => s_dynamicEntries.Clear();

    // ---- Construction ----

    private RegistryAccess(
        Dictionary<ResourceLocation, object> builtIns,
        Dictionary<ResourceLocation, DataAssetLoader> serverLoaders,
        Dictionary<ResourceLocation, DataAssetLoader> activeLoaders,
        string? basePath,
        string? datapackPath)
    {
        _builtIns = builtIns;
        _serverLoaders = serverLoaders;
        _activeLoaders = activeLoaders;
        _basePath = basePath;
        _datapackPath = datapackPath;
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
        // Built-in static registries
        var builtIns = new Dictionary<ResourceLocation, object>
        {
            [RegistryKeys.EntityTypes.Location] = DefaultRegistries.EntityTypes,
            [RegistryKeys.Biomes.Location] = DefaultRegistries.Biomes,
            [RegistryKeys.BlockEntityTypes.Location] = DefaultRegistries.BlockEntityTypes,
            [RegistryKeys.GameRules.Location] = DefaultRegistries.GameRules,
        };

        // Dynamic (data-driven) registries — load from base + global datapacks
        var serverLoaders = new Dictionary<ResourceLocation, DataAssetLoader>();
        foreach (IDynamicRegistryEntry entry in s_dynamicEntries)
        {
            DataAssetLoader loader = entry.CreateLoader();
            loader.LoadFromPaths(basePath, datapackPath, null);  // no world datapacks here
            loader.WaitForLoad();
            serverLoaders[entry.Key] = loader;
        }

        // Active loaders: clone with world datapacks if provided, otherwise same as server loaders
        Dictionary<ResourceLocation, DataAssetLoader> activeLoaders;
        if (worldDatapackPath != null)
        {
            activeLoaders = [];
            foreach (IDynamicRegistryEntry entry in s_dynamicEntries)
            {
                activeLoaders[entry.Key] = entry.CloneForWorld(serverLoaders[entry.Key], worldDatapackPath);
            }
        }
        else
        {
            activeLoaders = serverLoaders;
        }

        return new RegistryAccess(builtIns, serverLoaders, activeLoaders, basePath, datapackPath);
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
                activeLoaders[entry.Key] = entry.CloneForWorld(serverLoader, worldDatapackPath);
        }
        return new RegistryAccess(_builtIns, _serverLoaders, activeLoaders, _basePath, _datapackPath);
    }

    /// <summary>
    /// Returns a new <see cref="RegistryAccess"/> using only the server-level (base + global
    /// datapack) state — discarding any world-specific datapack layer without any disk I/O.
    /// </summary>
    public RegistryAccess WithoutWorldDatapacks()
        => new(_builtIns, _serverLoaders, _serverLoaders, _basePath, _datapackPath);
}
