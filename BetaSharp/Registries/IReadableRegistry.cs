using System.Diagnostics.CodeAnalysis;

namespace BetaSharp.Registries;

/// <summary>
/// Shared read-only query surface for both code-registered and data-driven registries.
/// </summary>
public interface IReadableRegistry<T> : IEnumerable<T> where T : class
{
    /// <summary>The registry's own identifier (e.g. <c>betasharp:entity_type</c>).</summary>
    ResourceLocation RegistryKey { get; }

    /// <summary>
    /// Returns a <see cref="Holder{T}"/> for the given key, or <c>null</c> if not present.
    /// </summary>
    Holder<T>? Get(ResourceLocation key);

    T? Get(int id);

    bool TryGet(ResourceLocation key, [NotNullWhen(true)] out T? asset)
    {
        asset = Get(key)?.Value;
        return asset != null;
    }

    /// <summary>
    /// Returns the current value for the given key, or <c>null</c> if not present.
    /// </summary>
    T? GetValue(ResourceLocation key) => Get(key)?.Value;

    int GetId(T value);
    ResourceLocation? GetKey(T value);

    bool ContainsKey(ResourceLocation key);

    IEnumerable<ResourceLocation> Keys { get; }
}
