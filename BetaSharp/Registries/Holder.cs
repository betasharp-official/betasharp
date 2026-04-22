namespace BetaSharp.Registries;

/// <summary>
/// An indirection wrapper for registry entries.
/// <para>
/// <see cref="Holder{T}"/> provides a stable reference that survives live-reloads:
/// the wrapper stays the same while its internal value is swapped out.
/// </para>
/// </summary>
public sealed class Holder<T> where T : class
{
    private T? _value;
    private Func<T>? _resolver;

    /// <summary>
    /// Returns the held value, resolving lazily on first access if needed.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the registry entry this holder points to has been invalidated.
    /// </exception>
    public T Value
    {
        get
        {
            if (IsInvalid)
            {
                throw new InvalidOperationException(
                    $"Registry entry of type '{typeof(T).Name}' has been removed. " +
                    "The server should have migrated all references before reloading.");
            }

            if (_value != null) return _value;

            if (_resolver == null)
            {
                throw new InvalidOperationException("Holder has no value and no resolver.");
            }

            T resolved = _resolver();
            _value = resolved;
            _resolver = null;
            return _value;
        }
        internal set
        {
            _value = value;
            _resolver = null;
        }
    }

    /// <summary>True once the value has been resolved.</summary>
    public bool IsResolved => _value != null;

    /// <summary>
    /// True if the registry entry this holder pointed to has been removed.
    /// Accessing <see cref="Value"/> on an invalidated holder throws.
    /// </summary>
    public bool IsInvalid { get; private set; }

    /// <summary>
    /// Replaces the held value.
    /// </summary>
    public void Update(T newValue)
    {
        _value = newValue;
        _resolver = null;
    }

    /// <summary>
    /// Invalidates this holder. Any subsequent access to <see cref="Value"/> will throw, surfacing the
    /// missing server-side migration rather than returning stale data.
    /// </summary>
    public void Invalidate()
    {
        IsInvalid = true;
        _value = default;
        _resolver = null;
    }

    public static implicit operator T(Holder<T> h) => h.Value;

    public override string ToString() => _value?.ToString() ?? "<unresolved>";

    /// <summary>Creates a directly-valued holder (already resolved).</summary>
    public Holder(T value)
    {
        _value = value;
    }

    private Holder(Func<T> resolver)
    {
        _resolver = resolver;
    }

    /// <summary>
    /// Creates a lazily-resolved holder that invokes <paramref name="resolver"/> on first access.
    /// </summary>
    internal static Holder<T> Reference(Func<T> resolver) => new(resolver);
}
