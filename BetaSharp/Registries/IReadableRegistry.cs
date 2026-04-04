namespace BetaSharp.Registries;

/// <summary>
/// Shared read-only query surface for both code-registered and data-driven registries.
/// </summary>
public interface IReadableRegistry<T> : IEnumerable<T> where T : class
{
    /// <summary>The registry's own identifier (e.g. <c>betasharp:entity_type</c>).</summary>
    ResourceLocation RegistryKey { get; }

    T? Get(ResourceLocation key);
    T? Get(int id);

    int GetId(T value);
    ResourceLocation? GetKey(T value);

    bool ContainsKey(ResourceLocation key);

    IEnumerable<ResourceLocation> Keys { get; }

    /// <summary>
    /// Returns a <see cref="Holder{T}"/> for the given key, or <c>null</c> if not present.
    /// Registries that support holder indirection override this; others return <c>null</c>.
    /// </summary>
    Holder<T>? GetHolder(ResourceLocation key) => null;
}
