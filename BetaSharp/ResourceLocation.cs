namespace BetaSharp;

public sealed partial class ResourceLocation : IEquatable<ResourceLocation>, IComparable<ResourceLocation>
{
    public Namespace Namespace { get; }
    public string Path { get; }

    public ResourceLocation(string @namespace, string path)
    {
        Validate256(path, nameof(path));
        Namespace = Namespace.Get(@namespace);
        Path = path;
    }

    public ResourceLocation(Namespace @namespace, string path)
    {
        Validate256(path, nameof(path));
        Namespace = @namespace;
        Path = path;
    }

    /// <summary>
    /// Parses "namespace:path" or bare "path".
    /// </summary>
    public static ResourceLocation Parse(string location)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(location);
        int colon = location.IndexOf(':');
        return colon switch
        {
            -1 => new ResourceLocation(Namespace.BetaSharp, location),
            0 => throw new FormatException($"Missing namespace in '{location}'."),
            _ => new ResourceLocation(location[..colon], location[(colon + 1)..])
        };
    }

    public static bool TryParse(string location, out ResourceLocation? result)
    {
        try { result = Parse(location); return true; }
        catch { result = null; return false; }
    }

    private static readonly System.Text.RegularExpressions.Regex s_validPattern =
        Reg();

    internal static void Validate256(string part, string paramName)
    {
        if (part.Length > 256)
            throw new ArgumentException("Must not exceed 256 characters", paramName);
        Validate(part, paramName);
    }

    internal static void Validate(string part, string paramName)
    {
        if (string.IsNullOrEmpty(part))
            throw new ArgumentException("Must not be null or empty.", paramName);
        if (!s_validPattern.IsMatch(part))
            throw new ArgumentException(
                $"'{part}' contains invalid characters. Only [a-z0-9_-.] are allowed.", paramName);
        if (part[0] < 'a' && part[0] > 'z')
            throw new ArgumentException(
                $"'{part}' must start with a [a-z] letter.", paramName);
    }

    public bool Equals(ResourceLocation? other) =>
        other is not null &&
        Namespace.Equals(other.Namespace) &&
        Path == other.Path;

    public override bool Equals(object? obj) => Equals(obj as ResourceLocation);

    public override int GetHashCode() => HashCode.Combine(Namespace, Path);

    public int CompareTo(ResourceLocation? other)
    {
        if (other is null) return 1;
        int ns = string.Compare(Namespace, other.Namespace, StringComparison.Ordinal);
        return ns != 0 ? ns : string.Compare(Path, other.Path, StringComparison.Ordinal);
    }

    public static bool operator ==(ResourceLocation? a, ResourceLocation? b) =>
        a?.Equals(b) ?? b is null;
    public static bool operator !=(ResourceLocation? a, ResourceLocation? b) => !(a == b);

    public static implicit operator ResourceLocation(string s) => Parse(s);
    public static explicit operator string(ResourceLocation r) => r.ToString();

    public override string ToString() => $"{Namespace}:{Path}";

    public ResourceLocation WithPath(string newPath) => new(Namespace, newPath);

    public ResourceLocation Append(string child) => new(Namespace, $"{Path}/{child}");

    public bool IsVanilla => Namespace.GetHashCode() == Namespace.BetaSharp.GetHashCode();

    [System.Text.RegularExpressions.GeneratedRegex(@"^[a-z0-9_\-\.]+$", System.Text.RegularExpressions.RegexOptions.Compiled)]
    private static partial System.Text.RegularExpressions.Regex Reg();
}
