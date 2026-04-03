using System.Text.Json.Serialization;

namespace BetaSharp.DataAsset;

public interface IDataAsset
{
    public string Name { get; internal set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public Namespace Namespace { get; internal set; }

    int GetHashCode() => HashCode.Combine(Name.GetHashCode(), Namespace.GetHashCode());
    string? ToString() => Namespace + ':' + Name;
}

public class BaseDataDataAsset : IDataAsset
{
    public string Name { get; set; } = "";

    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public Namespace Namespace { get; set; } = Namespace.BetaSharp;

    public override int GetHashCode() => HashCode.Combine(Name.GetHashCode(), Namespace.GetHashCode());
    public override string ToString() => Namespace + ':' + Name;
}
