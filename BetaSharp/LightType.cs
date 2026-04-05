namespace BetaSharp;

public readonly struct LightType : IEquatable<LightType>
{
    public static readonly LightType Sky = new(15);
    public static readonly LightType Block = new(0);

    public readonly int lightValue;

    private LightType(int lightValue)
    {
        this.lightValue = lightValue;
    }

    public override bool Equals(object? obj)
    {
        return obj is LightType other && Equals(other);
    }

    public bool Equals(LightType other)
    {
        return lightValue == other.lightValue;
    }

    public static bool operator ==(LightType left, LightType right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(LightType left, LightType right)
    {
        return !(left == right);
    }

    public override int GetHashCode()
    {
        throw new NotImplementedException();
    }
}
