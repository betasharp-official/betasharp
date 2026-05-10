namespace BetaSharp.Network.Packets.C2SPlay;

public class UpdateSelectedSlotC2SPacket() : Packet(PacketId.UpdateSelectedSlotC2S)
{
    public int SelectedSlot { get; private set; }

    public static UpdateSelectedSlotC2SPacket Get(int selectedSlot)
    {
        UpdateSelectedSlotC2SPacket p = Get<UpdateSelectedSlotC2SPacket>(PacketId.UpdateSelectedSlotC2S);
        p.SelectedSlot = selectedSlot;
        return p;
    }

    public override void Read(Stream stream) => SelectedSlot = stream.ReadShort();

    public override void Write(Stream stream) => stream.WriteShort((short)SelectedSlot);

    public override void Apply(NetHandler handler) => handler.onUpdateSelectedSlot(this);

    public override int Size() => 2;
}
