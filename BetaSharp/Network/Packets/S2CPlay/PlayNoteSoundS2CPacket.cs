using System.Net.Sockets;

namespace BetaSharp.Network.Packets.S2CPlay;

public class PlayNoteSoundS2CPacket() : Packet(PacketId.PlayNoteSoundS2C)
{
    public int xLocation;
    public int yLocation;
    public int zLocation;
    public int instrumentType;
    public int pitch;

    public PlayNoteSoundS2CPacket(int x, int y, int z, int instrument, int pitch) : this()
    {
        xLocation = x;
        yLocation = y;
        zLocation = z;
        instrumentType = instrument;
        this.pitch = pitch;
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
