using BetaSharp.Worlds.Core;

namespace BetaSharp.Client.Network;

public interface IWorldHost
{
    World? World { get; }
    void ChangeWorld(World? world);
}
