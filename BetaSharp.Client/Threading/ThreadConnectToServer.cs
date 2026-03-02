using BetaSharp.Client.Guis;
using BetaSharp.Client.Network;
using BetaSharp.Network.Packets;
using java.net;
using Microsoft.Extensions.Logging;

namespace BetaSharp.Client.Threading;

public class ThreadConnectToServer(GuiConnecting connectingGui, Minecraft mc, string hostName, int port) : java.lang.Thread
{
    private readonly ILogger<ThreadConnectToServer> _logger = Log.Instance.For<ThreadConnectToServer>();

    public override void run()
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
        catch (UnknownHostException)
        {
            if (GuiConnecting.isCancelled(connectingGui))
            {
                return;
            }

            mc.displayGuiScreen(new GuiConnectFailed("connect.failed", "disconnect.genericReason", "Unknown host \'" + hostName + "\'"));
        }
        catch (ConnectException ex)
        {
            if (GuiConnecting.isCancelled(connectingGui))
            {
                return;
            }

            mc.displayGuiScreen(new GuiConnectFailed("connect.failed", "disconnect.genericReason", ex.getMessage()));
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
