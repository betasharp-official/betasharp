using BetaSharp.Client.Input;
using BetaSharp.Client.UI;

namespace BetaSharp.Client.Network;

public interface IClientNetworkFactory
{
    PlayerController CreatePlayerController(ClientNetworkHandler handler);
    UIScreen CreateTerrainScreen(ClientNetworkHandler handler);
    UIScreen CreateFailedScreen(string messageKey, string detailKey, object[]? args);
}
