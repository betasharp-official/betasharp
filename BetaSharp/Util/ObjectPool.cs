using System.Collections.Concurrent;

namespace BetaSharp.Util;

public class ObjectPool<T> : IDisposable where T : class
{
    private readonly Func<T> factory;
    private readonly ConcurrentBag<T> pool;
    private readonly int capacity;

    public ObjectPool(Func<T> factory, int capacity = 32)
    {
        this.factory = factory;
        this.capacity = capacity;
        pool = new ConcurrentBag<T>();
    }

    public T Get() => pool.TryTake(out T? item) ? item : factory();

    public void Return(T obj)
    {
        if (pool.Count < capacity)
        {
            pool.Add(obj);
        }
        else if (obj is IDisposable d)
        {
            d.Dispose();
        }
    }

    public void Dispose()
    {
        while (pool.TryTake(out T? obj))
        {
            if (obj is IDisposable d) d.Dispose();
        }
    }
}
