using System.Net.Sockets;
using BetaSharp.Items;

namespace BetaSharp.Network.Packets.S2CPlay;

public class ScreenHandlerSlotUpdateS2CPacket() : Packet(PacketId.ScreenHandlerSlotUpdateS2C)
{
    public int syncId;
    public int slot;
    public ItemStack stack;

    public static ScreenHandlerSlotUpdateS2CPacket Get(int syncId, int slot, ItemStack stack)
    {
        var p = Get<ScreenHandlerSlotUpdateS2CPacket>(PacketId.ScreenHandlerSlotUpdateS2C);
        p.syncId = syncId;
        p.slot = slot;
        p.stack = stack == null ? stack : stack.copy();
        return p;
    }

    public override void Apply(NetHandler handler)
    {
        handler.onScreenHandlerSlotUpdate(this);
    }

    public override void Read(NetworkStream stream)
    {
        syncId = (sbyte)stream.ReadByte();
        slot = stream.ReadShort();
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
        if (stack == null)
        {
            stream.WriteShort(-1);
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
        return 8;
    }
}
