using BetaSharp.Items;

namespace BetaSharp.Network.Packets.C2SPlay;

public class PlayerInteractBlockC2SPacket() : Packet(PacketId.PlayerInteractBlockC2S)
{
    public int Side { get; set; }
    public ItemStack Stack { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
    public int Z { get; set; }

    public static PlayerInteractBlockC2SPacket Get(int x, int y, int z, int side, ItemStack stack)
    {
        PlayerInteractBlockC2SPacket p = Get<PlayerInteractBlockC2SPacket>(PacketId.PlayerInteractBlockC2S);
        p.X = x;
        p.Y = y;
        p.Z = z;
        p.Side = side;
        p.Stack = stack;
        return p;
    }

    public override void Read(Stream stream)
    {
        X = stream.ReadInt();
        Y = stream.ReadByte();
        Z = stream.ReadInt();
        Side = stream.ReadByte();
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
        stream.WriteInt(X);
        stream.WriteByte((byte)Y);
        stream.WriteInt(Z);
        stream.WriteByte((byte)Side);
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

    public override void Apply(NetHandler handler) => handler.onPlayerInteractBlock(this);

    public override int Size() => 15;
}
