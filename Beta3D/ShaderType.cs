using BetaSharp;

namespace Beta3D;

public enum ShaderType
{
    Vertex,
    Fragment
}

public static class ShaderTypeExtensions
{
    public static string GetName(this ShaderType type) => type switch
    {
        ShaderType.Vertex => "vertex",
        ShaderType.Fragment => "fragment",
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
    };

    public static string GetExtension(this ShaderType type) => type switch
    {
        ShaderType.Vertex => ".vsh",
        ShaderType.Fragment => ".fsh",
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
    };

    public static ShaderType? ByLocation(ResourceLocation location)
    {
        foreach (ShaderType type in Enum.GetValues<ShaderType>())
            if (location.Path.EndsWith(type.GetExtension()))
                return type;
        return null;
    }
}
