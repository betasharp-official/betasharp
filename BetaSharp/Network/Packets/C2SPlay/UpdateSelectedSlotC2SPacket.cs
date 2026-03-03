using System.Net.Sockets;

namespace BetaSharp.Network.Packets.C2SPlay;

public class UpdateSelectedSlotC2SPacket() : Packet(PacketId.UpdateSelectedSlotC2S)
{
    public int selectedSlot;

    public UpdateSelectedSlotC2SPacket(int selectedSlot) : this()
    {
        this.selectedSlot = selectedSlot;
    }

    public override void Read(NetworkStream stream)
    {
        selectedSlot = stream.ReadShort();
    }

    public override void Write(NetworkStream stream)
    {
        stream.WriteShort((short)selectedSlot);
    }

    public override void Apply(NetHandler handler)
    {
        handler.onUpdateSelectedSlot(this);
    }

    public override int Size()
    {
        return 2;
    }
}
