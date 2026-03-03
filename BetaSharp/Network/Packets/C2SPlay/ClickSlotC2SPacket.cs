using System.Net.Sockets;
using BetaSharp.Items;

namespace BetaSharp.Network.Packets.C2SPlay;

public class ClickSlotC2SPacket() : Packet(PacketId.ClickSlotC2S)
{
    public int syncId;
    public int slot;
    public int button;
    public short actionType;
    public ItemStack stack;
    public bool holdingShift;

    public ClickSlotC2SPacket(int syncId, int slot, int button, bool holdingShift, ItemStack stack, short actionType) : this()
    {
        this.syncId = syncId;
        this.slot = slot;
        this.button = button;
        this.stack = stack;
        this.actionType = actionType;
        this.holdingShift = holdingShift;
    }

    public override void Apply(NetHandler handler)
    {
        handler.onClickSlot(this);
    }

    public override void Read(NetworkStream stream)
    {
        syncId = (sbyte)stream.ReadByte();
        slot = stream.ReadShort();
        button = (sbyte)stream.ReadByte();
        actionType = stream.ReadShort();
        holdingShift = stream.ReadBoolean();
        short itemId = stream.ReadShort();
        if (itemId >= 0)
        {
            sbyte count = (sbyte)stream.ReadByte();
            short damage = stream.ReadShort();
            stack = new ItemStack(itemId, count, damage);
        }
        else
        {
            stack = null;
        }

    }

    public override void Write(NetworkStream stream)
    {
        stream.WriteByte((byte)syncId);
        stream.WriteShort((short)slot);
        stream.WriteByte((byte)button);
        stream.WriteShort((short)actionType);
        stream.WriteBoolean(holdingShift);
        if (stack == null)
        {
            stream.WriteShort((short)-1);
        }
        else
        {
            stream.WriteShort((short)stack.itemId);
            stream.WriteByte((byte)stack.count);
            stream.WriteShort((short)stack.getDamage());
        }

    }

    public override int Size()
    {
        return 11;
    }
}
