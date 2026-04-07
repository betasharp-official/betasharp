using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace BetaSharp.Registries.Data;

public class DataAssetLoader<T> : DataAssetLoader, IReadableRegistry<T> where T : class, IDataAsset
{
    private readonly string _path;
    private readonly bool _allowUnhandled;

    public Dictionary<ResourceLocation, Holder<T>> Assets { get; } = [];

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
    internal DataAssetLoader<T>? CloneForWorldDatapacks(string worldDatapackPath)
    {
        if (!Locations.HasFlag(LoadLocations.WorldDatapack)) return null;
        var clone = new DataAssetLoader<T>(_path, Locations, _allowUnhandled);
        foreach (KeyValuePair<ResourceLocation, Holder<T>> pair in Assets)
        {
            // Create an independent holder so world-datapack mutations cannot
            // corrupt the server-level registry that owns the original holders.
            Holder<T> original = pair.Value;
            clone.Assets[pair.Key] = original.IsResolved
                ? new Holder<T>(original.Value)
                : Holder<T>.Reference(() => original.Value);
        }
        clone.LoadPacksFrom(worldDatapackPath, LoadLocations.WorldDatapack);
        return clone;
    }

    private protected override void OnLoadAssets(string path, bool namespaced, LoadLocations location)
    {
        if (namespaced) LoadAssetsFromFolders(path, location);
        else LoadAssets(Namespace.BetaSharp, path, location);
    }

    private void LoadAssetsFromFolders(string path, LoadLocations location)
    {
        foreach (string dir in Directory.GetDirectories(path, "*", SearchOption.TopDirectoryOnly))
        {
            string dirName = Path.GetFileName(dir);
            LoadAssets(Namespace.Get(dirName), dir, location);
        }
    }

    private void LoadAssets(Namespace @namespace, string path, LoadLocations location)
    {
        path = Path.Join(path, _path);
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }

