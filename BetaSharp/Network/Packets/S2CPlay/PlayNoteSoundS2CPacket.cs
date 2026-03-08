using System.Net.Sockets;

namespace BetaSharp.Network.Packets.S2CPlay;

public class PlayNoteSoundS2CPacket() : Packet(PacketId.PlayNoteSoundS2C)
{
    public int xLocation;
    public int yLocation;
    public int zLocation;
    public int instrumentType;
    public int pitch;

    public static PlayNoteSoundS2CPacket Get(int x, int y, int z, int instrument, int pitch)
    {
        var p = Get<PlayNoteSoundS2CPacket>(PacketId.PlayNoteSoundS2C);
        p.xLocation = x;
        p.yLocation = y;
        p.zLocation = z;
        p.instrumentType = instrument;
        p.pitch = pitch;
        return p;
    }

    public override void Read(NetworkStream stream)
    {
        xLocation = stream.ReadInt();
        yLocation = stream.ReadShort();
        zLocation = stream.ReadInt();
        instrumentType = stream.ReadByte();
        pitch = stream.ReadByte();
    }

    public override void Write(NetworkStream stream)
    {
        stream.WriteInt(xLocation);
        stream.WriteShort((short)yLocation);
        stream.WriteInt(zLocation);
        stream.WriteByte((byte)instrumentType);
        stream.WriteByte((byte)pitch);
    }

    public override void Apply(NetHandler handler)
    {
        handler.onPlayNoteSound(this);
    }

    public override int Size()
    {
        return 12;
    }
}
