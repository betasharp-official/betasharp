namespace BetaSharp.Registries;

/// <summary>
/// A typed key that identifies a registry. The type parameter <typeparamref name="T"/>
/// constrains what <see cref="IReadableRegistry{T}"/> can be stored under this key.
/// </summary>
public sealed class RegistryKey<T>(ResourceLocation location) where T : class
{
    public ResourceLocation Location { get; } = location;

    public override string ToString() => Location.ToString();
    public override bool Equals(object? obj) => obj is RegistryKey<T> other && Location.Equals(other.Location);
    public override int GetHashCode() => Location.GetHashCode();
}
