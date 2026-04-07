using System.Net.Sockets;

namespace BetaSharp.Network.Packets.S2CPlay;

public class EntityTrackerUpdateS2CPacket() : PacketBaseEntity(PacketId.EntityTrackerUpdateS2C)
{
    public byte[] Data;

    public static EntityTrackerUpdateS2CPacket Get(int entityId, byte[] data)
    {
        var p = Get<EntityTrackerUpdateS2CPacket>(PacketId.EntityTrackerUpdateS2C);
        p.EntityId = entityId;
        p.Data = data;
        return p;
    }

    public override void Read(Stream stream)
    {
        base.Read(stream);
        Data = stream.ReadUntil(127);
    }

    public override void Write(Stream stream)
    {
        base.Write(stream);
        stream.Write(Data);
        stream.WriteByte(127);
    }

    public override void Apply(NetHandler handler)
    {
        handler.onEntityTrackerUpdate(this);
    }

    public override int Size()
    {
        return base.Size() + Data.Length + 1;
    }
}
