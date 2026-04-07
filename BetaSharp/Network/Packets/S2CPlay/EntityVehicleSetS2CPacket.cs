using System.Net.Sockets;
using BetaSharp.Entities;

namespace BetaSharp.Network.Packets.S2CPlay;

public class EntityVehicleSetS2CPacket() : PacketBaseEntity(PacketId.EntityVehicleSetS2C)
{
    public int VehicleEntityId;

    public static EntityVehicleSetS2CPacket Get(Entity entity, Entity vehicle)
    {
        var p = Get<EntityVehicleSetS2CPacket>(PacketId.EntityVehicleSetS2C);
        p.EntityId = entity.id;
        p.VehicleEntityId = vehicle != null ? vehicle.id : -1;
        return p;
    }

    public override void Read(Stream stream)
    {
        base.Read(stream);
        VehicleEntityId = stream.ReadInt();
    }

    public override void Write(Stream stream)
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
