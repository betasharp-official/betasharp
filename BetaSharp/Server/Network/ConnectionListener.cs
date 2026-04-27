using System.Net;
using System.Net.Sockets;
using BetaSharp.Network;
using BetaSharp.Profiling;
using BetaSharp.Server.Threading;
using Microsoft.Extensions.Logging;

namespace BetaSharp.Server.Network;

public class ConnectionListener
{
    public readonly record struct ConnectionTotals(long BytesRead, long BytesWritten, long PacketsRead, long PacketsWritten);

    public Socket Socket { get; }

    private readonly AcceptConnectionThread _thread;
    private readonly ILogger<ConnectionListener> _logger = Log.Instance.For<ConnectionListener>();

    public volatile bool open;
    private int _connectionCounter = 0;
    private readonly object _connectionCounterLock = new();
    private readonly object _pendingConnectionsLock = new();
    private readonly object _connectionsLock = new();
    private readonly List<ServerLoginNetworkHandler> _pendingConnections = [];
    private readonly List<ServerPlayNetworkHandler> _connections = [];
    private long _closedBytesRead;
    private long _closedBytesWritten;
    private long _closedPacketsRead;
    private long _closedPacketsWritten;
    public BetaSharpServer server;
    public int port;

    public int PendingConnectionCount
    {
        get
        {
            lock (_pendingConnectionsLock)
            {
                return _pendingConnections.Count;
            }
        }
    }

    public int ActiveConnectionCount
    {
        get
        {
            lock (_connectionsLock)
            {
                return _connections.Count;
            }
        }
    }

    public ConnectionListener(BetaSharpServer server, IPAddress address, int port, bool dualStack = false)
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
        _thread.Run();
    }

    public ConnectionListener(BetaSharpServer server)
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
        using (Profiler.Begin("PendingConnections", ProfilingDetailLevel.Detailed))
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
                        if (connection.connection.IsDisconnected || !connection.connection.IsOpen)
                        {
                            AccumulateClosedConnection(connection.connection);
                        }

                        _pendingConnections.RemoveAt(i--);
                    }
                }
            }
        }

        using (Profiler.Begin("ActiveConnections", ProfilingDetailLevel.Detailed))
        {
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
                        AccumulateClosedConnection(connection.connection);
                        _connections.RemoveAt(i--);
                    }
                }
            }
        }
    }

    public ConnectionTotals GetTotals()
    {
        long bytesRead = _closedBytesRead;
        long bytesWritten = _closedBytesWritten;
        long packetsRead = _closedPacketsRead;
        long packetsWritten = _closedPacketsWritten;

        lock (_pendingConnectionsLock)
        {
            for (int i = 0; i < _pendingConnections.Count; i++)
            {
                Connection connection = _pendingConnections[i].connection;
                bytesRead += connection.BytesRead;
                bytesWritten += connection.BytesWritten;
                packetsRead += connection.PacketsRead;
                packetsWritten += connection.PacketsWritten;
            }
        }

        lock (_connectionsLock)
        {
            for (int i = 0; i < _connections.Count; i++)
            {
                Connection connection = _connections[i].connection;
                bytesRead += connection.BytesRead;
                bytesWritten += connection.BytesWritten;
                packetsRead += connection.PacketsRead;
                packetsWritten += connection.PacketsWritten;
            }
        }

        return new ConnectionTotals(bytesRead, bytesWritten, packetsRead, packetsWritten);
    }

    private void AccumulateClosedConnection(Connection connection)
    {
        _closedBytesRead += connection.BytesRead;
        _closedBytesWritten += connection.BytesWritten;
        _closedPacketsRead += connection.PacketsRead;
        _closedPacketsWritten += connection.PacketsWritten;
    }
}
