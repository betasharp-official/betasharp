using System.Text.Json.Serialization;

namespace BetaSharp.Registries.Data;

public interface IDataAsset
{
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public string Name { get; internal set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public Namespace Namespace { get; internal set; }

    int GetHashCode() => HashCode.Combine(Name.GetHashCode(), Namespace.GetHashCode());
    string? ToString() => Namespace + ':' + Name;
}

public class DataAsset : IDataAsset
{
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public string Name { get; set; } = "";

    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public Namespace Namespace { get; set; } = Namespace.BetaSharp;

    public override int GetHashCode() => HashCode.Combine(Name.GetHashCode(), Namespace.GetHashCode());
    public override string ToString() => Namespace + ':' + Name;
}
