using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading;
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

    private int _timeout;
    private ConcurrentQueue<Packet> _sendQueue = [];
    private ConcurrentQueue<Packet> _delayedSendQueue = [];
    private int _delay;
    private Socket? _socket;
    private NetworkStream? _networkStream;

    public Connection(Socket socket, string address, NetHandler netHandler)
    {
        _socket = socket;
        _address = (IPEndPoint?)socket.RemoteEndPoint;
        this.netHandler = netHandler;

        socket.ReceiveTimeout = 30000;
        // setTrafficClass doesn't have a direct .NET equivalent and can be omitted

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
            if (Packet.Registry[packet.Id]!.WorldPacket)
            {
                _delayedSendQueue.Enqueue(packet);
            }
            else
            {
                _sendQueue.Enqueue(packet);
            }
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
        if (_sendQueue.Count > 1048576 || _delayedSendQueue.Count > 1048576)
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

        while (readQueue.TryDequeue(out var packet) && maxPacketsPerTick-- >= 0)
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

    public int getDelayedSendQueueSize()
    {
        return _delayedSendQueue.Count;
    }

    public int getWorldPacketBacklog()
    {
        return _delayedSendQueue.Count;
    }

    private async void Reading()
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
                    readQueue.Enqueue(packet);
                }
                else
                {
                    disconnect("disconnect.endOfStream");
                    break;
                }

                await Task.Delay(10);
            }
            catch (Exception exception)
            {
                disconnect(exception);
                break;
            }
        }
    }

    private async void Writing()
    {
        while (open && !closed)
        {
            try
            {
                ArgumentNullException.ThrowIfNull(_networkStream);

                Packet? packet;
                bool wrotePacket = false;

                if (!_sendQueue.IsEmpty)
                {
                    if (!_sendQueue.TryDequeue(out packet))
                    {
                        continue;
                    }

                    Interlocked.Increment(ref packet.UseCount);

                    Packet.Write(packet, _networkStream);
                    packet.Return();
                    wrotePacket = true;
                }

                if (!_delayedSendQueue.IsEmpty && _delay-- <= 0)
                {
                    if (!_delayedSendQueue.TryDequeue(out packet))
                    {
                        continue;
                    }

                    Interlocked.Increment(ref packet.UseCount);

                    _delay = 0;

                    Packet.Write(packet, _networkStream);
                    packet.Return();
                    wrotePacket = true;
                }

                if (wrotePacket)
                {
                    _networkStream.Flush();
                    await Task.Delay(0);
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
