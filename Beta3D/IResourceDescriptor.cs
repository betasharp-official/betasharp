namespace Beta3D;

public interface IResourceDescriptor<T>
{
    T Allocate();
    void Prepare(T resource) { }
    void Free(T resource);
    bool CanUsePhysicalResource(IResourceDescriptor<T> other) => Equals(other);
}
