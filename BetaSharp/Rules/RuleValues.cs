using System.Globalization;

namespace BetaSharp.Rules;

public sealed record BoolValue(bool Value) : IRuleValue
{
    public static implicit operator BoolValue(bool v) => new(v);
    public static implicit operator bool(BoolValue v) => v.Value;
    public override string ToString() => Value ? "true" : "false";
}

public sealed record IntValue(int Value) : IRuleValue
{
    public static implicit operator IntValue(int v) => new(v);
    public static implicit operator int(IntValue v) => v.Value;
    public override string ToString() => Value.ToString();
}

public sealed record FloatValue(float Value) : IRuleValue
{
    public static implicit operator FloatValue(float v) => new(v);
    public static implicit operator float(FloatValue v) => v.Value;
    public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);
}

public sealed record StringValue(string Value) : IRuleValue
{
    public static implicit operator StringValue(string v) => new(v);
    public static implicit operator string(StringValue v) => v.Value;
    public override string ToString() => Value;
}

public sealed record EnumValue<T>(T Value) : IRuleValue where T : struct, Enum
{
    public static implicit operator EnumValue<T>(T v) => new(v);
    public static implicit operator T(EnumValue<T> v) => v.Value;
    public override string ToString() => Value.ToString();
}
