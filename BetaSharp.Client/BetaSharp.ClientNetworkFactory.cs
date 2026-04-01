using BetaSharp.Client.Input;
using BetaSharp.Client.Network;
using BetaSharp.Client.UI;
using BetaSharp.Client.UI.Screens.Menu.Net;

namespace BetaSharp.Client;

public partial class BetaSharp : IClientNetworkFactory
{
    public PlayerController CreatePlayerController(ClientNetworkHandler handler) =>
        new PlayerControllerMP(this, handler);

    public UIScreen CreateTerrainScreen(ClientNetworkHandler handler) =>
        new DownloadingTerrainScreen(UIContext, handler);

    public UIScreen CreateFailedScreen(string messageKey, string detailKey, object[]? args)
    {
        StopInternalServer();
        return new ConnectFailedScreen(UIContext, messageKey, detailKey, args);
    }
}
