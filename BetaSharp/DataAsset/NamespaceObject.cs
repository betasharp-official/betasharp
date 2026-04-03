namespace BetaSharp.DataAsset;

public partial class Namespace
{
    private readonly int _id;
    private readonly string _name;

    private Namespace(int id, string name)
    {
        ResourceLocation.Validate(name, "namespace");
        _id = id;
        _name = name;
    }

    public override int GetHashCode() => _id;
    public override bool Equals(object? obj) => obj is Namespace ns && Equals(ns);
    public bool Equals(Namespace obj) => obj._id == _id;

    public override string ToString() => _name;
    public static implicit operator string(Namespace n) => n._name;

    public static explicit operator Namespace?(string n)
    {
        TryGetValue(n, out Namespace? asset);
        return asset;
    }
}
