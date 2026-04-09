namespace BetaSharp;

public readonly struct LightType : IEquatable<LightType>
{
    public static readonly LightType Sky = new(15);
    public static readonly LightType Block = new(0);

    public readonly int LightValue;

    private LightType(int lightValue) => LightValue = lightValue;

    public override bool Equals(object? obj) => obj is LightType other && Equals(other);

    public bool Equals(LightType other) => LightValue == other.LightValue;

    public static bool operator ==(LightType left, LightType right) => left.Equals(right);

    public static bool operator !=(LightType left, LightType right) => !(left == right);

    public override int GetHashCode() => throw new NotImplementedException();
}
