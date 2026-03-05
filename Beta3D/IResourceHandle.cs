namespace Beta3D;

public interface IResourceHandle<out T>
{
    static IResourceHandle<T> Invalid() => InvalidHandle<T>.Instance;
    T Value { get; }
}

sealed file class InvalidHandle<T> : IResourceHandle<T>
{
    public static readonly InvalidHandle<T> Instance = new();
    public T Value => throw new InvalidOperationException("Cannot dereference handle with no underlying resource");
}
