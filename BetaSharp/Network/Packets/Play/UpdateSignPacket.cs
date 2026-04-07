using System.Net.Sockets;

namespace BetaSharp.Network.Packets.Play;

public class UpdateSignPacket() : Packet(PacketId.UpdateSign)
{
    public int x;
    public int y;
    public int z;
    public string[] text;

    public static UpdateSignPacket Get(int x, int y, int z, string[] text)
    {
        var p = Get<UpdateSignPacket>(PacketId.UpdateSign);
        p.x = x;
        p.y = y;
        p.z = z;
        p.text = text;
        return p;
    }

    public override void Read(Stream stream)
    {
        x = stream.ReadInt();
        y = stream.ReadShort();
        z = stream.ReadInt();
        text = new string[4];

        for (int i = 0; i < 4; ++i)
        {

            text[i] = stream.ReadLongString(15);
        }

    }

    public override void Write(Stream stream)
    {
        stream.WriteInt(x);
        stream.WriteShort((short)y);
        stream.WriteInt(z);

        for (int i = 0; i < 4; ++i)
        {
            stream.WriteLongString(text[i]);
        }

    }

    public override void Apply(NetHandler networkHandler)
    {
        networkHandler.handleUpdateSign(this);
    }

    public override int Size()
    {
        int size = 0;

        for (int i = 0; i < 4; ++i)
        {
            size += text[i].Length;
        }

        return size;
    }
}
