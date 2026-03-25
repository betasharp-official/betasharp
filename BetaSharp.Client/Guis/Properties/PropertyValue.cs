namespace BetaSharp.Client.Guis;

public abstract class PropertyValue
{
    public Property Property { get; protected init; }
    public object? Value => GetBoxedValue();
    protected abstract object? GetBoxedValue();
}

public class PropertyValue<T> : PropertyValue
{
    public T Value { get; }

    public PropertyValue(T value)
    {
        Value = value;
    }

    protected override object? GetBoxedValue()
    {
        return Value;
    }
}
