using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace BetaSharp.DataAsset;

public class AssetLoader<T> : AssetLoader where T : class, IAsset
{
    private readonly string _path;
    private Task? _loadTask = null;
    private readonly Dictionary<(Namespace Namespace, string Name), AssetRef<T>> _assets = new();

    public Dictionary<(Namespace Namespace, string Name), AssetRef<T>> Assets
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

    public static implicit operator Dictionary<(Namespace Namespace, string Name), AssetRef<T>>(AssetLoader<T> loader) => loader.Assets;

    public AssetLoader(string path, LoadLocations locations) : base(locations)
    {
        _path = path;
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
            var json = File.OpenRead(file);
            JsonElement obj = await JsonSerializer.DeserializeAsync<JsonElement>(json, s_jsonOptions);
            json.Close();

            if (obj.ValueKind == JsonValueKind.Array)
            {
                // Handle as list
                foreach (var item in obj.EnumerateArray())
                {
                    string? key = GetName(item, file);
                    if (key == null)
                    {
                        s_logger.LogError($"Asset missing name in file '{file}'");
                        continue;
                    }

                    if (_assets.TryGetValue((@namespace, key), out AssetRef<T>? assetRef))
                    {
                        LoadedAssetsModify |= location;
                        if (GetReplace(item))
                        {
                            FromJson(item, file, true, assetRef);
                            continue;
                        }
                    }

                    T? asset = FromJson(item, file, true);
                    if (asset == null)
                    {
                        s_logger.LogError($"Asset failed to load from file '{file}'");
                        continue;
                    }

                    asset.Namespace = @namespace;
                    _assets.Add((asset.Namespace, asset.Name), new AssetRef<T>(asset));
                }
            }
            else if (obj.ValueKind == JsonValueKind.Object)
            {
                string key = GetName(obj, file)!;
                if (_assets.TryGetValue((@namespace, key), out AssetRef<T>? assetRef))
                {
                    LoadedAssetsModify |= location;
                    if (GetReplace(obj))
                    {
                        FromJson(obj, file, true, assetRef);
                        continue;
                    }
                }

                T? asset = FromJson(obj, file, false);
                if (asset == null)
                {
                    s_logger.LogError($"Asset failed to load from file '{file}'");
                    continue;
                }

                asset.Name = key;
                asset.Namespace = @namespace;
                _assets.Add((asset.Namespace, asset.Name), new AssetRef<T>(asset));
            }
        }
    }

    private string? GetName(JsonElement json, string path)
    {
        if (json.TryGetProperty("Name", out var nameElement))
        {
            return nameElement.GetString();
        }

        if (path.Length != 0)
        {
            return Path.GetFileNameWithoutExtension(path);
        }

        return null;
    }

    private bool GetReplace(JsonElement json)
    {
        if (json.TryGetProperty("Replace", out var nameElement))
        {
            return nameElement.GetBoolean();
        }

        return false;
    }

    private T? FromJson(JsonElement json, string path, bool isList)
    {
        T? asset = json.Deserialize<T>(s_jsonOptions);
        if (asset == null) return null;

        // Use filename if name is not defined
        if (string.IsNullOrEmpty(asset.Name))
        {
            if (isList)
            {
                s_logger.LogError($"Asset missing name in file '{path}'");
                return null;
            }

            asset.Name = Path.GetFileNameWithoutExtension(path);
        }

        return asset;
    }

    private void FromJson(JsonElement json, string path, bool isList, AssetRef<T> target)
    {
        // Serialize the default value to JSON
        JsonElement defaultElement = JsonSerializer.SerializeToElement(target.Asset);

        // Merge the JSON with the default, preferring values from json
        JsonElement merged = MergeJson(defaultElement, json);

        T? asset = merged.Deserialize<T>(s_jsonOptions);
        if (asset == null) return;

        // Use filename if name is not defined
        if (string.IsNullOrEmpty(asset.Name))
        {
            if (isList)
            {
                s_logger.LogError($"Asset missing name in file '{path}'");
                return;
            }

            asset.Name = Path.GetFileNameWithoutExtension(path);
        }

        target.Asset = asset;
    }

    private static JsonElement MergeJson(JsonElement defaultObj, JsonElement overrideObj)
    {
        if (overrideObj.ValueKind != JsonValueKind.Object || defaultObj.ValueKind != JsonValueKind.Object)
        {
            return overrideObj;
        }

        var merged = new Dictionary<string, JsonElement>();

        // Add all properties from default
        foreach (var property in defaultObj.EnumerateObject())
        {
            merged[property.Name] = property.Value;
        }

        // Override with properties from the override object
        foreach (var property in overrideObj.EnumerateObject())
        {
            if (merged.TryGetValue(property.Name, out var defaultValue) &&
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

        foreach (var a in Assets)
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
                foreach (var a in Assets)
                {
                    if (a.Key.Name[0] != name[0]) continue;
                    asset = a.Value;
                    return true;
                }
            }
            else
            {
                foreach (var a in Assets)
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
        foreach (var a in Assets)
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
                foreach (var a in Assets)
                {
                    if (a.Key.Name[0] != name[0]) continue;
                    if (!a.Key.Namespace.Equals(ns)) continue;
                    asset = a.Value;
                    return true;
                }
            }
            else
            {
                foreach (var a in Assets)
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
