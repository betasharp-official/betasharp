using BetaSharp.Worlds.Core.Systems;
using BetaSharp.Worlds.Storage;

namespace BetaSharp.Client;

public interface ISingleplayerHost
{
    IWorldStorageSource SaveLoader { get; }
    void LoadWorld(string worldDir, string displayName, WorldSettings settings);
}
