using BetaSharp.Entities;

namespace BetaSharp.Network.Packets.S2CPlay;

public class EntityVehicleSetS2CPacket() : Packet(PacketId.EntityVehicleSetS2C), IPacketEntity
{
    public int VehicleEntityId { get; private set; }
    public int EntityId { get; private set; }

    public static EntityVehicleSetS2CPacket Get(Entity entity, Entity vehicle)
    {
        EntityVehicleSetS2CPacket p = Get<EntityVehicleSetS2CPacket>(PacketId.EntityVehicleSetS2C);
        p.EntityId = entity.ID;
        p.VehicleEntityId = vehicle != null ? vehicle.ID : -1;
        return p;
    }

    public override void Read(Stream stream)
    {
        EntityId = stream.ReadInt();
        VehicleEntityId = stream.ReadInt();
    }

    public override void Write(Stream stream)
    {
        stream.WriteInt(EntityId);
        stream.WriteInt(VehicleEntityId);
    }

    public override void Apply(NetHandler handler) => handler.onEntityVehicleSet(this);

    public override int Size() => IPacketEntity.PacketBaseEntitySize + 4;
}
