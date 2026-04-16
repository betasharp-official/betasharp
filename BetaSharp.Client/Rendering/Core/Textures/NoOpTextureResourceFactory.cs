namespace BetaSharp.Client.Rendering.Core.Textures;

public sealed class NoOpTextureResourceFactory : ITextureResourceFactory
{
    private int _activeTextureCount;
    public int ActiveTextureCount => _activeTextureCount;

    public ITextureResource CreateTexture(string source)
    {
        _activeTextureCount++;
        return new NoOpTextureResource(source, () => _activeTextureCount--);
    }
}
