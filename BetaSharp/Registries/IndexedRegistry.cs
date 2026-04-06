using System.Collections;

namespace BetaSharp.Registries;

public class IndexedRegistry<T>(ResourceLocation registryKey) : IRegistry<T> where T : class
{
    private readonly List<T?> _byId = new(256);
    private readonly Dictionary<ResourceLocation, T> _byLocation = [];
    private readonly Dictionary<T, int> _toId = [];
    private readonly Dictionary<T, ResourceLocation> _toLocation = [];
    private Dictionary<ResourceLocation, Holder<T>>? _holderCache;

    public ResourceLocation RegistryKey { get; } = registryKey;
    public bool IsFrozen { get; private set; }

    public void Register(ResourceLocation key, T value)
    {
        Register(-1, key, value);
    }

    public void Register(int id, ResourceLocation key, T value)
    {
        if (IsFrozen)
        {
            throw new InvalidOperationException($"Registry {RegistryKey} is frozen.");
        }

        if (_byLocation.ContainsKey(key))
        {
            throw new ArgumentException($"Duplicate key: {key}", nameof(key));
        }

        if (_toId.ContainsKey(value))
        {
            throw new ArgumentException("Duplicate value.", nameof(value));
        }

        if (id < 0)
        {
            id = _byId.Count;
        }

        while (_byId.Count <= id)
        {
            _byId.Add(null);
        }

        if (_byId[id] != null)
        {
            throw new ArgumentException($"ID {id} is already occupied by {_toLocation[_byId[id]!]}", nameof(id));
        }

        _byId[id] = value;
        _byLocation[key] = value;
        _toId[value] = id;
        _toLocation[value] = key;
    }

    public T? Get(int id)
    {
        if (id < 0 || id >= _byId.Count) return null;
        return _byId[id];
    }

    public T? Get(ResourceLocation key)
    {
        _byLocation.TryGetValue(key, out T? value);
        return value;
    }

    public int GetId(T value)
    {
        return _toId.TryGetValue(value, out int id) ? id : -1;
    }

    public ResourceLocation? GetKey(T value)
    {
        _toLocation.TryGetValue(value, out ResourceLocation? key);
        return key;
    }

    public bool ContainsKey(ResourceLocation key) => _byLocation.ContainsKey(key);
    public bool ContainsId(int id) => id >= 0 && id < _byId.Count && _byId[id] != null;

    public IEnumerable<ResourceLocation> Keys => _byLocation.Keys;

    public Holder<T>? GetHolder(ResourceLocation key)
    {
        T? value = Get(key);
        if (value == null) return null;

        _holderCache ??= [];
        if (!_holderCache.TryGetValue(key, out Holder<T>? holder))
        {
            holder = new(value);
            _holderCache[key] = holder;
        }

        return holder;
    }

    public void Freeze()
    {
        IsFrozen = true;
    }

    public IEnumerator<T> GetEnumerator()
    {
        return _byId.Where(x => x != null).GetEnumerator()!;
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
