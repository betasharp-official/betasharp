using System.Diagnostics.CodeAnalysis;

namespace BetaSharp.DataAsset;

public partial class Namespace
{
    public static readonly Namespace BetaSharp = new(0, "betasharp");
    private static readonly List<Namespace> s_idToName = [BetaSharp];

    private static readonly Dictionary<string, int> s_nameToId = new()
    {
        [BetaSharp._name] = 0
    };

    public static Namespace Get(string name)
    {
        if (s_nameToId.TryGetValue(name, out int value)) return s_idToName[value];
        value = s_idToName.Count;
        var ns = new Namespace(value, name);
        s_nameToId.Add(name, value);
        s_idToName.Add(ns);
        return ns;
    }

    public static bool TryGetValue(string name, [NotNullWhen(true)] out Namespace? asset)
    {
        if (s_nameToId.TryGetValue(name, out int value))
        {
            asset = s_idToName[value];
            return true;
        }

        asset = null;
        return false;
    }

    public static bool TryGetValue(int id, [NotNullWhen(true)] out Namespace? asset)
    {
        if (id >= 0 && id < s_idToName.Count)
        {
            asset = s_idToName[id];
            return true;
        }

        asset = null;
        return false;
    }

    public static Namespace? FindIndex(string name, bool allowShortName)
    {
        if (string.IsNullOrEmpty(name)) return null;
        if (s_nameToId.TryGetValue(name, out int value)) return s_idToName[value];
        if (!allowShortName) return null;
        if (name.Length == 1)
        {
            foreach (var ns in s_idToName)
            {
                if (ns._name[0] == name[0]) return ns;
            }
        }
        else
        {
            foreach (var ns in s_idToName)
            {
                if (ns._name.Length <= name.Length) continue;
                if (ns._name.Substring(0, name.Length) == name) return ns;
            }
        }

        return null;
    }
}
