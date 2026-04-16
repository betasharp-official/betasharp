using BetaSharp.Worlds.Core;

namespace BetaSharp.Client.Rendering.Chunks;

/// <summary>
/// Backend-owned factory for chunk renderer instances tied to a specific world.
/// </summary>
public interface IChunkRendererFactory
{
    IChunkRenderer Create(World world, Func<bool> useAlternateBlockModels);
}
