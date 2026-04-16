namespace BetaSharp.Client.Rendering.Core.Textures;

public sealed class OpenGlTextureResourceFactory : ITextureResourceFactory
{
    public int ActiveTextureCount => GLTexture.ActiveTextureCount;

    public ITextureResource CreateTexture(string source)
    {
        return new GLTexture(source);
    }
}
