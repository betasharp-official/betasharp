using BetaSharp.Client.Entities;
using BetaSharp.Client.Input;

namespace BetaSharp.Client.Network;

public interface IClientPlayerHost
{
    ClientPlayerEntity? Player { get; }
    void SetPlayerController(PlayerController controller);
    void Respawn(bool resetHealth, int dimensionId);
}
