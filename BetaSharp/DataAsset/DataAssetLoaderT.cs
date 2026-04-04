using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace BetaSharp.DataAsset;

public class DataAssetLoader<T> : DataAssetLoader where T : class, IDataAsset
{
    private readonly string _path;
    private readonly bool _allowUnhandled;
    private Task? _loadTask = null;
    private readonly Dictionary<(Namespace Namespace, string Name), DataAssetRef<T>> _assets = [];

    public Dictionary<(Namespace Namespace, string Name), DataAssetRef<T>> Assets
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

    public static implicit operator Dictionary<(Namespace Namespace, string Name), DataAssetRef<T>>(DataAssetLoader<T> loader) => loader.Assets;

    public DataAssetLoader(string path, LoadLocations locations, bool allowUnhandled = true) : base(locations)
    {
        _path = path;
        _allowUnhandled = allowUnhandled;
    }

    private protected override void Clear() => Assets.Clear();

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
            if (_assets.TryGetValue((@namespace, key), out DataAssetRef<T>? assetRef))
            {
                if (GetReplace(obj))
                {
                    FromJsonReplace(obj, file, assetRef);
                    continue;
                }
                else
                {
                    FromJsonUpdate(obj, assetRef);
                    continue;
                }
            }
            else if (_allowUnhandled)
            {
                _assets.Add((@namespace, key), new DataAssetRef<T>(this, path, @namespace, key));
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
            _assets.Add((asset.Namespace, asset.Name), new DataAssetRef<T>(asset));
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

    private static void FromJsonUpdate(JsonElement json, DataAssetRef<T> target)
    {
        // Serialize the default value to JSON
        JsonElement defaultElement = JsonSerializer.SerializeToElement(target.Asset);

        // Merge the JSON with the default, preferring values from json
        JsonElement merged = MergeJson(defaultElement, json);

        T? asset = merged.Deserialize<T>(s_jsonOptions);
        if (asset == null)
        {
            s_logger.LogError($"Asset failed to deserialize into class '{target}'");
            return;
        }

        asset.Name = target.Name;
        target.Asset = asset;
    }

    internal static void FromJsonReplace(string path, DataAssetRef<T> target)
    {
        path = Path.Join(path, target.Name + ".json");
        using FileStream json = File.OpenRead(path);
        JsonElement obj = JsonSerializer.Deserialize<JsonElement>(json, s_jsonOptions);
        FromJsonReplace(obj, path, target);
    }

    private static void FromJsonReplace(JsonElement json, string path, DataAssetRef<T> target)
    {
        T? v = FromJson(json);
        if (v == null)
        {
            s_logger.LogError($"Asset failed to load from file '{path}'");
            return;
        }

        v.Name = target.Name;
        v.Namespace = target.Namespace;

        target.Asset = v;
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

        foreach (KeyValuePair<(Namespace Namespace, string Name), DataAssetRef<T>> a in Assets)
        {
            if (a.Key.Name != name) continue;

            asset = a.Value;
            return true;
        }

        if (shortName)
        {
            int nameLen = name.Length;
            if (nameLen == 1)
            {
                foreach (KeyValuePair<(Namespace Namespace, string Name), DataAssetRef<T>> a in Assets)
                {
                    if (a.Key.Name[0] != name[0]) continue;
                    asset = a.Value;
                    return true;
                }
            }
            else
            {
                foreach (KeyValuePair<(Namespace Namespace, string Name), DataAssetRef<T>> a in Assets)
                {
                    if (a.Key.Name.Length <= nameLen || a.Key.Name.Substring(0, nameLen) != name) continue;

                    asset = a.Value;
                    return true;
                }
            }
        }

        return false;
    }

    public bool TryGet(Namespace ns, string name, [NotNullWhen(true)] out T? asset, bool shortName = false)
    {
        foreach (KeyValuePair<(Namespace Namespace, string Name), DataAssetRef<T>> a in Assets)
        {
            if (!a.Key.Namespace.Equals(ns)) continue;
            if (a.Key.Name != name) continue;

            asset = a.Value;
            return true;
        }

        if (shortName)
        {
            int nameLen = name.Length;
            if (nameLen == 1)
            {
                foreach (KeyValuePair<(Namespace Namespace, string Name), DataAssetRef<T>> a in Assets)
                {
                    if (a.Key.Name[0] != name[0]) continue;
                    if (!a.Key.Namespace.Equals(ns)) continue;
                    asset = a.Value;
                    return true;
                }
            }
            else
            {
                foreach (KeyValuePair<(Namespace Namespace, string Name), DataAssetRef<T>> a in Assets)
                {
                    if (!a.Key.Namespace.Equals(ns)) continue;
                    if (a.Key.Name.Length <= nameLen || a.Key.Name.Substring(0, nameLen) != name) continue;

                    asset = a.Value;
                    return true;
                }
            }
        }

        asset = null;
        return false;
    }
}
