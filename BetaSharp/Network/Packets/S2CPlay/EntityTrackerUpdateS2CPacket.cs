using System.Net.Sockets;

namespace BetaSharp.Network.Packets.S2CPlay;

public class EntityTrackerUpdateS2CPacket() : PacketBaseEntity(PacketId.EntityTrackerUpdateS2C)
{
    public byte[] Data;

    public EntityTrackerUpdateS2CPacket(int entityId, byte[] data) : this()
    {
        EntityId = entityId;
        Data = data;
    }

    public override void Read(NetworkStream stream)
    {
        base.Read(stream);
        Data = stream.ReadUntil(127);
    }

    public override void Write(NetworkStream stream)
    {
        base.Write(stream);
        stream.Write(Data);
    }

    public override void Apply(NetHandler handler)
    {
        handler.onEntityTrackerUpdate(this);
    }

    public override int Size()
    {
        // TODO : this is wrong
        return 5;
    }
}
