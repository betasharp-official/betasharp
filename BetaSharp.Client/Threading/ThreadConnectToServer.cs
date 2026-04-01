using System.Net.Sockets;
using BetaSharp.Client.Network;
using BetaSharp.Client.UI.Screens.Menu.Net;
using BetaSharp.Network.Packets;
using Microsoft.Extensions.Logging;

namespace BetaSharp.Client.Threading;

public class ThreadConnectToServer(
    ConnectingScreen connectingScreen,
    ClientNetworkContext context,
    string hostName,
    int port)
{
    private readonly ILogger<ThreadConnectToServer> _logger = Log.Instance.For<ThreadConnectToServer>();

    public void Start()
    {
        Thread thread = new(Run)
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
            connectingScreen.ClientHandler = new ClientNetworkHandler(context, hostName, port);

            if (connectingScreen.IsCancelled)
            {
                return;
            }

            connectingScreen.ClientHandler.AddToSendQueue(new HandshakePacket(context.Session.username));
        }
        catch (SocketException ex) when (ex.SocketErrorCode == SocketError.HostNotFound)
        {
            if (connectingScreen.IsCancelled)
            {
                return;
            }

            context.Navigator.Navigate(context.Factory.CreateFailedScreen("connect.failed", "disconnect.genericReason", [$"Unknown host '{hostName}'"]));
        }
        catch (SocketException ex)
        {
            if (connectingScreen.IsCancelled)
            {
                return;
            }

            context.Navigator.Navigate(context.Factory.CreateFailedScreen("connect.failed", "disconnect.genericReason", [ex.Message]));
        }
        catch (Exception e)
        {
            if (connectingScreen.IsCancelled)
            {
                return;
            }

            _logger.LogError(e, e.Message);
            context.Navigator.Navigate(context.Factory.CreateFailedScreen("connect.failed", "disconnect.genericReason", [e.ToString()]));
        }
    }
}
