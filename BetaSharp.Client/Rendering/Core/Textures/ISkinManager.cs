namespace BetaSharp.Client.Rendering.Core.Textures;

public interface ISkinManager : IDisposable
{
    void RequestDownload(string? username);
    TextureHandle? GetTextureHandle(string? url);
    void Release(string? url);
}
