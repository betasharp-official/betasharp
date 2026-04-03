using BetaSharp.Server.Internal;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Client;

public interface IInternalServerHost
{
    InternalServer? InternalServer { get; }
    void StartInternalServer(string worldDir, WorldSettings settings);
}
