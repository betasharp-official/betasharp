using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using BetaSharp.Registries;
using Microsoft.Extensions.Logging;

namespace BetaSharp.DataAsset;

public class DataAssetLoader<T> : DataAssetLoader, IReadableRegistry<T> where T : class, IDataAsset
{
    private readonly string _path;
    private readonly bool _allowUnhandled;
    private Task? _loadTask = null;
    private readonly Dictionary<ResourceLocation, Holder<T>> _assets = [];

    public Dictionary<ResourceLocation, Holder<T>> Assets
    {
        get
        {
            if (_loadTask != null) Wait();
            return _assets;
        }
    }

    private protected override void Wait()
    {
        if (_loadTask == null) return;

        if (_loadTask.IsFaulted)
        {
            throw _loadTask.Exception;
        }

        if (_loadTask.IsCompleted)
        {
            _loadTask = null;
        }
        else
        {
            _loadTask.Wait();
            if (_loadTask.IsFaulted)
            {
                throw _loadTask.Exception;
            }

            _loadTask = null;
        }
    }

    public static implicit operator Dictionary<ResourceLocation, Holder<T>>(DataAssetLoader<T> loader) => loader.Assets;

    public DataAssetLoader(string path, LoadLocations locations, bool allowUnhandled = true) : base(locations)
    {
        _path = path;
        _allowUnhandled = allowUnhandled;
    }

    private protected override void Clear() => Assets.Clear();

    /// <summary>
    /// Creates a copy of this loader with all currently-loaded assets, then applies
    /// <paramref name="worldDatapackPath"/> on top. The original loader is unaffected.
    /// </summary>
    internal DataAssetLoader<T> CloneForWorldDatapacks(string worldDatapackPath)
    {
        var clone = new DataAssetLoader<T>(_path, Locations, _allowUnhandled);
        foreach (KeyValuePair<ResourceLocation, Holder<T>> pair in Assets)
        {
            // Create an independent holder so world-datapack mutations cannot
            // corrupt the server-level registry that owns the original holders.
            Holder<T> original = pair.Value;
            clone._assets[pair.Key] = original.IsResolved
                ? new Holder<T>(original.Value)
                : Holder<T>.Reference(() => original.Value);
        }
        clone.LoadPacksFrom(worldDatapackPath, LoadLocations.WorldDatapack);
        clone.WaitForLoad();
        return clone;
    }

    private protected override void OnLoadAssets(string path, bool namespaced, LoadLocations location)
    {
        // Complete pending loading before the next one.
        Wait();
        _loadTask = namespaced ? LoadAssetsFromFolders(path, location) : LoadAssets(Namespace.BetaSharp, path, location);
    }

    private async Task LoadAssetsFromFolders(string path, LoadLocations location)
    {
        foreach (string dir in Directory.GetDirectories(path, "*", SearchOption.TopDirectoryOnly))
        {
            string dirName = Path.GetFileName(dir);
            await LoadAssets(Namespace.Get(dirName), dir, location);
        }
    }

    private async Task LoadAssets(Namespace @namespace, string path, LoadLocations location)
    {
        path = Path.Join(path, _path);
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }

