using System.Net.Sockets;

namespace BetaSharp.Network.Packets.Play;

public class UpdateSignPacket() : Packet(PacketId.UpdateSign)
{
    public int x;
    public int y;
    public int z;
    public string[] text;

    public UpdateSignPacket(int x, int y, int z, string[] text) : this()
    {
        this.x = x;
        this.y = y;
        this.z = z;
        this.text = text;
    }

    public override void Read(NetworkStream stream)
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

    public override void Write(NetworkStream stream)
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
