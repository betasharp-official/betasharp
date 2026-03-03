using System.Net.Sockets;
using BetaSharp.Items;

namespace BetaSharp.Network.Packets.S2CPlay;

public class EntityEquipmentUpdateS2CPacket() : PacketBaseEntity(PacketId.EntityEquipmentUpdateS2C)
{
    public int slot;
    public int itemRawId;
    public int itemDamage;

    public EntityEquipmentUpdateS2CPacket(int entityId, int slot, ItemStack itemStack) : this()
    {
        EntityId = entityId;
        this.slot = slot;
        if (itemStack == null)
        {
            itemRawId = -1;
            itemDamage = 0;
        }
        else
        {
            itemRawId = itemStack.itemId;
            itemDamage = itemStack.getDamage();
        }
    }

    public override void Read(NetworkStream stream)
    {
        base.Read(stream);
        slot = stream.ReadShort();
        itemRawId = stream.ReadShort();
        itemDamage = stream.ReadShort();
    }

    public override void Write(NetworkStream stream)
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
