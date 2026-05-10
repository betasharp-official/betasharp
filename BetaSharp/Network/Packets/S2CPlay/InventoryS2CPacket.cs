using BetaSharp.Items;

namespace BetaSharp.Network.Packets.S2CPlay;

public class InventoryS2CPacket() : Packet(PacketId.InventoryS2C)
{
    public ItemStack[] Contents { get; private set; } = [];
    public int SyncId { get; private set; }

    public static InventoryS2CPacket Get(int syncId, List<ItemStack> contents)
    {
        InventoryS2CPacket p = Get<InventoryS2CPacket>(PacketId.InventoryS2C);
        p.SyncId = syncId;
        p.Contents = new ItemStack[contents.Count];

        for (int i = 0; i < p.Contents.Length; i++)
        {
            ItemStack itemStack = contents[i];
            p.Contents[i] = itemStack == null ? null : itemStack.copy();
        }

        return p;
    }

    public override void Read(Stream stream)
    {
        SyncId = (sbyte)stream.ReadByte();
        short itemsCount = stream.ReadShort();
        Contents = new ItemStack[itemsCount];

        for (int i = 0; i < itemsCount; ++i)
        {
            short itemId = stream.ReadShort();
            if (itemId >= 0)
            {
                sbyte count = (sbyte)stream.ReadByte();
                short damage = stream.ReadShort();

                Contents[i] = new ItemStack(itemId, count, damage);
            }
        }
    }

    public override void Write(Stream stream)
    {
        stream.WriteByte((byte)SyncId);
        stream.WriteShort((short)Contents.Length);

        for (int i = 0; i < Contents.Length; ++i)
        {
            if (Contents[i] == null)
            {
                stream.WriteShort(-1);
            }
            else
            {
                stream.WriteShort((short)Contents[i].ItemId);
                stream.WriteByte((byte)Contents[i].Count);
                stream.WriteShort((short)Contents[i].getDamage());
            }
        }
    }

    public override void Apply(NetHandler handler) => handler.onInventory(this);

    public override int Size() => 3 + Contents.Length * 5;
}
