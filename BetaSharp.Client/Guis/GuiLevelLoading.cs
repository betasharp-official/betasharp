using BetaSharp.Client.Network;
using BetaSharp.Network;
using BetaSharp.Server.Internal;
using BetaSharp.Server.Threading;
using Microsoft.Extensions.Logging;

namespace BetaSharp.Client.Guis;

public class GuiLevelLoading : Screen
{
    private readonly ILogger<GuiLevelLoading> _logger = Log.Instance.For<GuiLevelLoading>();
    private bool _serverStarted;
    public override bool PausesGame => false;

    public GuiLevelLoading(string worldDir, long seed)
    {
        if (!_serverStarted)
        {
            _serverStarted = true;
            MC.internalServer = new(System.IO.Path.Combine(Minecraft.getMinecraftDir().getAbsolutePath(), "saves"),
            worldDir, seed.ToString(), MC.options.renderDistance, MC.options.Difficulty);
            new RunServerThread(MC.internalServer, "InternalServer").start();
        }
    }

    protected override void OnTick()
    {
        if (MC.internalServer != null)
        {
            if (MC.internalServer.stopped)
            {
                MC.OpenScreen(new GuiConnectFailed("connect.failed", "disconnect.genericReason",
                    "Internal server stopped unexpectedly"));
                return;
            }

            if (MC.internalServer.isReady)
            {
                InternalConnection clientConnection = new(null, "Internal-Client");
                InternalConnection serverConnection = new(null, "Internal-Server");

                clientConnection.AssignRemote(serverConnection);
                serverConnection.AssignRemote(clientConnection);

                MC.internalServer.connections.AddInternalConnection(serverConnection);
                _logger.LogInformation("[Internal-Client] Created internal connection");

                ClientNetworkHandler clientHandler = new(MC, clientConnection);
                clientConnection.setNetworkHandler(clientHandler);
                clientHandler.addToSendQueue(new BetaSharp.Network.Packets.HandshakePacket(MC.session.username));

                MC.OpenScreen(new GuiConnecting(MC, clientHandler));
            }
        }
    }

    protected override void OnRender(RenderEventArgs e)
    {
        DrawDefaultBackground();

        string title = "Loading level";
        string progressMsg = "";
        int progress = 0;

        if (MC.internalServer != null)
        {
            progressMsg = MC.internalServer.progressMessage ?? "Starting server...";
            progress = MC.internalServer.progress;
        }

        Gui.DrawCenteredString(FontRenderer, title, EffectiveWidth / 2, EffectiveHeight / 2 - 50, 0xFFFFFF);
        Gui.DrawCenteredString(FontRenderer, progressMsg + " (" + progress + "%)", EffectiveWidth / 2, EffectiveHeight / 2 - 10, 0xFFFFFF);
    }
}
