using BetaSharp.Client.Rendering.Core.Textures;

namespace BetaSharp.Client.Rendering.Backends;

internal sealed class NoOpSkinManager : ISkinManager
{
    public void RequestDownload(string? username)
    {
    }

    public TextureHandle? GetTextureHandle(string? url)
    {
        return null;
    }

    public void Release(string? url)
    {
    }

    public void Dispose()
    {
    }
}
