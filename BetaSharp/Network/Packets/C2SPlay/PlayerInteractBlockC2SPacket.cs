using System.Net.Sockets;
using BetaSharp.Items;

namespace BetaSharp.Network.Packets.C2SPlay;

public class PlayerInteractBlockC2SPacket() : Packet(PacketId.PlayerInteractBlockC2S)
{
    public int x;
    public int y;
    public int z;
    public int side;
    public ItemStack stack;

    public PlayerInteractBlockC2SPacket(int x, int y, int z, int side, ItemStack stack) : this()
    {
        this.x = x;
        this.y = y;
        this.z = z;
        this.side = side;
        this.stack = stack;
    }

    public override void Read(NetworkStream stream)
    {
        x = stream.ReadInt();
        y = stream.ReadByte();
        z = stream.ReadInt();
        side = stream.ReadByte();
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
        stream.WriteInt(x);
        stream.WriteByte((byte)y);
        stream.WriteInt(z);
        stream.WriteByte((byte)side);
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

    public override void Apply(NetHandler handler)
    {
        handler.onPlayerInteractBlock(this);
    }

    public override int Size()
    {
        return 15;
    }
}
