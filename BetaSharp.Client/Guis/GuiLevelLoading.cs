using BetaSharp.Client.Network;
using BetaSharp.Network;
using BetaSharp.Server.Internal;
using BetaSharp.Server.Threading;
using BetaSharp.Worlds;
using Microsoft.Extensions.Logging;

namespace BetaSharp.Client.Guis;

public class GuiLevelLoading(string worldDir, WorldSettings settings) : GuiScreen
{
    private readonly ILogger<GuiLevelLoading> _logger = Log.Instance.For<GuiLevelLoading>();
    private readonly string _worldDir = worldDir;
    private readonly WorldSettings _settings = settings;
    private bool _serverStarted;

    public override bool PausesGame=> false;

    public override void InitGui()
    {
        _controlList.Clear();
        if (!_serverStarted)
        {
            _serverStarted = true;
            Game.internalServer = new InternalServer(Path.Combine(BetaSharp.getBetaSharpDir(), "saves"), _worldDir, _settings, Game.options.renderDistance, Game.options.Difficulty);
            new RunServerThread(Game.internalServer, "InternalServer").start();
        }
    }

    public override void UpdateScreen()
    {
        if (Game.internalServer != null)
        {
            if (Game.internalServer.stopped)
            {
                Game.displayGuiScreen(new GuiConnectFailed("connect.failed", "disconnect.genericReason", "Internal server stopped unexpectedly"));
                return;
            }

            if (Game.internalServer.isReady)
            {
                InternalConnection clientConnection = new(null, "Internal-Client");
                InternalConnection serverConnection = new(null, "Internal-Server");

                clientConnection.AssignRemote(serverConnection);
                serverConnection.AssignRemote(clientConnection);

                Game.internalServer.connections.AddInternalConnection(serverConnection);
                _logger.LogInformation("[Internal-Client] Created internal connection");

                ClientNetworkHandler clientHandler = new(Game, clientConnection);
                clientConnection.setNetworkHandler(clientHandler);
                clientHandler.addToSendQueue(new global::BetaSharp.Network.Packets.HandshakePacket(Game.session.username));

                Game.displayGuiScreen(new GuiConnecting(Game, clientHandler));
            }
        }
    }

    public override void Render(int var1, int var2, float var3)
    {
        DrawDefaultBackground();
        TranslationStorage var4 = TranslationStorage.Instance;

        string title = "Loading level";
        string progressMsg = "";
        int progress = 0;

        if (Game.internalServer != null)
        {
            progressMsg = Game.internalServer.progressMessage ?? "Starting server...";
            progress = Game.internalServer.progress;
        }

        DrawCenteredString(FontRenderer, title, Width / 2, Height / 2 - 50, Color.White);
        DrawCenteredString(FontRenderer, progressMsg + " (" + progress + "%)", Width / 2, Height / 2 - 10, Color.White);

        base.Render(var1, var2, var3);
    }
}
