using BetaSharp.Items;

namespace BetaSharp.Network.Packets.S2CPlay;

public class ScreenHandlerSlotUpdateS2CPacket() : Packet(PacketId.ScreenHandlerSlotUpdateS2C)
{
    public int Slot { get; private set; }
    public ItemStack Stack { get; private set; }
    public int SyncId { get; private set; }

    public static ScreenHandlerSlotUpdateS2CPacket Get(int syncId, int slot, ItemStack? stack)
    {
        ScreenHandlerSlotUpdateS2CPacket p = Get<ScreenHandlerSlotUpdateS2CPacket>(PacketId.ScreenHandlerSlotUpdateS2C);
        p.SyncId = syncId;
        p.Slot = slot;
        p.Stack = stack == null ? stack : stack.copy();
        return p;
    }

    public override void Apply(NetHandler handler) => handler.onScreenHandlerSlotUpdate(this);

    public override void Read(Stream stream)
    {
        SyncId = (sbyte)stream.ReadByte();
        Slot = stream.ReadShort();
        short itemId = stream.ReadShort();
        if (itemId >= 0)
        {
            sbyte count = (sbyte)stream.ReadByte();
            short damage = stream.ReadShort();
            Stack = new ItemStack(itemId, count, damage);
        }
        else
        {
            Stack = null;
        }
    }

    public override void Write(Stream stream)
    {
        stream.WriteByte((byte)SyncId);
        stream.WriteShort((short)Slot);
        if (Stack == null)
        {
            stream.WriteShort(-1);
        }
        else
        {
            stream.WriteShort((short)Stack.ItemId);
            stream.WriteByte((byte)Stack.Count);
            stream.WriteShort((short)Stack.getDamage());
        }
    }

    public override int Size() => 8;
}
