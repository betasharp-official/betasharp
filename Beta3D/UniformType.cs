namespace Beta3D;

public enum UniformType
{
    UniformBuffer,
    TexelBuffer
}

public static class UniformTypeExtensions
{
    public static string GetName(this UniformType type) => type switch
    {
        UniformType.UniformBuffer => "ubo",
        UniformType.TexelBuffer => "utb",
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
    };
}
