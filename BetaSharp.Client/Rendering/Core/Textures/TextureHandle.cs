namespace BetaSharp.Client.Rendering.Core.Textures;

public class TextureHandle
{
    public GLTexture? Texture { get; internal set; }
    public int Id => (int)(Texture?.Id ?? 0u);

    internal TextureHandle(GLTexture? texture)
    {
        Texture = texture;
    }

    public void Bind()
    {
        Texture?.Bind();
    }

    public override string ToString()
    {
        return $"TextureHandle(Id={Id}, Source={Texture?.Source ?? "null"})";
    }
}
