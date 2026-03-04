using BetaSharp.Client.Guis;
using BetaSharp.Client.Network;
using BetaSharp.Network.Packets;
using Microsoft.Extensions.Logging;
using System.Net.Sockets;

namespace BetaSharp.Client.Threading;

public static class ThreadConnectToServer
{
    private static readonly ILogger s_logger = Log.Instance.For(nameof(ThreadConnectToServer));

    public static void Run(GuiConnecting connectingGui, BetaSharp game, string hostName, int port)
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
        catch (SocketException ex) when (ex.SocketErrorCode == SocketError.HostNotFound)
        {

            if (GuiConnecting.isCancelled(connectingGui))
            {
                return;
            }

            game.displayGuiScreen(new GuiConnectFailed("connect.failed", "disconnect.genericReason", "Unknown host \'" + hostName + "\'"));
        }
        catch (SocketException ex)
        {

            if (GuiConnecting.isCancelled(connectingGui))
            {
                return;
            }

            game.displayGuiScreen(new GuiConnectFailed("connect.failed", "disconnect.genericReason", ex.Message));
        }
        catch (Exception e)
        {
            if (GuiConnecting.isCancelled(connectingGui))
            {
                return;
            }

            s_logger.LogError(e, e.Message);
            game.displayGuiScreen(new GuiConnectFailed("connect.failed", "disconnect.genericReason", e.ToString()));
        }
    }
}
