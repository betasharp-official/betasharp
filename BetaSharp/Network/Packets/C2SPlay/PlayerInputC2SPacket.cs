namespace BetaSharp.Network.Packets.C2SPlay;

public class PlayerInputC2SPacket() : Packet(PacketId.PlayerInputC2S)
{
    public float Forward { get; private set; }
    public bool Jumping { get; private set; }
    public float Pitch { get; private set; }
    public float Sideways { get; private set; }
    public bool Sneaking { get; private set; }
    public float Yaw { get; private set; }

    public override void Read(Stream stream)
    {
        Sideways = stream.ReadFloat();
        Forward = stream.ReadFloat();
        Pitch = stream.ReadFloat();
        Yaw = stream.ReadFloat();
        Jumping = stream.ReadBoolean();
        Sneaking = stream.ReadBoolean();
    }

    public override void Write(Stream stream)
    {
        stream.WriteFloat(Sideways);
        stream.WriteFloat(Forward);
        stream.WriteFloat(Pitch);
        stream.WriteFloat(Yaw);
        stream.WriteBoolean(Jumping);
        stream.WriteBoolean(Sneaking);
    }

    public override void Apply(NetHandler handler) => handler.onPlayerInput(this);

    public override int Size() => 18;
}
