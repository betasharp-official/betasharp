using System.Net.Sockets;
using BetaSharp.Items;

namespace BetaSharp.Network.Packets.S2CPlay;

public class InventoryS2CPacket() : Packet(PacketId.InventoryS2C)
{
    public int syncId;
    public ItemStack[] contents;

    public static InventoryS2CPacket Get(int syncId, List<ItemStack> contents)
    {
        var p = Get<InventoryS2CPacket>(PacketId.InventoryS2C);
        p.syncId = syncId;
        p.contents = new ItemStack[contents.Count];

        for (int i = 0; i < p.contents.Length; i++)
        {
            ItemStack itemStack = contents[i];
            p.contents[i] = itemStack == null ? null : itemStack.copy();
        }

        return p;
    }

    public override void Read(NetworkStream stream)
    {
        syncId = (sbyte)stream.ReadByte();
        short itemsCount = stream.ReadShort();
        contents = new ItemStack[itemsCount];

        for (int i = 0; i < itemsCount; ++i)
        {
            short itemId = stream.ReadShort();
            if (itemId >= 0)
            {
                sbyte count = (sbyte)stream.ReadByte();
                short damage = stream.ReadShort();

                contents[i] = new ItemStack(itemId, count, damage);
            }
        }

    }

    public override void Write(NetworkStream stream)
    {
        stream.WriteByte((byte)syncId);
        stream.WriteShort((short)contents.Length);

        for (int i = 0; i < contents.Length; ++i)
        {
            if (contents[i] == null)
            {
                stream.WriteShort(-1);
            }
            else
            {
                stream.WriteShort((short)contents[i].itemId);
                stream.WriteByte((byte)contents[i].count);
                stream.WriteShort((short)contents[i].getDamage());
            }
        }

    }

    public override void Apply(NetHandler handler)
    {
        handler.onInventory(this);
    }

    public override int Size()
    {
        return 3 + contents.Length * 5;
    }
}
