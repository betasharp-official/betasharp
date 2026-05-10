namespace BetaSharp.Network.Packets.S2CPlay;

public class PlayNoteSoundS2CPacket() : Packet(PacketId.PlayNoteSoundS2C)
{
    public int InstrumentType { get; private set; }
    public int Pitch { get; private set; }
    public int XLocation { get; private set; }
    public int YLocation { get; private set; }
    public int ZLocation { get; private set; }

    public static PlayNoteSoundS2CPacket Get(int x, int y, int z, int instrument, int pitch)
    {
        PlayNoteSoundS2CPacket p = Get<PlayNoteSoundS2CPacket>(PacketId.PlayNoteSoundS2C);
        p.XLocation = x;
        p.YLocation = y;
        p.ZLocation = z;
        p.InstrumentType = instrument;
        p.Pitch = pitch;
        return p;
    }

    public override void Read(Stream stream)
    {
        XLocation = stream.ReadInt();
        YLocation = stream.ReadShort();
        ZLocation = stream.ReadInt();
        InstrumentType = stream.ReadByte();
        Pitch = stream.ReadByte();
    }

    public override void Write(Stream stream)
    {
        stream.WriteInt(XLocation);
        stream.WriteShort((short)YLocation);
        stream.WriteInt(ZLocation);
        stream.WriteByte((byte)InstrumentType);
        stream.WriteByte((byte)Pitch);
    }

    public override void Apply(NetHandler handler) => handler.onPlayNoteSound(this);

    public override int Size() => 12;
}
