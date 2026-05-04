using BetaSharp.Items;

namespace BetaSharp.Network.Packets.S2CPlay;

public class EntityEquipmentUpdateS2CPacket() : Packet(PacketId.EntityEquipmentUpdateS2C), IPacketEntity
{
    public int Slot { get; private set; }
    public int ItemRawId { get; private set; } = -1;
    public int ItemDamage { get; private set; }
    public int EntityId { get; private set; }

    public static EntityEquipmentUpdateS2CPacket Get(int entityId, int slot, ItemStack? itemStack = null)
    {
        EntityEquipmentUpdateS2CPacket p = Get<EntityEquipmentUpdateS2CPacket>(PacketId.EntityEquipmentUpdateS2C);
        p.EntityId = entityId;
        p.Slot = slot;
        if (itemStack == null)
        {
            p.ItemRawId = -1;
            p.ItemDamage = 0;
        }
        else
        {
            p.ItemRawId = itemStack.ItemId;
            p.ItemDamage = itemStack.getDamage();
        }

        return p;
    }

    public override void Read(Stream stream)
    {
        EntityId = stream.ReadInt();
        Slot = stream.ReadShort();
        ItemRawId = stream.ReadShort();
        ItemDamage = stream.ReadShort();
    }

    public override void Write(Stream stream)
    {
        stream.WriteInt(EntityId);
        stream.WriteShort((short)Slot);
        stream.WriteShort((short)ItemRawId);
        stream.WriteShort((short)ItemDamage);
    }

    public override void Apply(NetHandler handler) => handler.onEntityEquipmentUpdate(this);

    public override int Size() => 6 + IPacketEntity.PacketBaseEntitySize;
}
