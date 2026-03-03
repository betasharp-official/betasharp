using BetaSharp.Client.Guis;
using BetaSharp.Client.Network;
using BetaSharp.Network.Packets;
using java.net;
using Microsoft.Extensions.Logging;

namespace BetaSharp.Client.Threading;

public class ThreadConnectToServer(GuiConnecting connectingGui, BetaSharp game, string hostName, int port) : java.lang.Thread
{
    private readonly ILogger<ThreadConnectToServer> _logger = Log.Instance.For<ThreadConnectToServer>();

    public override void run()
    {
        try
        {
            GuiConnecting.setNetClientHandler(connectingGui, new ClientNetworkHandler(game, hostName, port));

            if (GuiConnecting.isCancelled(connectingGui))
            {
                return;
            }

            GuiConnecting.getNetClientHandler(connectingGui).addToSendQueue(new HandshakePacket(game.session.username));
        }
        catch (UnknownHostException)
        {
            if (GuiConnecting.isCancelled(connectingGui))
            {
                return;
            }

            game.displayGuiScreen(new GuiConnectFailed("connect.failed", "disconnect.genericReason", "Unknown host \'" + hostName + "\'"));
        }
        catch (ConnectException ex)
        {
            if (GuiConnecting.isCancelled(connectingGui))
            {
                return;
            }

            game.displayGuiScreen(new GuiConnectFailed("connect.failed", "disconnect.genericReason", ex.getMessage()));
        }
        catch (Exception e)
        {
            if (GuiConnecting.isCancelled(connectingGui))
            {
                return;
            }

            _logger.LogError(e, e.Message);
            game.displayGuiScreen(new GuiConnectFailed("connect.failed", "disconnect.genericReason", e.ToString()));
        }
    }
}
