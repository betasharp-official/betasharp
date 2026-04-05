using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace BetaSharp.DataAsset;

public abstract class DataAssetLoader
{
    private protected static readonly ILogger s_logger = Log.Instance.For(typeof(DataAssetLoader).FullName ?? nameof(DataAssetLoader));

    private static readonly List<DataAssetLoader> s_assetLoaders = [];

    private protected static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenReading
    };

    private protected readonly LoadLocations Locations;

    private protected LoadLocations LoadedAssetsModify;

    public bool IsFrozen { get; private set; }
    public bool HasErrors { get; protected set; }
    public string? FirstErrorMessage { get; protected set; }

    private static string? s_lastDataPath = null;
    private static string? s_lastWorldDataPath = null;
    private static string? s_lastResourcePath = null;

    private protected DataAssetLoader(LoadLocations locations)
    {
        Locations = locations;
    }

    internal void Freeze() => IsFrozen = true;

    private static void LoadBaseAssets(LoadLocations filter)
    {
        const string p = "assets";
        if (!Directory.Exists(p))
        {
            Directory.CreateDirectory(p);
        }

        foreach (DataAssetLoader loader in s_assetLoaders)
        {
            if (!loader.Locations.HasFlag(LoadLocations.Assets)) continue;
            if (!loader.LoadedAssetsModify.HasFlag(filter)) continue;

            loader.OnLoadAssets(p, false, LoadLocations.Assets);
        }
    }

    public static void LoadDatapackAssets(string? path) => LoadDatapackAssets(path, LoadLocations.None);

    private static void LoadDatapackAssets(string? path, LoadLocations filter)
    {
        s_lastDataPath = path;
        string p = path != null ? Path.Combine(path, "datapacks") : "datapacks";
        if (!Directory.Exists(p))
        {
            Directory.CreateDirectory(p);
        }

        foreach (string pack in Directory.EnumerateDirectories(p))
        {
            if (pack.EndsWith(".disabled")) continue;
            string assets = Path.Join(pack, "data");
            if (!Directory.Exists(assets)) continue;
            foreach (DataAssetLoader loader in s_assetLoaders)
            {
                if (!loader.Locations.HasFlag(LoadLocations.GameDatapack)) continue;
                if (!loader.LoadedAssetsModify.HasFlag(filter)) continue;

                loader.OnLoadAssets(assets, true, LoadLocations.GameDatapack);
            }
        }
    }

    private static void LoadWorldAssets(string path, LoadLocations filter)
    {
        s_lastWorldDataPath = path;
        string p = Path.Combine(path, "datapacks");
        if (!Directory.Exists(p))
        {
            Directory.CreateDirectory(p);
        }

        foreach (string pack in Directory.EnumerateDirectories(p))
        {
            if (pack.EndsWith(".disabled")) continue;
            string assets = Path.Join(pack, "data");
            if (!Directory.Exists(assets)) continue;

            foreach (DataAssetLoader loader in s_assetLoaders)
            {
                if (!loader.Locations.HasFlag(LoadLocations.WorldDatapack)) continue;
                if (!loader.LoadedAssetsModify.HasFlag(filter)) continue;

                loader.OnLoadAssets(assets, true, LoadLocations.WorldDatapack);
            }
        }
    }

    public static void LoadResourcepackAssets(string path) => LoadResourcepackAssets(path, LoadLocations.None);

    private static void LoadResourcepackAssets(string path, LoadLocations filter)
    {
        s_lastResourcePath = path;
        string p = Path.Combine(path, "resourcepacks");
        if (!Directory.Exists(p))
        {
            Directory.CreateDirectory(p);
        }

        foreach (string pack in Directory.EnumerateDirectories(p))
        {
            if (pack.EndsWith(".disabled")) continue;
            string assets = Path.Join(pack, "data");
            if (!Directory.Exists(assets)) continue;

            foreach (DataAssetLoader loader in s_assetLoaders)
            {
                if (!loader.Locations.HasFlag(LoadLocations.Resourcepack)) continue;
                if (!loader.LoadedAssetsModify.HasFlag(filter)) continue;

                loader.OnLoadAssets(assets, true, LoadLocations.Resourcepack);
            }
        }
    }

    public static void ResetResourcepackAssets(bool wait = false)
    {
        foreach (DataAssetLoader loader in s_assetLoaders)
        {
            if (!loader.Locations.HasFlag(LoadLocations.Resourcepack)) continue;
            if (!loader.LoadedAssetsModify.HasFlag(LoadLocations.Resourcepack)) continue;
            loader.Clear();
        }

        LoadBaseAssets(LoadLocations.Resourcepack);
        if (s_lastDataPath != null) LoadDatapackAssets(s_lastDataPath, LoadLocations.Resourcepack);
        if (s_lastWorldDataPath != null) LoadWorldAssets(s_lastWorldDataPath, LoadLocations.Resourcepack);
        if (s_lastResourcePath != null) LoadResourcepackAssets(s_lastResourcePath, LoadLocations.Resourcepack);

        if (wait)
        {
            foreach (DataAssetLoader loader in s_assetLoaders)
            {
                loader.Wait();
            }
        }
    }

    /// <summary>
    /// Runs the full load pipeline for this loader instance independently of the static
    /// <see cref="s_assetLoaders"/> list. Used by <see cref="Registries.RegistryAccess.Build"/>.
    /// </summary>
    internal void LoadFromPaths(string? basePath, string? datapackPath, string? worldDatapackPath)
    {
        if (IsFrozen)
            throw new InvalidOperationException("Cannot load into a frozen registry.");

        if (Locations.HasFlag(LoadLocations.Assets))
        {
            string assetsPath = basePath != null ? Path.Combine(basePath, "assets") : "assets";
            if (!Directory.Exists(assetsPath))
                Directory.CreateDirectory(assetsPath);
            OnLoadAssets(assetsPath, false, LoadLocations.Assets);
        }

        if (Locations.HasFlag(LoadLocations.GameDatapack) && datapackPath != null)
            LoadPacksFrom(datapackPath, LoadLocations.GameDatapack);

        if (Locations.HasFlag(LoadLocations.WorldDatapack) && worldDatapackPath != null)
            LoadPacksFrom(worldDatapackPath, LoadLocations.WorldDatapack);
    }

    private protected void LoadPacksFrom(string basePath, LoadLocations location)
    {
        if (IsFrozen)
            throw new InvalidOperationException("Cannot load into a frozen registry.");
        string packsDir = Path.Combine(basePath, "datapacks");
        if (!Directory.Exists(packsDir))
        {
            Directory.CreateDirectory(packsDir);
            return;
        }

        foreach (string pack in Directory.EnumerateDirectories(packsDir))
        {
            if (pack.EndsWith(".disabled")) continue;
            string assets = Path.Join(pack, "data");
            if (!Directory.Exists(assets)) continue;
            OnLoadAssets(assets, true, location);
        }
    }

    /// <summary>Blocks until any in-flight async load task completes.</summary>
    internal void WaitForLoad() => Wait();

    private protected abstract void OnLoadAssets(string path, bool namespaced, LoadLocations location);
    private protected abstract void Clear();
    private protected abstract void Wait();
}
