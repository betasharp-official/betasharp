using BetaSharp.Client.Network;
using BetaSharp.Network;
using BetaSharp.Server.Internal;
using BetaSharp.Server.Threading;
using Microsoft.Extensions.Logging;

namespace BetaSharp.Client.Guis;

public class GuiLevelLoading(string worldDir, long seed) : GuiScreen
{
    private readonly ILogger<GuiLevelLoading> _logger = Log.Instance.For<GuiLevelLoading>();
    private readonly string _worldDir = worldDir;
    private readonly long _seed = seed;
    private bool _serverStarted;

    public override bool PausesGame=> false;

    public override void InitGui()
    {
        _controlList.Clear();
        if (!_serverStarted)
        {
            _serverStarted = true;
            mc.internalServer = new InternalServer(System.IO.Path.Combine(Minecraft.getMinecraftDir(), "saves"), _worldDir, _seed.ToString(), mc.options.renderDistance, mc.options.Difficulty);
            new RunServerThread(mc.internalServer, "InternalServer").Start();
        }
    }

    public override void UpdateScreen()
    {
        if (mc.internalServer != null)
        {
            if (mc.internalServer.Stopped)
            {
                mc.displayGuiScreen(new GuiConnectFailed("connect.failed", "disconnect.genericReason", "Internal server Stopped unexpectedly"));
                return;
            }

            if (mc.internalServer.IsReady)
            {
                InternalConnection clientConnection = new(null, "Internal-Client");
                InternalConnection serverConnection = new(null, "Internal-Server");

                clientConnection.AssignRemote(serverConnection);
                serverConnection.AssignRemote(clientConnection);

                mc.internalServer.Connections.AddInternalConnection(serverConnection);
                _logger.LogInformation("[Internal-Client] Created internal connection");

                ClientNetworkHandler clientHandler = new(mc, clientConnection);
                clientConnection.setNetworkHandler(clientHandler);
                clientHandler.addToSendQueue(new BetaSharp.Network.Packets.HandshakePacket(mc.session.username));

                mc.displayGuiScreen(new GuiConnecting(mc, clientHandler));
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

        if (mc.internalServer != null)
        {
            progressMsg = mc.internalServer.ProgressMessage ?? "Starting server...";
            progress = mc.internalServer.Progress;
        }

        DrawCenteredString(FontRenderer, title, Width / 2, Height / 2 - 50, Color.White);
        DrawCenteredString(FontRenderer, progressMsg + " (" + progress + "%)", Width / 2, Height / 2 - 10, Color.White);

        base.Render(var1, var2, var3);
    }
}
