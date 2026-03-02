using System.Net.Sockets;
using BetaSharp.Entities;
using BetaSharp.Network;
using BetaSharp.Network.Packets;
using BetaSharp.Network.Packets.Play;
using BetaSharp.Network.Packets.S2CPlay;
using BetaSharp.Server.Internal;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds;
using Microsoft.Extensions.Logging;

namespace BetaSharp.Server.Network;

public class ServerLoginNetworkHandler : NetHandler
{
    private static JavaRandom random = new();
    public Connection Connection;
    public bool Closed;
    private MinecraftServer server;
    private int loginTicks;
    private string username;
    private LoginHelloPacket loginPacket;
    private string serverId = "";

    private readonly ILogger<ServerLoginNetworkHandler> _logger = Log.Instance.For<ServerLoginNetworkHandler>();

    public ServerLoginNetworkHandler(MinecraftServer server, Socket socket, string name)
    {
        this.server = server;
        Connection = new Connection(socket, name, this);
        Connection.lag = 0;
    }

    public ServerLoginNetworkHandler(MinecraftServer server, Connection connection)
    {
        this.server = server;
        this.Connection = connection;
        Connection.setNetworkHandler(this);
        Connection.lag = 0;
    }

    public void Tick()
    {
        if (loginPacket != null)
        {
            Accept(loginPacket);
            loginPacket = null;
        }

        if (loginTicks++ == 600)
        {
            Disconnect("Took too long to log in");
        }
        else
        {
            Connection.tick();
        }
    }

    public void Disconnect(string reason)
    {
        try
        {
            _logger.LogInformation($"Disconnecting {GetConnectionInfo()}: {reason}");
            Connection.SendPacket(new DisconnectPacket(reason));
            Connection.Disconnect();
            Closed = true;
        }
        catch (Exception e)
        {
            _logger.LogError(e, e.Message);
        }
    }

    public override void onHandshake(HandshakePacket packet)
    {
        if (server.OnlineMode)
        {
            serverId = random.NextLong().ToString("x");
            Connection.SendPacket(new HandshakePacket(serverId));
        }
        else
        {
            Connection.SendPacket(new HandshakePacket("-"));
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
            // This is a BetaSharp client. We can use this for future protocol extensions.
        }

        username = packet.username;
        if (packet.protocolVersion != 14)
        {
            if (packet.protocolVersion > 14)
            {
                Disconnect("Outdated server!");
            }
            else
            {
                Disconnect("Outdated client!");
            }
        }
        else
        {
            if (!server.OnlineMode)
            {
                Accept(packet);
            }
            else
            {
                //TODO: ADD SOME KIND OF AUTH
                //new C_15575233(this, packet).start();
                throw new InvalidOperationException("Auth not supported");
            }
        }
    }

    public void Accept(LoginHelloPacket packet)
    {
        ServerPlayerEntity ent = server.PlayerManager.ConnectPlayer(this, packet.username);
        if (ent != null)
        {
            server.PlayerManager.LoadPlayerData(ent);
            ent.setWorld(server.GetWorld(ent.dimensionId));
            _logger.LogInformation($"{GetConnectionInfo()} logged in with entity id {ent.id} at ({ent.x}, {ent.y}, {ent.z})");
            ServerWorld var3 = server.GetWorld(ent.dimensionId);
            Vec3i var4 = var3.getSpawnPos();
            ServerPlayNetworkHandler handler = new ServerPlayNetworkHandler(server, Connection, ent);
            handler.SendPacket(new LoginHelloPacket("", ent.id, var3.getSeed(), (sbyte)var3.dimension.Id));
            handler.SendPacket(new PlayerSpawnPositionS2CPacket(var4.X, var4.Y, var4.Z));
            server.PlayerManager.SendWorldInfo(ent, var3);
            server.PlayerManager.SendToAll(new ChatMessagePacket("Â§e" + ent.name + " joined the game."));
            server.PlayerManager.AddPlayer(ent);
            handler.Teleport(ent.x, ent.y, ent.z, ent.yaw, ent.pitch);
            server.Connections.AddConnection(handler);
            handler.SendPacket(new WorldTimeUpdateS2CPacket(var3.getTime()));
            ent.initScreenHandler();
        }

        Closed = true;
    }

    public override void onDisconnected(string reason, object[]? objects)
    {
        _logger.LogInformation($"{GetConnectionInfo()} lost connection");
        Closed = true;
    }

    public override void handle(Packet packet)
    {
        Disconnect("Protocol error");
    }

    public string GetConnectionInfo()
    {
        var endPoint = Connection.getAddress();

        if (endPoint == null) return "Internal";

        return !string.IsNullOrWhiteSpace(username) ? username : endPoint.ToString();
    }

    public override bool IsServerSide()
    {
        return true;
    }
}
