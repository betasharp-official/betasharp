using System.Net.Sockets;
using BetaSharp.Entities;

namespace BetaSharp.Network.Packets.S2CPlay;

public class EntityVehicleSetS2CPacket() : PacketBaseEntity(PacketId.EntityVehicleSetS2C)
{
    public int VehicleEntityId;

    public EntityVehicleSetS2CPacket(Entity entity, Entity vehicle) : this()
    {
        EntityId = entity.id;
        VehicleEntityId = vehicle != null ? vehicle.id : -1;
    }

    public override void Read(NetworkStream stream)
    {
        base.Read(stream);
        VehicleEntityId = stream.ReadInt();
    }

    public override void Write(NetworkStream stream)
    {
        base.Write(stream);
        stream.WriteInt(VehicleEntityId);
    }

    public override void Apply(NetHandler handler)
    {
        handler.onEntityVehicleSet(this);
    }

    public override int Size()
    {
        return PacketBaseEntitySize + 4;
    }
}
