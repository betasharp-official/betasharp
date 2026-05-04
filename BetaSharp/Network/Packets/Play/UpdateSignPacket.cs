namespace BetaSharp.Network.Packets.Play;

public class UpdateSignPacket() : Packet(PacketId.UpdateSign)
{
    public string[] Text { get; private set; } = [];
    public int X { get; private set; }
    public int Y { get; private set; }
    public int Z { get; private set; }

    public static UpdateSignPacket Get(int x, int y, int z, string[] text)
    {
        UpdateSignPacket p = Get<UpdateSignPacket>(PacketId.UpdateSign);
        p.X = x;
        p.Y = y;
        p.Z = z;
        p.Text = text;
        return p;
    }

    public override void Read(Stream stream)
    {
        X = stream.ReadInt();
        Y = stream.ReadShort();
        Z = stream.ReadInt();
        Text = new string[4];

        for (int i = 0; i < 4; ++i)
        {
            Text[i] = stream.ReadLongString(15);
        }
    }

    public override void Write(Stream stream)
    {
        stream.WriteInt(X);
        stream.WriteShort((short)Y);
        stream.WriteInt(Z);

        for (int i = 0; i < 4; ++i)
        {
            stream.WriteLongString(Text[i]);
        }
    }

    public override void Apply(NetHandler networkHandler) => networkHandler.handleUpdateSign(this);

    public override int Size()
    {
        int size = 0;

        for (int i = 0; i < 4; ++i)
        {
            size += Text[i].Length;
        }

        return size;
    }
}
