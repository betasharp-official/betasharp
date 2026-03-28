using System.Net.Sockets;
using BetaSharp.Entities;
using BetaSharp.Network;
using BetaSharp.Network.Packets;
using BetaSharp.Network.Packets.Play;
using BetaSharp.Network.Packets.S2CPlay;
using BetaSharp.Server.Internal;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core;
using Microsoft.Extensions.Logging;
using Exception = System.Exception;

namespace BetaSharp.Server.Network;

public class ServerLoginNetworkHandler : NetHandler
{
    private static JavaRandom random = new();
    public Connection connection;
    public bool closed;
    private BetaSharpServer server;
    private int loginTicks;
    private string username;
    private LoginHelloPacket loginPacket;
    private string serverId = "";

    private readonly ILogger<ServerLoginNetworkHandler> _logger = Log.Instance.For<ServerLoginNetworkHandler>();

    public ServerLoginNetworkHandler(BetaSharpServer server, Socket socket, string name)
    {
        this.server = server;
        connection = new Connection(socket, name, this);
    }

    public ServerLoginNetworkHandler(BetaSharpServer server, Connection connection)
    {
        this.server = server;
        this.connection = connection;
        connection.setNetworkHandler(this);
    }

    public void tick()
    {
        if (loginPacket != null)
        {
            accept(loginPacket);
            loginPacket = null;
        }

        if (loginTicks++ == 600)
        {
            disconnect("Took too long to log in");
        }
        else
        {
            connection.tick();
        }
    }

    public void disconnect(string reason)
    {
        try
        {
            _logger.LogInformation($"Disconnecting {getConnectionInfo()}: {reason}");
            connection.sendPacket(DisconnectPacket.Get(reason));
            connection.disconnect();
            closed = true;
        }
        catch (Exception e)
        {
            _logger.LogError(e, e.Message);
        }
    }

    public override void onHandshake(HandshakePacket packet)
    {
        if (server.onlineMode)
        {
            serverId = random.NextLong().ToString("x");
            connection.sendPacket(new HandshakePacket(serverId));
        }
        else
        {
            connection.sendPacket(new HandshakePacket("-"));
        }
    }

    public override void onHello(LoginHelloPacket packet)
    {
        if (server is InternalServer)
        {
            packet.username = "player";
        }
        if (packet.worldSeed == LoginHelloPacket.BETASHARP_CLIENT_SIGNATURE)
        {
            connection.betaSharpClient = true;
        }

        username = packet.username;
        if (packet.protocolVersion != 14)
        {
            if (packet.protocolVersion > 14)
            {
                disconnect("Outdated server!");
            }
            else
            {
                disconnect("Outdated client!");
            }
        }
        else
        {
            if (!server.onlineMode)
            {
                accept(packet);
            }
            else
            {
                //TODO: ADD SOME KIND OF AUTH
                //new C_15575233(this, packet).start();
                throw new InvalidOperationException("Auth not supported");
            }
        }
    }

    public void accept(LoginHelloPacket packet)
    {
        ServerPlayerEntity ent = server.playerManager.connectPlayer(this, packet.username);
        if (ent != null)
        {
            server.playerManager.loadPlayerData(ent);
            ent.setWorld(server.getWorld(ent.dimensionId));
            ent.GameMode = GameModes.DefaultGameMode;
            _logger.LogInformation($"{getConnectionInfo()} logged in with entity id {ent.id} at ({ent.x}, {ent.y}, {ent.z})");
            ServerWorld var3 = server.getWorld(ent.dimensionId);
            Vec3i var4 = var3.Properties.GetSpawnPos();
            ServerPlayNetworkHandler handler = new ServerPlayNetworkHandler(server, connection, ent);
            handler.sendPacket(new LoginHelloPacket("", ent.id, var3.Seed, (sbyte)var3.Dimension.Id));
            handler.sendPacket(PlayerGameModeUpdateS2CPacket.Get(ent.GameMode));
            handler.sendPacket(PlayerSpawnPositionS2CPacket.Get(var4.X, var4.Y, var4.Z));
            server.playerManager.sendWorldInfo(ent, var3);
            server.playerManager.sendToAll(PlayerConnectionUpdateS2CPacket.Get(ent.id, PlayerConnectionUpdateS2CPacket.ConnectionUpdateType.Join, ent.name));
            server.playerManager.sendToAll(ChatMessagePacket.Get("§e" + ent.name + " joined the game."));
            server.playerManager.addPlayer(ent);
            handler.teleport(ent.x, ent.y, ent.z, ent.yaw, ent.pitch);
            server.connections.AddConnection(handler);
            handler.sendPacket(WorldTimeUpdateS2CPacket.Get(var3.GetTime()));
            ent.initScreenHandler();
        }

        closed = true;
    }

    public override void onDisconnected(string reason, object[]? objects)
    {
        _logger.LogInformation($"{getConnectionInfo()} lost connection");
        closed = true;
    }

    public override void handle(Packet packet)
    {
        disconnect("Protocol error");
    }

    public string getConnectionInfo()
    {
        var endPoint = connection.getAddress();

        if (endPoint == null) return "Internal";

        return !string.IsNullOrWhiteSpace(username) ? username : endPoint.ToString();
    }

    public override bool isServerSide()
    {
        return true;
    }
}
