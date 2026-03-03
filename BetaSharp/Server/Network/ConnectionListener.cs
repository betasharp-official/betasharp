using System.Net;
using System.Net.Sockets;
using BetaSharp.Network;
using BetaSharp.Server.Threading;
using Microsoft.Extensions.Logging;

namespace BetaSharp.Server.Network;

public class ConnectionListener
{
    public Socket Socket { get; }

    private readonly java.lang.Thread _thread;
    private readonly ILogger<ConnectionListener> _logger = Log.Instance.For<ConnectionListener>();

    public volatile bool open;
    private int _connectionCounter = 0;
    private readonly object _connectionCounterLock = new();
    private readonly object _pendingConnectionsLock = new();
    private readonly object _connectionsLock = new();
    private readonly List<ServerLoginNetworkHandler> _pendingConnections = [];
    private readonly List<ServerPlayNetworkHandler> _connections = [];
    public MinecraftServer server;
    public int port;

    public ConnectionListener(MinecraftServer server, IPAddress address, int port, bool dualStack = false)
    {
        this.server = server;

        Socket = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
        if (address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
        {
            Socket.DualMode = dualStack;
        }
        Socket.Bind(new IPEndPoint(address, port));
        Socket.Listen();

        this.port = port;
        open = true;
        _thread = new AcceptConnectionThread(this, "Listen Thread");
        _thread.start();
    }

    public ConnectionListener(MinecraftServer server)
    {
        this.server = server;
        Socket = null;
        port = 0;
        open = true;
        _thread = null;
    }

    public int GetNextConnectionCounter()
    {
        lock (_connectionCounterLock)
        {
            return _connectionCounter++;
        }
    }

    public void AddConnection(ServerPlayNetworkHandler connection)
    {
        lock (_connectionsLock)
        {
            _connections.Add(connection);
        }
    }

    public void AddPendingConnection(ServerLoginNetworkHandler connection)
    {
        if (connection == null)
        {
            throw new ArgumentException("Got null pendingconnection!", nameof(connection));
        }
        else
        {
            lock (_pendingConnectionsLock)
            {
                _pendingConnections.Add(connection);
            }
        }
    }

    public void AddInternalConnection(InternalConnection connection)
    {
        ServerLoginNetworkHandler loginHandler = new(server, connection);
        lock (_pendingConnectionsLock)
        {
            _pendingConnections.Add(loginHandler);
        }
    }

    public void Tick()
    {
        lock (_pendingConnectionsLock)
        {
            for (int i = 0; i < _pendingConnections.Count; i++)
            {
                ServerLoginNetworkHandler connection = _pendingConnections[i];

                try
            {
                connection.tick();
            }
            catch (Exception ex)
            {
                connection.disconnect("Internal server error");
                _logger.LogError($"Failed to handle packet: {ex}");
            }

            if (connection.closed)
            {
                _pendingConnections.RemoveAt(i--);
            }

                connection.connection.interrupt();
            }
        }

        lock (_connectionsLock)
        {
            for (int i = 0; i < _connections.Count; i++)
            {
                ServerPlayNetworkHandler connection = _connections[i];

            try
            {
                connection.tick();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to handle packet: {ex}");
                connection.disconnect("Internal server error");
            }

            if (connection.disconnected)
            {
                _connections.RemoveAt(i--);
            }

            connection.connection.interrupt();
            }
        }
    }
}
