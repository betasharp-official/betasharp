using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace BetaSharp.DataAsset;

public class AssetLoader<T> : AssetLoader where T : class, IAsset
{
    private string _path;
    public SortedDictionary<(int NamespaceId, string Name), AssetRef<T>> Assets { get; } = new();
    public static implicit operator SortedDictionary<(int NamespaceId, string Name), AssetRef<T>>(AssetLoader<T> loader) => loader.Assets;

    public AssetLoader(string path, LoadLocations locations) : base(locations)
    {
        _path = path;
    }

    private protected override void Clear() => Assets.Clear();

    private protected override async Task OnLoadAssets(string path, bool namespaced, LoadLocations location)
    {
        if (namespaced)
        {
            foreach (string dir in Directory.GetDirectories(path, "*", SearchOption.TopDirectoryOnly))
            {
                string dirName = Path.GetFileName(dir);
                await LoadAssets(AssetNamespace.GetIndex(dirName), dir, location);
            }
        }
        else
        {
            await LoadAssets(0, path, location);
        }
    }

    private async Task LoadAssets(int namespaceId, string path, LoadLocations location)
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


                    if (Assets.TryGetValue((namespaceId, key), out AssetRef<T>? assetRef))
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

                    asset.NamespaceId = namespaceId;
                    Assets.Add((asset.NamespaceId, asset.Name), new AssetRef<T>(asset));
                }
            }
            else if (obj.ValueKind == JsonValueKind.Object)
            {
                string key = GetName(obj, file)!;
                if (Assets.TryGetValue((namespaceId, key), out AssetRef<T>? assetRef))
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
                asset.NamespaceId = namespaceId;
                Assets.Add((asset.NamespaceId, asset.Name), new AssetRef<T>(asset));
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

            int namespaceId = AssetNamespace.FindIndex(namespaceName, shortName);
            if (namespaceId == -1) return false;

            return TryGet(namespaceId, name, out asset, shortName);
        }

        foreach (var gm in Assets)
        {
            if (gm.Key.Name != name) continue;

            asset = gm.Value;
            return true;
        }

        if (shortName)
        {
            int nameLen = name.Length;
            if (nameLen == 1)
            {
                foreach (var gm in Assets)
                {
                    if (gm.Key.Name[0] != name[0]) continue;
                    asset = gm.Value;
                    return true;
                }
            }
            else
            {
                foreach (var gm in Assets)
                {
                    if (gm.Key.Name.Length <= nameLen || gm.Key.Name.Substring(0, nameLen) != name) continue;

                    asset = gm.Value;
                    return true;
                }
            }
        }

        return false;
    }

    public bool TryGet(int namespaceId, string name, [NotNullWhen(true)] out T? asset, bool shortName = false)
    {
        foreach (var gm in Assets)
        {
            if (gm.Key.NamespaceId != namespaceId) continue;
            if (gm.Key.Name != name) continue;

            asset = gm.Value;
            return true;
        }

        if (shortName)
        {
            int nameLen = name.Length;
            if (nameLen == 1)
            {
                foreach (var gm in Assets)
                {
                    if (gm.Key.Name[0] != name[0]) continue;
                    if (gm.Key.NamespaceId != namespaceId) continue;
                    asset = gm.Value;
                    return true;
                }
            }
            else
            {
                foreach (var gm in Assets)
                {
                    if (gm.Key.NamespaceId != namespaceId) continue;
                    if (gm.Key.Name.Length <= nameLen || gm.Key.Name.Substring(0, nameLen) != name) continue;

                    asset = gm.Value;
                    return true;
                }
            }
        }

        asset = null;
        return false;
    }
}
