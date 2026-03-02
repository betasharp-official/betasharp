using System.Net;
using System.Net.Sockets;
using BetaSharp.Network;
using BetaSharp.Server.Threading;
using Microsoft.Extensions.Logging;

namespace BetaSharp.Server.Network;

public class ConnectionListener
{
    public Socket Socket { get; }

    private readonly AcceptConnectionThread? _thread;
    private readonly ILogger<ConnectionListener> _logger = Log.Instance.For<ConnectionListener>();

    public volatile bool Open;
    public int ConnectionCounter = 0;
    private readonly List<ServerLoginNetworkHandler> _pendingConnections = [];
    private readonly List<ServerPlayNetworkHandler> _connections = [];
    public MinecraftServer Server;
    public int Port;

    public ConnectionListener(MinecraftServer server, IPAddress address, int port, bool dualStack = false)
    {
        this.Server = server;

        Socket = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
        if (dualStack)
        {
            Socket.DualMode = true;
        }
        Socket.Bind(new IPEndPoint(address, port));
        Socket.Listen();

        this.Port = port;
        Open = true;
        _thread = new AcceptConnectionThread(this, "Listen Thread");
        _thread.Start();
    }

    public ConnectionListener(MinecraftServer server)
    {
        this.Server = server;
        Socket = null!;
        Port = 0;
        Open = true;
        _thread = null;
    }

    public void AddConnection(ServerPlayNetworkHandler connection)
    {
        _connections.Add(connection);
    }

    public void AddPendingConnection(ServerLoginNetworkHandler connection)
    {
        if (connection == null)
        {
            throw new ArgumentException("Got null pendingconnection!", nameof(connection));
        }
        else
        {
            _pendingConnections.Add(connection);
        }
    }

    public void AddInternalConnection(InternalConnection connection)
    {
        ServerLoginNetworkHandler loginHandler = new(Server, connection);
        _pendingConnections.Add(loginHandler);
    }

    public void Tick()
    {
        for (int i = 0; i < _pendingConnections.Count; i++)
        {
            ServerLoginNetworkHandler connection = _pendingConnections[i];

            try
            {
                connection.Tick();
            }
            catch (Exception ex)
            {
                connection.Disconnect("Internal server error");
                _logger.LogError($"Failed to handle packet: {ex}");
            }

            if (connection.Closed)
            {
                _pendingConnections.RemoveAt(i--);
            }

            connection.Connection.interrupt();
        }

        for (int i = 0; i < _connections.Count; i++)
        {
            ServerPlayNetworkHandler connection = _connections[i];

            try
            {
                connection.Tick();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to handle packet: {ex}");
                connection.Disconnect("Internal server error");
            }

            if (connection.Disconnected)
            {
                _connections.RemoveAt(i--);
            }

            connection.Connection.interrupt();
        }
    }
}
