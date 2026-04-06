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
    public T Value
    {
        get
        {
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

    public static implicit operator T(Holder<T> h) => h.Value;

    public override string ToString() => _value?.ToString() ?? "<unresolved>";

    /// <summary>Creates a directly-valued holder (already resolved).</summary>
    public Holder(T value)
    {
        _value = value;
    }

    /// <summary>Creates a lazily-resolved holder.</summary>
    internal Holder(Func<T> resolver)
    {
        _resolver = resolver;
    }

    /// <summary>
    /// Creates a lazily-resolved holder that invokes <paramref name="resolver"/> on first access.
    /// </summary>
    internal static Holder<T> Reference(Func<T> resolver) => new(resolver);
}
