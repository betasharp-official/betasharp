using BetaSharp.Items;

namespace BetaSharp.Network.Packets.C2SPlay;

public class ClickSlotC2SPacket() : Packet(PacketId.ClickSlotC2S)
{
    public short ActionType { get; private set; }
    public int Button { get; private set; }
    public bool HoldingShift { get; private set; }
    public int Slot { get; private set; }
    public ItemStack? Stack { get; private set; }
    public int SyncId { get; private set; }

    public static ClickSlotC2SPacket Get(int syncId, int slot, int button, bool holdingShift, ItemStack stack, short actionType)
    {
        ClickSlotC2SPacket p = Get<ClickSlotC2SPacket>(PacketId.ClickSlotC2S);
        p.SyncId = syncId;
        p.Slot = slot;
        p.Button = button;
        p.Stack = stack;
        p.ActionType = actionType;
        p.HoldingShift = holdingShift;
        return p;
    }

    public override void Apply(NetHandler handler) => handler.onClickSlot(this);

    public override void Read(Stream stream)
    {
        SyncId = (sbyte)stream.ReadByte();
        Slot = stream.ReadShort();
        Button = (sbyte)stream.ReadByte();
        ActionType = stream.ReadShort();
        HoldingShift = stream.ReadBoolean();
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
        stream.WriteByte((byte)Button);
        stream.WriteShort(ActionType);
        stream.WriteBoolean(HoldingShift);
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

    public override int Size() => 11;
}
