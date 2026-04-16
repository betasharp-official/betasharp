namespace BetaSharp.Client.Rendering.Core.Textures;

public interface ITextureResourceFactory
{
    int ActiveTextureCount { get; }
    ITextureResource CreateTexture(string source);
}
