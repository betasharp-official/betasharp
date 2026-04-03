using BetaSharp.Client.Guis;
using BetaSharp.Client.Network;
using BetaSharp.Client.UI.Controls;
using BetaSharp.Client.UI.Controls.Core;
using BetaSharp.Client.UI.Layout.Flexbox;
using BetaSharp.Network;
using BetaSharp.Network.Packets;
using BetaSharp.Server.Internal;
using BetaSharp.Worlds.Core.Systems;
using Microsoft.Extensions.Logging;

namespace BetaSharp.Client.UI.Screens.Menu.Net;

public class LevelLoadingScreen(
    UIContext context,
    ClientNetworkContext networkContext,
    string worldDir,
    WorldSettings settings,
    IInternalServerHost serverHost) : UIScreen(context)
{
    private readonly ILogger<LevelLoadingScreen> _logger = Log.Instance.For<LevelLoadingScreen>();
    private bool _serverStarted;

    private Label _lblProgress = null!;

    public override bool PausesGame => false;

    protected override void Init()
    {
        Root.AddChild(new Background());
        Root.Style.AlignItems = Align.Center;
        Root.Style.JustifyContent = Justify.Center;

        Label lblTitle = new()
        {
            Text = "Loading level",
            TextColor = Color.White,
            Centered = true
        };
        lblTitle.Style.MarginBottom = 10;
        Root.AddChild(lblTitle);

        _lblProgress = new Label
        {
            Text = "Starting server...",
            TextColor = Color.White,
            Centered = true
        };
        Root.AddChild(_lblProgress);

        if (!_serverStarted)
        {
            _serverStarted = true;
            serverHost.StartInternalServer(worldDir, settings);
        }
    }

    public override void Update(float partialTicks)
    {
        base.Update(partialTicks);

        InternalServer? server = serverHost.InternalServer;
        if (server != null)
        {
            if (server.stopped)
            {
                Context.Navigator.Navigate(new ConnectFailedScreen(Context, "connect.failed", "disconnect.genericReason", "Internal server stopped unexpectedly"));
                return;
            }

            string progressMsg = server.progressMessage ?? "Starting server...";
            int progress = server.progress;
            _lblProgress.Text = $"{progressMsg} ({progress}%)";

            if (server.isReady)
            {
                InternalConnection clientConnection = new(null, "Internal-Client");
                InternalConnection serverConnection = new(null, "Internal-Server");

                clientConnection.AssignRemote(serverConnection);
                serverConnection.AssignRemote(clientConnection);

                server.connections.AddInternalConnection(serverConnection);
                _logger.LogInformation("[Internal-Client] Created internal connection");

                ClientNetworkHandler clientHandler = new(networkContext, clientConnection);
                clientConnection.setNetworkHandler(clientHandler);
                _logger.LogInformation("[Internal-Client] Sending HandshakePacket");
                clientHandler.AddToSendQueue(new HandshakePacket(networkContext.Session.username));

                Context.Navigator.Navigate(new ConnectingScreen(Context, clientHandler));
            }
        }
    }

    public override void KeyTyped(int key, char character)
    {
        // Do nothing to prevent escaping
    }
}