        foreach (string file in Directory.EnumerateFiles(path, "*.json"))
        {
            try
            {
                var id = new ResourceLocation(@namespace, Path.GetFileNameWithoutExtension(file));

                if (Assets.TryGetValue(id, out Holder<T>? assetRef))
                {
                    using FileStream json = File.OpenRead(file);
                    JsonElement obj = JsonSerializer.Deserialize<JsonElement>(json, s_jsonOptions);

                    if (obj.ValueKind != JsonValueKind.Object)
                    {
                        s_logger.LogError($"Unexpected Json format in file '{file}'. Expected Object, found {obj.ValueKind}.");
                        HasErrors = true;
                        continue;
                    }

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
                    Assets.Add(id, CreateLazyHolder(File.ReadAllText(file), id));
                    continue;
                }

                T? asset = FromPath(file);
                if (asset == null)
                {
                    s_logger.LogError($"Asset failed to load from file '{file}'");
                    HasErrors = true;
                    continue;
                }

                asset.Name = id.Path;
                asset.Namespace = id.Namespace;
                Assets.Add(id, new Holder<T>(asset));
            }
            catch (JsonException ex)
            {
                string msg = $"Syntax error in '{file}' at line {ex.LineNumber}, pos {ex.BytePositionInLine}: {ex.Message}";
                HasErrors = true;
                FirstErrorMessage ??= msg;
            }
            catch (Exception)
            {
                HasErrors = true;
                FirstErrorMessage ??= $"Unexpected error in {Path.GetFileName(file)}";
            }
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

    private static T? FromPath(string path)
    {
        using FileStream json = File.OpenRead(path);
        return JsonSerializer.Deserialize<T>(json, s_jsonOptions);
    }

    private static T? FromJson(JsonElement json) => json.Deserialize<T>(s_jsonOptions);

    private void UpdateHolder(JsonElement json, Holder<T> target)
    {
        try
        {
            // Serialize the default value to JSON
            JsonElement defaultElement = JsonSerializer.SerializeToElement(target.Value);

            // Merge the JSON with the default, preferring values from json
            JsonElement merged = MergeJson(defaultElement, json);

            T? asset = merged.Deserialize<T>(s_jsonOptions);
            if (asset == null)
            {
                s_logger.LogError($"Asset failed to deserialize into class '{target}'");
                HasErrors = true;
                return;
            }

            asset.Name = target.Value.Name;
            target.Value = asset;
        }
        catch (JsonException ex)
        {
            string msg = $"Syntax error updating '{target}' at line {ex.LineNumber}, pos {ex.BytePositionInLine}: {ex.Message}";
            HasErrors = true;
            FirstErrorMessage ??= msg;
        }
        catch (Exception)
        {
            HasErrors = true;
            FirstErrorMessage ??= $"Unexpected error updating {target}";
        }
    }

    /// <summary>
    /// Creates a lazy <see cref="Holder{T}"/> that parses <paramref name="jsonContent"/> on
    /// first access. The raw JSON string is captured at load time so no file I/O occurs at
    /// resolve time.
    /// </summary>
    internal static Holder<T> CreateLazyHolder(string jsonContent, ResourceLocation id)
    {
        return Holder<T>.Reference(() =>
        {
            try
            {
                T asset = JsonSerializer.Deserialize<T>(jsonContent, s_jsonOptions)
                    ?? throw new InvalidOperationException($"Asset '{id}' deserialized to null.");
                asset.Name = id.Path;
                asset.Namespace = id.Namespace;
                return asset;
            }
            catch (JsonException ex)
            {
                string msg = $"Syntax error in lazy-loaded JSON for '{id}' at line {ex.LineNumber}, pos {ex.BytePositionInLine}: {ex.Message}";
                throw new InvalidOperationException(msg, ex);
            }
            catch (Exception ex) when (ex is not InvalidOperationException)
            {
                string msg = $"Unexpected error parsing lazy-loaded JSON for '{id}': {ex.Message}";
                throw new InvalidOperationException(msg, ex);
            }
        });
    }

    private void ReplaceHolder(JsonElement json, string path, ResourceLocation id, Holder<T> target)
    {
        try
        {
            T? v = FromJson(json);
            if (v == null)
            {
                s_logger.LogError($"Asset failed to load from file '{path}'");
                HasErrors = true;
                return;
            }

            v.Name = id.Path;
            v.Namespace = id.Namespace;
            target.Value = v;
        }
        catch (JsonException ex)
        {
            string msg = $"Syntax error in '{path}' at line {ex.LineNumber}, pos {ex.BytePositionInLine}: {ex.Message}";
            HasErrors = true;
            FirstErrorMessage ??= msg;
        }
        catch (Exception)
        {
            HasErrors = true;
            FirstErrorMessage ??= $"Unexpected error in {Path.GetFileName(path)}";
        }
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

    /// <summary>
    /// Looks up an entry by name. If <paramref name="name"/> contains a <c>:</c> it is
    /// treated as <c>namespace:path</c>; otherwise all namespaces are searched by path.
    /// </summary>
    public bool TryGet(string name, [NotNullWhen(true)] out T? asset)
    {
        if (TryGetHolder(name, out var holder))
        {
            asset = holder.Value;
            return true;
        }

        asset = null;
        return false;
    }

    public bool TryGetByPrefix(string prefix, [NotNullWhen(true)] out T? asset)
    {
        if (TryGetHolderByPrefix(prefix, out var holder))
        {
            asset = holder.Value;
            return true;
        }

        asset = null;
        return false;
    }

    public bool TryGetHolder(string name, [NotNullWhen(true)] out Holder<T>? holder)
        => TryGetHolderInternal(name, out holder, prefix: false);

    public bool TryGetHolderByPrefix(string prefix, [NotNullWhen(true)] out Holder<T>? holder)
        => TryGetHolderInternal(prefix, out holder, prefix: true);

    private bool TryGetHolderInternal(string name, [NotNullWhen(true)] out Holder<T>? holder, bool prefix)
    {
        holder = null;
        int split = name.IndexOf(':');

        if (split != -1)
        {
            string namespaceName = name.Substring(0, split);
            name = name.Substring(split + 1);

            List<Namespace> nss = Namespace.FindNamespaces(namespaceName.ToLower(), prefix);

            if (nss.Count == 0) return false;

            foreach (Namespace ns in nss)
            {
                if (TryGetHolderInNamespace(ns, name, out holder, prefix)) return true;
            }
            return false;
        }

        foreach (KeyValuePair<ResourceLocation, Holder<T>> a in Assets)
        {
            if (a.Key.Path != name) continue;

            holder = a.Value;
            return true;
        }

        if (prefix)
        {
            int nameLen = name.Length;
            if (nameLen == 1)
            {
                foreach (KeyValuePair<ResourceLocation, Holder<T>> a in Assets)
                {
                    if (a.Key.Path[0] != name[0]) continue;
                    holder = a.Value;
                    return true;
                }
            }
            else
            {
                foreach (KeyValuePair<ResourceLocation, Holder<T>> a in Assets)
                {
                    if (a.Key.Path.Length <= nameLen || a.Key.Path.Substring(0, nameLen) != name) continue;

                    holder = a.Value;
                    return true;
                }
            }
        }

        return false;
    }

    private bool TryGetHolderInNamespace(Namespace ns, string name, [NotNullWhen(true)] out Holder<T>? holder, bool prefix)
    {
        if (!prefix)
        {
            var key = new ResourceLocation(ns, name);
            if (Assets.TryGetValue(key, out holder))
            {
                return true;
            }

            holder = null;
            return false;
        }

        foreach (KeyValuePair<ResourceLocation, Holder<T>> a in Assets)
        {
            if (!a.Key.Namespace.Equals(ns)) continue;
            if (a.Key.Path != name) continue;

            holder = a.Value;
            return true;
        }

        int nameLen = name.Length;
        if (nameLen == 1)
        {
            foreach (KeyValuePair<ResourceLocation, Holder<T>> a in Assets)
            {
                if (a.Key.Path[0] != name[0]) continue;
                if (!a.Key.Namespace.Equals(ns)) continue;
                holder = a.Value;
                return true;
            }
        }
        else
        {
            foreach (KeyValuePair<ResourceLocation, Holder<T>> a in Assets)
            {
                if (!a.Key.Namespace.Equals(ns)) continue;
                if (a.Key.Path.Length <= nameLen || a.Key.Path.Substring(0, nameLen) != name) continue;

                holder = a.Value;
                return true;
            }
        }

        holder = null;
        return false;
    }

    // ---- IReadableRegistry<T> implementation ----

    public ResourceLocation RegistryKey => new(Namespace.BetaSharp, _path);

    Holder<T>? IReadableRegistry<T>.Get(ResourceLocation key)
    {
        Assets.TryGetValue(key, out Holder<T>? holder);
        return holder;
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

    public IEnumerator<T> GetEnumerator()
    {
        foreach (Holder<T> h in Assets.Values)
        {
            yield return h.Value;
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
