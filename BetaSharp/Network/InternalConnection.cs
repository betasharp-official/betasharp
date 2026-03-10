using System.Net;
using BetaSharp.Network.Packets;
using Microsoft.Extensions.Logging;

namespace BetaSharp.Network;

public class InternalConnection : Connection
{
    public InternalConnection RemoteConnection { get; set; }

    public string Name { get; set; }

    private readonly ILogger<InternalConnection> _logger = Log.Instance.For<InternalConnection>();

    public InternalConnection(NetHandler? netHandler, string name)
    {
        this.netHandler = netHandler;
        Name = name;
    }

    public void AssignRemote(InternalConnection remote)
    {
        RemoteConnection = remote;
    }

    public override void sendPacket(Packet packet)
    {
        if (!closed)
        {
            packet.ProcessForInternal();

            if (RemoteConnection != null && !RemoteConnection.closed)
            {
                Interlocked.Increment(ref packet.UseCount);
                RemoteConnection.ReceivePacket(packet);
            }
        }
    }

    protected void ReceivePacket(Packet packet)
    {
        readQueue.Enqueue(packet);
    }

    protected override void processPackets()
    {
        if (netHandler == null)
        {
            throw new Exception($"InternalConnection is not initialized");
        }

        int count = 0;
        while (readQueue.TryDequeue(out var packet))
        {
            packet.Apply(netHandler);
            packet.Return();
            count++;
        }
        if (count > 0)
        {
            // _logger.LogInformation($"[{Name}] Processed {count} packets");
        }
    }

    public override void disconnect(string disconnectedReason, params object[] disconnectReasonArgs)
    {
        if (open)
        {
            open = false;
            disconnected = true;
            this.disconnectedReason = disconnectedReason;
            this.disconnectReasonArgs = disconnectReasonArgs;

            _logger.LogInformation($"[{Name}] Disconnected: {disconnectedReason}");

            if (RemoteConnection != null && RemoteConnection.open)
            {
                RemoteConnection.OnRemoteDisconnect(disconnectedReason, disconnectReasonArgs);
            }
        }
    }

    public void OnRemoteDisconnect(string reason, object[] args)
    {
        if (open)
        {
            open = false;
            disconnected = true;
            disconnectedReason = reason;
            disconnectReasonArgs = args;
            _logger.LogInformation($"[{Name}] Remote disconnected: {reason}");
        }
    }

    public override void disconnect()
    {
        disconnect("Disconnecting");
    }

    public override void tick()
    {
        processPackets();
        if (disconnected && readQueue.IsEmpty)
        {
            netHandler?.onDisconnected(disconnectedReason, disconnectReasonArgs);
        }
    }

    public override IPEndPoint getAddress()
    {
        return new IPEndPoint(IPAddress.Parse("127.0.0.1"), 12345);
    }
}