        foreach (string file in Directory.EnumerateFiles(path, "*.json"))
        {
            await using FileStream json = File.OpenRead(file);
            JsonElement obj = await JsonSerializer.DeserializeAsync<JsonElement>(json, s_jsonOptions);

            if (obj.ValueKind != JsonValueKind.Object)
            {
                s_logger.LogError($"Unexpected Json format in file '{file}'. Expected Object, found {obj.ValueKind}.");
                continue;
            }

            LoadedAssetsModify |= location;

            string key = Path.GetFileNameWithoutExtension(file);
            var id = new ResourceLocation(@namespace, key);

            if (_assets.TryGetValue(id, out Holder<T>? assetRef))
            {
                if (GetReplace(obj))
                {
                    ReplaceHolder(obj, file, id, assetRef);
                    continue;
                }
                else
                {
                    UpdateHolder(obj, assetRef);
                    continue;
                }
            }
            else if (_allowUnhandled)
            {
                _assets.Add(id, CreateLazyHolder(path, id));
                continue;
            }

            T? asset = FromJson(obj);
            if (asset == null)
            {
                s_logger.LogError($"Asset failed to load from file '{file}'");
                continue;
            }

            asset.Name = key;
            asset.Namespace = @namespace;
            _assets.Add(id, new Holder<T>(asset));
        }
    }

    private static bool GetReplace(JsonElement json)
    {
        if (json.TryGetProperty("Replace", out JsonElement nameElement))
        {
            return nameElement.GetBoolean();
        }

        return false;
    }

    private static T? FromJson(JsonElement json)
    {
        T? asset = json.Deserialize<T>(s_jsonOptions);
        if (asset == null) return null;
        return asset;
    }

    private static void UpdateHolder(JsonElement json, Holder<T> target)
    {
        // Serialize the default value to JSON
        JsonElement defaultElement = JsonSerializer.SerializeToElement(target.Value);

        // Merge the JSON with the default, preferring values from json
        JsonElement merged = MergeJson(defaultElement, json);

        T? asset = merged.Deserialize<T>(s_jsonOptions);
        if (asset == null)
        {
            s_logger.LogError($"Asset failed to deserialize into class '{target}'");
            return;
        }

        asset.Name = target.Value.Name;
        target.Value = asset;
    }

    /// <summary>
    /// Creates a lazy <see cref="Holder{T}"/> that loads its asset from
    /// <paramref name="dirPath"/>/<paramref name="id"/>.json on first access.
    /// </summary>
    internal static Holder<T> CreateLazyHolder(string dirPath, ResourceLocation id)
    {
        return Holder<T>.Reference(() =>
        {
            string filePath = Path.Join(dirPath, id.Path + ".json");
            using FileStream json = File.OpenRead(filePath);
            JsonElement obj = JsonSerializer.Deserialize<JsonElement>(json, s_jsonOptions);
            T? asset = FromJson(obj);
            if (asset == null)
                throw new InvalidOperationException($"Asset '{id}' failed to load from '{filePath}'.");
            asset.Name = id.Path;
            asset.Namespace = id.Namespace;
            return asset;
        });
    }

    private static void ReplaceHolder(JsonElement json, string path, ResourceLocation id, Holder<T> target)
    {
        T? v = FromJson(json);
        if (v == null)
        {
            s_logger.LogError($"Asset failed to load from file '{path}'");
            return;
        }

        v.Name = id.Path;
        v.Namespace = id.Namespace;
        target.Value = v;
    }

    private static JsonElement MergeJson(JsonElement defaultObj, JsonElement overrideObj)
    {
        if (overrideObj.ValueKind != JsonValueKind.Object || defaultObj.ValueKind != JsonValueKind.Object)
        {
            return overrideObj;
        }

        var merged = new Dictionary<string, JsonElement>();

        // Add all properties from default
        foreach (JsonProperty property in defaultObj.EnumerateObject())
        {
            merged[property.Name] = property.Value;
        }

        // Override with properties from the override object
        foreach (JsonProperty property in overrideObj.EnumerateObject())
        {
            if (merged.TryGetValue(property.Name, out JsonElement defaultValue) &&
                property.Value.ValueKind == JsonValueKind.Object &&
                defaultValue.ValueKind == JsonValueKind.Object)
            {
                // Recursively merge nested objects
                merged[property.Name] = MergeJson(defaultValue, property.Value);
            }
            else
            {
                merged[property.Name] = property.Value;
            }
        }

        return JsonSerializer.SerializeToElement(merged, s_jsonOptions);
    }

    public bool TryGet(string name, [NotNullWhen(true)] out T? asset, bool shortName = false)
    {
        asset = null;
        int split = name.IndexOf(':');

        if (split != -1)
        {
            string namespaceName = name.Substring(0, split);
            name = name.Substring(split + 1);

            Namespace? ns = Namespace.FindIndex(namespaceName.ToLower(), shortName);
            if (ns == null) return false;

            return TryGet(ns, name, out asset, shortName);
        }

        foreach (KeyValuePair<ResourceLocation, Holder<T>> a in Assets)
        {
            if (a.Key.Path != name) continue;

            asset = a.Value;
            return true;
        }

        if (shortName)
        {
            int nameLen = name.Length;
            if (nameLen == 1)
            {
                foreach (KeyValuePair<ResourceLocation, Holder<T>> a in Assets)
                {
                    if (a.Key.Path[0] != name[0]) continue;
                    asset = a.Value;
                    return true;
                }
            }
            else
            {
                foreach (KeyValuePair<ResourceLocation, Holder<T>> a in Assets)
                {
                    if (a.Key.Path.Length <= nameLen || a.Key.Path.Substring(0, nameLen) != name) continue;

                    asset = a.Value;
                    return true;
                }
            }
        }

        return false;
    }

    public bool TryGet(Namespace ns, string name, [NotNullWhen(true)] out T? asset, bool shortName = false)
    {
        if (!shortName)
        {
            var key = new ResourceLocation(ns, name);
            if (_assets.TryGetValue(key, out Holder<T>? holder))
            {
                asset = holder;
                return true;
            }

            asset = null;
            return false;
        }

        foreach (KeyValuePair<ResourceLocation, Holder<T>> a in Assets)
        {
            if (!a.Key.Namespace.Equals(ns)) continue;
            if (a.Key.Path != name) continue;

            asset = a.Value;
            return true;
        }

        int nameLen = name.Length;
        if (nameLen == 1)
        {
            foreach (KeyValuePair<ResourceLocation, Holder<T>> a in Assets)
            {
                if (a.Key.Path[0] != name[0]) continue;
                if (!a.Key.Namespace.Equals(ns)) continue;
                asset = a.Value;
                return true;
            }
        }
        else
        {
            foreach (KeyValuePair<ResourceLocation, Holder<T>> a in Assets)
            {
                if (!a.Key.Namespace.Equals(ns)) continue;
                if (a.Key.Path.Length <= nameLen || a.Key.Path.Substring(0, nameLen) != name) continue;

                asset = a.Value;
                return true;
            }
        }

        asset = null;
        return false;
    }

    // ---- IReadableRegistry<T> implementation ----

    public ResourceLocation RegistryKey => new(Namespace.BetaSharp, _path);

    T? IReadableRegistry<T>.Get(ResourceLocation key)
    {
        if (_assets.TryGetValue(key, out Holder<T>? holder)) return holder.Value;
        return null;
    }

    T? IReadableRegistry<T>.Get(int id) => null;

    int IReadableRegistry<T>.GetId(T value) => -1;

    ResourceLocation? IReadableRegistry<T>.GetKey(T value)
    {
        foreach (KeyValuePair<ResourceLocation, Holder<T>> pair in Assets)
        {
            if (pair.Value.IsResolved && ReferenceEquals(pair.Value.Value, value))
                return pair.Key;
        }
        return null;
    }

    bool IReadableRegistry<T>.ContainsKey(ResourceLocation key) => Assets.ContainsKey(key);

    IEnumerable<ResourceLocation> IReadableRegistry<T>.Keys => Assets.Keys;

    Holder<T>? IReadableRegistry<T>.GetHolder(ResourceLocation key)
    {
        Assets.TryGetValue(key, out Holder<T>? holder);
        return holder;
    }

    public IEnumerator<T> GetEnumerator()
    {
        foreach (Holder<T> h in Assets.Values)
        {
            yield return h.Value;
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
