using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace BetaSharp.DataAsset;

public abstract class DataAssetLoader
{
    private protected static readonly ILogger s_logger = Log.Instance.For(typeof(DataAssetLoader).FullName ?? nameof(DataAssetLoader));

    private static readonly List<DataAssetLoader> s_assetLoaders =
    [
        GameMode.GameModes.GameModesLoader
    ];

    private protected static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenReading
    };

    private protected readonly LoadLocations Locations;

    private protected LoadLocations LoadedAssetsModify;

    private static string? s_lastDataPath = null;
    private static string? s_lastWorldDataPath = null;
    private static string? s_lastResourcePath = null;

    private protected DataAssetLoader(LoadLocations locations)
    {
        //s_assetLoaders.Add(this);
        Locations = locations;
    }

    public static void LoadBaseAssets() => LoadBaseAssets(LoadLocations.None);

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

    public static void LoadWorldAssets(string path) => LoadWorldAssets(path, LoadLocations.None);

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

    public static void UnloadWorldAssets(bool wait = false)
    {
        if (s_lastWorldDataPath == null) return;
        s_lastWorldDataPath = null;

        foreach (DataAssetLoader loader in s_assetLoaders)
        {
            if (!loader.Locations.HasFlag(LoadLocations.WorldDatapack)) continue;
            if (!loader.LoadedAssetsModify.HasFlag(LoadLocations.WorldDatapack)) continue;
            loader.Clear();
        }

        LoadBaseAssets(LoadLocations.WorldDatapack);
        if (s_lastDataPath != null) LoadDatapackAssets(s_lastDataPath, LoadLocations.WorldDatapack);
        if (s_lastResourcePath != null) LoadResourcepackAssets(s_lastResourcePath, LoadLocations.WorldDatapack);

        if (wait)
        {
            foreach (DataAssetLoader loader in s_assetLoaders)
            {
                loader.Wait();
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

    private protected abstract void OnLoadAssets(string path, bool namespaced, LoadLocations location);
    private protected abstract void Clear();
    private protected abstract void Wait();
}
