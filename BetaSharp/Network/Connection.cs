using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using BetaSharp.Network.Packets;
using Microsoft.Extensions.Logging;

namespace BetaSharp.Network;

public class Connection
{
    public bool betaSharpClient = false;

    private readonly ILogger<Connection> _logger = Log.Instance.For<Connection>();
    private readonly IPEndPoint? _address;

    protected bool open = true;
    protected ConcurrentQueue<Packet> readQueue = [];
    protected NetHandler? netHandler;
    protected bool closed;
    protected bool disconnected;
    protected string disconnectedReason = "";
    protected object[]? disconnectReasonArgs;

    public long BytesRead { get; protected set; }
    public long BytesWritten { get; protected set; }
    public int PacketsRead { get; protected set; }
    public int PacketsWritten { get; protected set; }

    private int _timeout;
    private readonly ConcurrentQueue<Packet> _sendQueue = [];
    private Socket? _socket;
    private NetworkStream? _networkStream;

    public Connection(Socket socket, string address, NetHandler netHandler)
    {
        _socket = socket;
        _address = (IPEndPoint?)socket.RemoteEndPoint;
        this.netHandler = netHandler;

        socket.ReceiveTimeout = 30000;

        _networkStream = new NetworkStream(socket);

        Task.Factory.StartNew(Reading, TaskCreationOptions.LongRunning);
        Task.Factory.StartNew(Writing, TaskCreationOptions.LongRunning);
    }

    protected Connection()
    {
        _address = null;
    }

    public void setNetworkHandler(NetHandler netHandler)
    {
        this.netHandler = netHandler;
    }

    public virtual void sendPacket(Packet packet)
    {
        if (packet is ExtendedProtocolPacket && !betaSharpClient) return;

        if (!closed)
        {
            Interlocked.Increment(ref packet.UseCount);
            _sendQueue.Enqueue(packet);
        }
    }

    private void disconnect(Exception e)
    {
        _logger.LogError(e, e.Message);
        disconnect("disconnect.genericReason", "Internal exception: " + e);
    }

    public virtual void disconnect(string disconnectedReason, params object[] disconnectReasonArgs)
    {
        if (open)
        {
            disconnected = true;
            this.disconnectedReason = disconnectedReason;
            this.disconnectReasonArgs = disconnectReasonArgs;
            open = false;

            try
            {
                _networkStream?.Close();
                _networkStream = null;

                _socket?.Close();
                _socket = null;
            }
            catch (Exception)
            {
                // Ignore.
            }
        }
    }

    public virtual void tick()
    {
        if (_sendQueue.Count > 1048576)
        {
            disconnect("disconnect.overflow");
        }

        if (readQueue.IsEmpty)
        {
            if (_timeout++ == 1200)
            {
                disconnect("disconnect.timeout");
            }
        }
        else
        {
            _timeout = 0;
        }

        processPackets();

        if (disconnected && readQueue.IsEmpty)
        {
            netHandler?.onDisconnected(disconnectedReason, disconnectReasonArgs);
        }
    }

    protected virtual void processPackets()
    {
        if (netHandler == null)
        {
            throw new Exception("networkHandler is null");
        }

        int maxPacketsPerTick = 100;

        while (readQueue.TryDequeue(out Packet? packet) && maxPacketsPerTick-- >= 0)
        {
            packet.Apply(netHandler);
            packet.Return();
        }
    }

    public virtual IPEndPoint? getAddress()
    {
        return _address;
    }

    public virtual void disconnect()
    {
        closed = true;
        disconnect(new Exception("disconnect.closed"));
    }

    public int getWorldPacketBacklog()
    {
        return 0;
    }

    private void Reading()
    {
        while (open && !closed)
        {
            try
            {
                ArgumentNullException.ThrowIfNull(_networkStream);
                ArgumentNullException.ThrowIfNull(netHandler);

                Packet? packet = Packet.Read(_networkStream, netHandler.isServerSide());

                if (packet is not null)
                {
                    BytesRead += packet.Size();
                    PacketsRead++;
                    readQueue.Enqueue(packet);
                }
                else
                {
                    disconnect("disconnect.endOfStream");
                    break;
                }
            }
            catch (Exception exception)
            {
                disconnect(exception);
                break;
            }
        }
    }

    private async Task Writing()
    {
        while (open && !closed)
        {
            try
            {
                ArgumentNullException.ThrowIfNull(_networkStream);
                Packet? packet;
                bool wrotePacket = false;

                while (_sendQueue.TryDequeue(out packet))
                {
                    Interlocked.Increment(ref packet.UseCount);
                    int pSize = packet.Size();
                    Packet.Write(packet, _networkStream);
                    packet.Return();
                    BytesWritten += pSize;
                    PacketsWritten++;
                    wrotePacket = true;
                }

                if (wrotePacket)
                {
                    _networkStream.Flush();
                }
                else
                {
                    await Task.Delay(1);
                }
            }
            catch (Exception exception)
            {
                if (!disconnected)
                {
                    disconnect(exception);
                }
                break;
            }
        }
    }
}
