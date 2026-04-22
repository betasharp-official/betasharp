using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace BetaSharp.Registries.Data;

public abstract class DataAssetLoader
{
    private protected static readonly ILogger s_logger = Log.Instance.For(typeof(DataAssetLoader).FullName ?? nameof(DataAssetLoader));

    private protected static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenReading
    };

    private protected readonly LoadLocations Locations;

    public bool IsFrozen { get; private set; }
    public bool HasErrors { get; protected set; }
    public string? FirstErrorMessage { get; protected set; }

    private protected DataAssetLoader(LoadLocations locations)
    {
        Locations = locations;
    }

    internal void Freeze() => IsFrozen = true;

    /// <summary>
    /// Runs the full load pipeline for this loader instance. Used by <see cref="RegistryAccess.Build"/>.
    /// </summary>
    internal void LoadFromPaths(string? basePath, string? datapackPath, string? worldDatapackPath)
    {
        if (IsFrozen)
        {
            throw new InvalidOperationException("Cannot load into a frozen registry.");
        }

        if (Locations.HasFlag(LoadLocations.Assets))
        {
            string assetsPath = Path.Combine(basePath ?? AppContext.BaseDirectory, "assets");
            if (!Directory.Exists(assetsPath))
                Directory.CreateDirectory(assetsPath);
            OnLoadAssets(assetsPath, false, LoadLocations.Assets);
        }

        if (Locations.HasFlag(LoadLocations.GameDatapack) && datapackPath != null)
        {
            LoadPacksFrom(datapackPath, LoadLocations.GameDatapack);
        }

        if (Locations.HasFlag(LoadLocations.WorldDatapack) && worldDatapackPath != null)
        {
            LoadPacksFrom(worldDatapackPath, LoadLocations.WorldDatapack);
        }
    }

    private protected void LoadPacksFrom(string basePath, LoadLocations location)
    {
        if (IsFrozen)
        {
            throw new InvalidOperationException("Cannot load into a frozen registry.");
        }

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

    private protected abstract void OnLoadAssets(string path, bool namespaced, LoadLocations location);
    private protected abstract void Clear();
}
