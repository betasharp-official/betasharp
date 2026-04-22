using System.Net.Sockets;
using BetaSharp.Items;

namespace BetaSharp.Network.Packets.S2CPlay;

public class EntityEquipmentUpdateS2CPacket() : PacketBaseEntity(PacketId.EntityEquipmentUpdateS2C)
{
    public int slot;
    public int itemRawId;
    public int itemDamage;

    public static EntityEquipmentUpdateS2CPacket Get(int entityId, int slot, ItemStack itemStack)
    {
        var p = Get<EntityEquipmentUpdateS2CPacket>(PacketId.EntityEquipmentUpdateS2C);
        p.EntityId = entityId;
        p.slot = slot;
        if (itemStack == null)
        {
            p.itemRawId = -1;
            p.itemDamage = 0;
        }
        else
        {
            p.itemRawId = itemStack.ItemId;
            p.itemDamage = itemStack.getDamage();
        }

        return p;
    }

    public override void Read(Stream stream)
    {
        base.Read(stream);
        slot = stream.ReadShort();
        itemRawId = stream.ReadShort();
        itemDamage = stream.ReadShort();
    }

    public override void Write(Stream stream)
    {
        base.Write(stream);
        stream.WriteShort((short)slot);
        stream.WriteShort((short)itemRawId);
        stream.WriteShort((short)itemDamage);
    }

    public override void Apply(NetHandler handler)
    {
        handler.onEntityEquipmentUpdate(this);
    }

    public override int Size()
    {
        return 6 + PacketBaseEntitySize;
    }
}
