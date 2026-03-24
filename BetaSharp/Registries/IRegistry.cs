namespace BetaSharp.Registries;

public interface IRegistry<T> : IEnumerable<T>
{
    ResourceLocation Key { get; }
    void Register(ResourceLocation key, T value);
    void Register(int id, ResourceLocation key, T value);
    
    T? Get(int id);
    T? Get(ResourceLocation key);
    
    int GetId(T value);
    ResourceLocation? GetKey(T value);
    
    bool ContainsKey(ResourceLocation key);
    bool ContainsId(int id);

    void Freeze();
    bool IsFrozen { get; }
}
