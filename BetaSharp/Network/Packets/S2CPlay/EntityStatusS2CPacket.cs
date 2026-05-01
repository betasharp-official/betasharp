namespace BetaSharp.Network.Packets.S2CPlay;

public class EntityStatusS2CPacket() : Packet(PacketId.EntityStatusS2C), IPacketEntity
{
    public enum EntityState : byte
    {
        Hurt = 2,
        Death = 3,
        WolfSmokeFx = 6,
        WolfHeartsFx = 7,
        WolfShaking = 8
    }

    public sbyte EntityStatus { get; private set; }
    public int EntityId { get; private set; }

    public static EntityStatusS2CPacket Get(int entityId, EntityState status) => Get(entityId, (byte)status);

    public static EntityStatusS2CPacket Get(int entityId, byte status)
    {
        EntityStatusS2CPacket p = Get<EntityStatusS2CPacket>(PacketId.EntityStatusS2C);
        p.EntityId = entityId;
        p.EntityStatus = (sbyte)status;
        return p;
    }

    public override void Read(Stream stream)
    {
        EntityId = stream.ReadInt();
        EntityStatus = (sbyte)stream.ReadByte();
    }

    public override void Write(Stream stream)
    {
        stream.WriteInt(EntityId);
        stream.WriteByte((byte)EntityStatus);
    }

    public override void Apply(NetHandler handler) => handler.onEntityStatus(this);

    public override int Size() => IPacketEntity.PacketBaseEntitySize + 1;
}
