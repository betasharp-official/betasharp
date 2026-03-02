using System.Net.Sockets;
using BetaSharp.Client.Guis;
using BetaSharp.Client.Network;
using BetaSharp.Network.Packets;
using Microsoft.Extensions.Logging;

namespace BetaSharp.Client.Threading;

public class ThreadConnectToServer(GuiConnecting connectingGui, Minecraft mc, string hostName, int port)
{
    private readonly ILogger<ThreadConnectToServer> _logger = Log.Instance.For<ThreadConnectToServer>();

    public void Start()
    {
        var thread = new Thread(Run) { Name = "ConnectToServer", IsBackground = true };
        thread.Start();
    }

    private void Run()
    {
        try
        {
            GuiConnecting.setNetClientHandler(connectingGui, new ClientNetworkHandler(mc, hostName, port));

            if (GuiConnecting.isCancelled(connectingGui))
            {
                return;
            }

            GuiConnecting.getNetClientHandler(connectingGui).addToSendQueue(new HandshakePacket(mc.session.username));
        }
        catch (SocketException ex) when (ex.SocketErrorCode is SocketError.HostNotFound or SocketError.TryAgain or SocketError.HostUnreachable)
        {
            if (GuiConnecting.isCancelled(connectingGui))
            {
                return;
            }

            mc.displayGuiScreen(new GuiConnectFailed("connect.failed", "disconnect.genericReason", "Unknown host '" + hostName + "'"));
        }
        catch (SocketException ex)
        {
            if (GuiConnecting.isCancelled(connectingGui))
            {
                return;
            }

            mc.displayGuiScreen(new GuiConnectFailed("connect.failed", "disconnect.genericReason", ex.Message));
        }
        catch (Exception e)
        {
            if (GuiConnecting.isCancelled(connectingGui))
            {
                return;
            }

            _logger.LogError(e, e.Message);
            mc.displayGuiScreen(new GuiConnectFailed("connect.failed", "disconnect.genericReason", e.ToString()));
        }
    }
}
