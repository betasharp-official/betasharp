using BetaSharp.Client.Guis;
using BetaSharp.Client.Network;
using BetaSharp.Network.Packets;
using Microsoft.Extensions.Logging;
using System.Net.Sockets;

namespace BetaSharp.Client.Threading;

public class ThreadConnectToServer(GuiConnecting connectingGui, BetaSharp game, string hostName, int port)
{
    private readonly ILogger<ThreadConnectToServer> _logger = Log.Instance.For<ThreadConnectToServer>();

    public void Start()
    {
        Thread thread = new Thread(Run)
        {
            IsBackground = true,
            Name = $"Server Connector ({hostName}:{port})"
        };
        thread.Start();
    }

    private void Run()
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

            _logger.LogError(e, e.Message);
            game.displayGuiScreen(new GuiConnectFailed("connect.failed", "disconnect.genericReason", e.ToString()));
        }
    }
}