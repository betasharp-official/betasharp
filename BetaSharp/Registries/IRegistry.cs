namespace BetaSharp.Registries;

public interface IRegistry<T> : IReadableRegistry<T> where T : class
{
    void Register(ResourceLocation key, T value);
    void Register(int id, ResourceLocation key, T value);

    void Freeze();
    bool IsFrozen { get; }
}
