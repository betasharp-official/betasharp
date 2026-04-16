namespace BetaSharp.Client.Rendering.Core.Textures;

public class TextureHandle
{
    public ITextureResource? Texture { get; internal set; }
    public int Id => (int)(Texture?.Id ?? 0u);
    public string Source => Texture?.Source ?? "null";

    internal TextureHandle(ITextureResource? texture)
    {
        Texture = texture;
    }

    public void Bind()
    {
        Texture?.Bind();
    }

    public override string ToString()
    {
        return $"TextureHandle(Id={Id}, Source={Source})";
    }
}
