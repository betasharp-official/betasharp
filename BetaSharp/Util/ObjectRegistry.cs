using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;

namespace BetaSharp.Util;

public class ObjectRegistry<T>(int initialSize = 0) : ObjectRegistry<T, RegistryItem<T>>(initialSize) where T : class;

public class ObjectFactory<T1, T2>(int initialSize = 0) : ObjectRegistry<Func<T1>, T2>(initialSize) where T1 : class where T2 : FactoryItem<T1>
{
    public static ObjectFactory<T, FactoryItem<T>> New<T>(int initialSize = 0) where T : class => new(initialSize);
}

public class ObjectRegistry<T1, T2>(int initialSize = 0)
    where T1 : class
    where T2 : RegistryItem<T1>
{
    // ReSharper disable once StaticMemberInGenericType
    private static readonly ILogger s_logger = Log.Instance.For("ObjectRegistry");

    private T2?[] _registry = new T2?[initialSize];

    public int Count => _registry.Length;

    public T2? this[int index]
    {
        get => _registry[index];
        set => _registry[index] = value;
    }

    public bool ContainsKey(int id)
    {
        if (id < 0 || id >= _registry.Length) return false;
        return _registry[id] != null;
    }

    public bool TryGet(int id, [NotNullWhen(true)] out T2? item)
    {
        try
        {
            item = _registry[id];
            return item != null;
        }
        catch (Exception)
        {
            item = null;
            return false;
        }
    }

    public void Register(T2 item)
    {
        if (item.Id < 0)
        {
            throw new ArgumentException("Id cannot be negative:" + item.Id, nameof(item));
        }

        if (item.Id < _registry.Length)
        {
            if (_registry[item.Id] != null)
            {
                throw new ArgumentException("Duplicate id:" + item.Id, nameof(item));
            }

            _registry[item.Id] = item;
            return;
        }

        Resize(item.Id + 1);
        _registry[item.Id] = item;
    }

    public void Register(T2[] items)
    {
        // find largest index
        int idMax = -1;
        foreach (var item in items)
        {
            if (idMax < item.Id) idMax = item.Id;
        }

        Resize(idMax + 1);

        foreach (var item in items)
        {
            int id = item.Id;
            if (id < 0)
            {
                var e = new ArgumentException("Id cannot be negative:" + id, nameof(items));
                s_logger.LogError(e, e.Message);
                continue;
            }

            if (_registry[id] != null)
            {
                var e = new ArgumentException("Duplicate id:" + id, nameof(items));
                s_logger.LogError(e, e.Message);
                continue;
            }

            _registry[id] = item;
        }
    }

    public void Resize(int size)
    {
        if (_registry.Length < size)
        {
            T2?[] newItems = new T2?[size];
            if (_registry.Length > 0)
            {
                Array.Copy(_registry, newItems, _registry.Length);
            }

            _registry = newItems;
        }
    }
}

public static class ObjectRegistryExtensions
{
    extension<T>(ObjectRegistry<T, RegistryItem<T>> registry) where T : class
    {
        public void Register(int id, T obj)
        {
            registry.Resize(id + 1);

            if (id < 0)
            {
                throw new ArgumentException("Id cannot be negative:" + id, nameof(id));
            }

            if (registry[id] != null)
            {
                throw new ArgumentException("Duplicate id:" + id, nameof(id));
            }

            registry[id] = new RegistryItem<T>(id, obj);
        }

        public int Register(T obj)
        {
            int i = 0;
            for (int l = registry.Count; i < l; i++)
            {
                if (registry[i] == null)
                {
                    registry[i] = new RegistryItem<T>(i, obj);
                    return i;
                }
            }

            registry.Register(new RegistryItem<T>(i, obj));
            return i;
        }
    }
}

public class RegistryItem<T>(int id, T item)
{
    public readonly int Id = id;
    public readonly T Item = item;
}

public class FactoryItem<T>(int id, Func<T> item) : RegistryItem<Func<T>>(id, item)
{
    public virtual T New() => Item.Invoke();
}
