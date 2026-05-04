namespace BetaSharp.Network.Packets.S2CPlay;

public class EntityTrackerUpdateS2CPacket() : Packet(PacketId.EntityTrackerUpdateS2C), IPacketEntity
{
    public byte[] Data { get; private set; } = [];
    public int EntityId { get; private set; }

    public static EntityTrackerUpdateS2CPacket Get(int entityId, byte[] data)
    {
        EntityTrackerUpdateS2CPacket p = Get<EntityTrackerUpdateS2CPacket>(PacketId.EntityTrackerUpdateS2C);
        p.EntityId = entityId;
        p.Data = data;
        return p;
    }

    public override void Read(Stream stream)
    {
        EntityId = stream.ReadInt();
        Data = stream.ReadUntil(127);
    }

    public override void Write(Stream stream)
    {
        stream.WriteInt(EntityId);
        stream.Write(Data);
        stream.WriteByte(127);
    }

    public override void Apply(NetHandler handler) => handler.onEntityTrackerUpdate(this);

    public override int Size() => IPacketEntity.PacketBaseEntitySize + Data.Length + 1;
}
