namespace Beta3D;

public interface IGraphicsResourceAllocator
{
    static readonly IGraphicsResourceAllocator Unpooled = new UnpooledAllocator();
    T Acquire<T>(IResourceDescriptor<T> descriptor);
    void Release<T>(IResourceDescriptor<T> descriptor, T resource);
}

sealed file class UnpooledAllocator : IGraphicsResourceAllocator
{
    public T Acquire<T>(IResourceDescriptor<T> descriptor)
    {
        T resource = descriptor.Allocate();
        descriptor.Prepare(resource);
        return resource;
    }

    public void Release<T>(IResourceDescriptor<T> descriptor, T resource)
        => descriptor.Free(resource);
}
