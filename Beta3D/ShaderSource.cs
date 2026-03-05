using BetaSharp;

namespace Beta3D;

public interface IShaderSource
{
    string? Get(ResourceLocation location, ShaderType type);
}
